using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Engine;

/// <summary>
/// Central asset management coordinator. Handles async loading, deduplication, type-safe storage,
/// dependency tracking, and hot-reload via file watching.
/// </summary>
/// <remarks>
/// <para>
/// Registered as a <see cref="World"/> resource by <see cref="AssetPlugin"/>. The server orchestrates:
/// <list type="bullet">
///   <item><description>Pluggable <see cref="IAssetReader"/> sources (filesystem, embedded, network).</description></item>
///   <item><description>Pluggable <see cref="IAssetLoader{T}"/> per file extension.</description></item>
///   <item><description>Background thread pool for async loading via <see cref="Channel{T}"/>.</description></item>
///   <item><description>Per-path deduplication - same path always returns the same <see cref="Handle{T}"/>.</description></item>
///   <item><description>Hot-reload via <see cref="IAssetWatcher"/> when enabled.</description></item>
///   <item><description>Typed <see cref="Assets{T}"/> storage and <see cref="AssetEvent{T}"/> lifecycle events.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Threading model:</b> <see cref="Load{T}(string)"/> can be called from any thread and returns
/// a handle immediately. Actual I/O runs on background threads. The <see cref="ProcessCompleted"/>
/// method is called once per frame on the schedule thread (by <see cref="AssetPlugin"/>) to drain
/// completed loads into <see cref="Assets{T}"/> and fire <see cref="AssetEvent{T}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a plugin's Build method:
/// var server = world.Resource&lt;AssetServer&gt;();
/// server.RegisterLoader(new TextureLoader());
///
/// // In a system:
/// var server = world.Resource&lt;AssetServer&gt;();
/// Handle&lt;Texture&gt; tex = server.Load&lt;Texture&gt;("textures/ground.png");
///
/// // Next frame, check if loaded:
/// var assets = world.Resource&lt;Assets&lt;Texture&gt;&gt;();
/// if (assets.TryGet(tex, out var texture))
///     BindTexture(texture);
/// </code>
/// </example>
/// <seealso cref="AssetPlugin"/>
/// <seealso cref="Assets{T}"/>
/// <seealso cref="Handle{T}"/>
/// <seealso cref="IAssetLoader{T}"/>
/// <seealso cref="IAssetReader"/>
public sealed class AssetServer : IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.AssetServer");

    // ── Sources ──────────────────────────────────────────────────
    private readonly List<(string Label, IAssetReader Reader)> _sources = [];
    private readonly List<IAssetWatcher> _watchers = [];

    // ── Loaders ──────────────────────────────────────────────────
    // Extension → loader (e.g. ".png" → TextureLoader adapter)
    private readonly Dictionary<string, IAssetLoaderUntyped> _loaders = new(StringComparer.OrdinalIgnoreCase);

    // ── Handle tracking ──────────────────────────────────────────
    // path string → (AssetId, AssetType) - deduplication
    private readonly ConcurrentDictionary<string, (AssetId Id, Type AssetType)> _pathToId = new();
    // AssetId → LoadState
    private readonly ConcurrentDictionary<AssetId, LoadState> _states = new();
    // AssetId → AssetPath (for reload/diagnostics)
    private readonly ConcurrentDictionary<AssetId, AssetPath> _idToPath = new();
    // AssetId → list of dependency AssetIds
    private readonly ConcurrentDictionary<AssetId, HashSet<AssetId>> _dependencies = new();

    // ── Background loading ───────────────────────────────────────
    private readonly Channel<LoadRequest> _loadQueue = Channel.CreateUnbounded<LoadRequest>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });
    private readonly ConcurrentQueue<CompletedLoad> _completedLoads = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task[] _workers;

    // ── Configuration ────────────────────────────────────────────
    private bool _watchEnabled;
    private bool _disposed;

    /// <summary>Whether file watching (hot-reload) is enabled.</summary>
    public bool WatchForChanges => _watchEnabled;

    /// <summary>Number of registered asset sources.</summary>
    public int SourceCount => _sources.Count;

    /// <summary>Number of registered loaders.</summary>
    public int LoaderCount => _loaders.Count;

    /// <summary>Number of assets currently tracked (any state).</summary>
    public int TrackedAssetCount => _pathToId.Count;

    /// <summary>
    /// Creates a new <see cref="AssetServer"/> with the specified number of background worker threads.
    /// </summary>
    /// <param name="workerCount">
    /// Number of background threads for async loading. Defaults to
    /// <c>Math.Max(2, Environment.ProcessorCount / 2)</c>.
    /// </param>
    public AssetServer(int? workerCount = null)
    {
        int count = workerCount ?? Math.Max(2, Environment.ProcessorCount / 2);
        _workers = new Task[count];
        for (int i = 0; i < count; i++)
        {
            int id = i;
            _workers[i] = Task.Factory.StartNew(
                () => WorkerLoop(id, _cts.Token),
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
        }

        Logger.Info($"AssetServer created with {count} worker thread(s).");
    }

    // ── Source Registration ──────────────────────────────────────

    /// <summary>
    /// Adds an asset source with an optional label. Sources are probed in registration order.
    /// </summary>
    /// <param name="reader">The asset reader to add.</param>
    /// <param name="label">An optional human-readable label for diagnostics.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public AssetServer AddSource(IAssetReader reader, string? label = null)
    {
        label ??= reader.GetType().Name;
        _sources.Add((label, reader));
        Logger.Info($"Asset source added: '{label}' ({reader.GetType().Name})");
        return this;
    }

    // ── Loader Registration ─────────────────────────────────────

    /// <summary>
    /// Registers a typed asset loader. The loader handles all file extensions declared
    /// by <see cref="IAssetLoader{T}.Extensions"/>.
    /// </summary>
    /// <typeparam name="T">The asset type the loader produces.</typeparam>
    /// <param name="loader">The loader implementation.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public AssetServer RegisterLoader<T>(IAssetLoader<T> loader)
    {
        var adapter = new AssetLoaderAdapter<T>(loader);
        foreach (string ext in loader.Extensions)
        {
            string normalized = ext.StartsWith('.') ? ext : $".{ext}";
            _loaders[normalized] = adapter;
            Logger.Debug($"Loader registered: {normalized} → {loader.GetType().Name} → {typeof(T).Name}");
        }
        return this;
    }

    // ── Loading ──────────────────────────────────────────────────

    /// <summary>
    /// Begins loading an asset from the given path. Returns a handle immediately;
    /// the actual load happens on a background thread. Deduplicates by path.
    /// </summary>
    /// <typeparam name="T">The expected asset type.</typeparam>
    /// <param name="path">
    /// Relative asset path, optionally with a label (e.g. <c>"models/tree.gltf#Mesh0"</c>).
    /// </param>
    /// <returns>A strong <see cref="Handle{T}"/> that will resolve once loading completes.</returns>
    /// <exception cref="InvalidOperationException">No loader registered for the file extension.</exception>
    public Handle<T> Load<T>(string path)
    {
        var assetPath = AssetPath.Parse(path);
        return Load<T>(assetPath);
    }

    /// <summary>
    /// Begins loading an asset from the given <see cref="AssetPath"/>.
    /// </summary>
    /// <typeparam name="T">The expected asset type.</typeparam>
    /// <param name="path">The asset path.</param>
    /// <returns>A strong <see cref="Handle{T}"/>.</returns>
    public Handle<T> Load<T>(AssetPath path)
    {
        string key = path.ToString();

        // Deduplication: return existing handle if already requested
        if (_pathToId.TryGetValue(key, out var existing))
        {
            var handle = new Handle<T>(existing.Id, path, strong: true);
            HandleRefCounts.Increment(existing.Id);
            Logger.Debug($"Load (deduplicated): {path} → {existing.Id}");
            return handle;
        }

        // Validate loader exists
        string ext = path.Extension;
        if (!_loaders.ContainsKey(ext))
            throw new InvalidOperationException($"No asset loader registered for extension '{ext}'. Path: {path}");

        // Allocate ID and register
        var id = AssetId.Next();
        _pathToId[key] = (id, typeof(T));
        _states[id] = LoadState.Loading;
        _idToPath[id] = path;
        HandleRefCounts.Increment(id);

        var handleResult = new Handle<T>(id, path, strong: true);

        // Enqueue for background loading
        _loadQueue.Writer.TryWrite(new LoadRequest(id, path, typeof(T)));
        Logger.Debug($"Load enqueued: {path} → {id}");

        return handleResult;
    }

    /// <summary>
    /// Synchronously loads an asset, blocking the calling thread until the load completes.
    /// Use sparingly - prefer async <see cref="Load{T}(string)"/> in most cases.
    /// </summary>
    /// <typeparam name="T">The expected asset type.</typeparam>
    /// <param name="path">Relative asset path.</param>
    /// <returns>The loaded asset.</returns>
    /// <exception cref="InvalidOperationException">Load failed or no loader registered.</exception>
    public T LoadSync<T>(string path)
    {
        var assetPath = AssetPath.Parse(path);
        string ext = assetPath.Extension;

        if (!_loaders.TryGetValue(ext, out var loader))
            throw new InvalidOperationException($"No asset loader registered for extension '{ext}'. Path: {path}");

        // Find the stream
        Stream? stream = null;
        foreach (var (_, reader) in _sources)
        {
            if (!reader.Exists(assetPath)) continue;
            stream = reader.ReadAsync(assetPath, CancellationToken.None).GetAwaiter().GetResult();
            break;
        }

        if (stream is null)
            throw new FileNotFoundException($"Asset not found in any source: {path}");

        using var ctx = new AssetLoadContext(stream, assetPath, depPath =>
        {
            // Synchronous dependency tracking: just allocate an ID
            string depKey = depPath.ToString();
            if (_pathToId.TryGetValue(depKey, out var dep))
                return dep.Id;
            var depId = AssetId.Next();
            _pathToId[depKey] = (depId, typeof(object));
            _states[depId] = LoadState.NotLoaded;
            _idToPath[depId] = depPath;
            return depId;
        });

        var result = loader.LoadUntypedAsync(ctx, CancellationToken.None).GetAwaiter().GetResult();
        if (!result.Success || result.Asset is null)
            throw new InvalidOperationException($"Failed to load asset '{path}': {result.Error ?? "Unknown error"}");

        if (result.Asset is not T typedAsset)
            throw new InvalidCastException($"Asset '{path}' loaded as {result.Asset.GetType().Name} but expected {typeof(T).Name}");

        // Store in tracking
        var id = AssetId.Next();
        string keyStr = assetPath.ToString();
        _pathToId[keyStr] = (id, typeof(T));
        _states[id] = LoadState.Loaded;
        _idToPath[id] = assetPath;

        Logger.Debug($"LoadSync completed: {path} → {id}");
        return typedAsset;
    }

    /// <summary>Gets the current load state of an asset.</summary>
    /// <param name="id">The asset ID to query.</param>
    /// <returns>The current <see cref="LoadState"/>.</returns>
    public LoadState GetLoadState(AssetId id) =>
        _states.GetValueOrDefault(id, LoadState.NotLoaded);

    /// <summary>Gets the current load state of a handle.</summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="handle">The handle to query.</param>
    /// <returns>The current <see cref="LoadState"/>.</returns>
    public LoadState GetLoadState<T>(Handle<T> handle) => GetLoadState(handle.Id);

    /// <summary>Returns <c>true</c> when the asset and all its dependencies are loaded.</summary>
    /// <param name="id">The asset ID.</param>
    public bool IsLoadedWithDependencies(AssetId id)
    {
        if (GetLoadState(id) != LoadState.Loaded) return false;
        if (!_dependencies.TryGetValue(id, out var deps)) return true;
        foreach (var dep in deps)
        {
            if (GetLoadState(dep) != LoadState.Loaded) return false;
        }
        return true;
    }

    // ── Hot-Reload ───────────────────────────────────────────────

    /// <summary>Enables file watching for hot-reload on all filesystem sources.</summary>
    public void EnableWatching()
    {
        if (_watchEnabled) return;
        _watchEnabled = true;

        foreach (var (label, reader) in _sources)
        {
            var watcher = reader.CreateWatcher();
            if (watcher is null) continue;

            watcher.AssetsChanged += OnAssetsChanged;
            watcher.Start();
            _watchers.Add(watcher);
            Logger.Info($"Hot-reload watcher started for source: '{label}'");
        }
    }

    /// <summary>Disables file watching.</summary>
    public void DisableWatching()
    {
        _watchEnabled = false;
        foreach (var w in _watchers)
            w.Dispose();
        _watchers.Clear();
    }

    private void OnAssetsChanged(AssetPath[] changedPaths)
    {
        foreach (var path in changedPaths)
        {
            string key = path.ToString();
            if (!_pathToId.TryGetValue(key, out var info)) continue;

            // Re-enqueue load for hot-reload
            _states[info.Id] = LoadState.Loading;
            _loadQueue.Writer.TryWrite(new LoadRequest(info.Id, path, info.AssetType, true));
            Logger.Info($"Hot-reload triggered: {path}");
        }
    }

    // ── Frame Processing ─────────────────────────────────────────

    /// <summary>
    /// Drains completed background loads into <see cref="Assets{T}"/> and fires
    /// <see cref="AssetEvent{T}"/>. Called once per frame by the <see cref="AssetPlugin"/>
    /// system in <see cref="Stage.PreUpdate"/>.
    /// </summary>
    /// <param name="world">The world containing asset and event resources.</param>
    public void ProcessCompleted(World world)
    {
        int processed = 0;
        while (_completedLoads.TryDequeue(out var completed))
        {
            processed++;
            if (!completed.Success)
            {
                _states[completed.Id] = LoadState.Failed;
                Logger.Error($"Asset load failed: {completed.Path} - {completed.Error}");
                continue;
            }

            _states[completed.Id] = LoadState.Loaded;

            // Store dependencies
            if (completed.Dependencies is { Count: > 0 })
            {
                var depIds = new HashSet<AssetId>();
                foreach (var dep in completed.Dependencies)
                {
                    if (_pathToId.TryGetValue(dep.ToString(), out var depInfo))
                        depIds.Add(depInfo.Id);
                }
                _dependencies[completed.Id] = depIds;
            }

            // Store in typed Assets<T> and fire events
            StoreAndNotify(world, completed);
        }

        // Check for newly-satisfied dependency trees
        if (processed > 0)
            CheckDependencyCompletion(world);
    }

    private void StoreAndNotify(World world, CompletedLoad completed)
    {
        // Use reflection to call the generic store method for the correct asset type
        var method = typeof(AssetServer)
            .GetMethod(nameof(StoreTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(completed.AssetType);
        method.Invoke(null, [world, completed]);
    }

    private static void StoreTyped<T>(World world, CompletedLoad completed)
    {
        // Ensure Assets<T> resource exists
        var assets = world.GetOrInsertResource(() => new Assets<T>());
        var handle = new Handle<T>(completed.Id, completed.Path, strong: true);

        bool existed = assets.Contains(completed.Id);
        assets.Set(completed.Id, (T)completed.Asset!);

        // Fire event
        var events = Events.Get<AssetEvent<T>>(world);
        events.Send(existed ? AssetEvent<T>.Modified(handle) : AssetEvent<T>.Added(handle));
    }

    private void CheckDependencyCompletion(World world)
    {
        foreach (var kv in _dependencies)
        {
            if (GetLoadState(kv.Key) != LoadState.Loaded) continue;
            if (!IsLoadedWithDependencies(kv.Key)) continue;

            // All deps satisfied - fire LoadedWithDependencies if not already done
            if (!_idToPath.TryGetValue(kv.Key, out var path)) continue;
            if (!_pathToId.TryGetValue(path.ToString(), out var info)) continue;

            // We fire the event via reflection for the correct type
            var method = typeof(AssetServer)
                .GetMethod(nameof(FireDepsLoaded), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(info.AssetType);
            method.Invoke(null, [world, kv.Key, path]);
        }
    }

    private static void FireDepsLoaded<T>(World world, AssetId id, AssetPath path)
    {
        var handle = new Handle<T>(id, path, strong: true);
        Events.Get<AssetEvent<T>>(world).Send(AssetEvent<T>.LoadedWithDependencies(handle));
    }

    /// <summary>
    /// Clears all asset events. Called once per frame at <see cref="Stage.Last"/>
    /// by the <see cref="AssetPlugin"/>.
    /// </summary>
    /// <param name="world">The world containing event resources.</param>
    public void ClearEvents(World world)
    {
        // Clear events for all known asset types
        foreach (var kv in _pathToId.Values)
        {
            var method = typeof(AssetServer)
                .GetMethod(nameof(ClearEventsTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(kv.AssetType);
            method.Invoke(null, [world]);
        }
    }

    private static void ClearEventsTyped<T>(World world)
    {
        if (world.TryGetResource<Events<AssetEvent<T>>>(out var events))
            events.Clear();
    }

    // ── Background Worker ────────────────────────────────────────

    private async Task WorkerLoop(int workerId, CancellationToken ct)
    {
        Logger.Debug($"Asset worker #{workerId} started.");
        try
        {
            await foreach (var request in _loadQueue.Reader.ReadAllAsync(ct))
            {
                try
                {
                    var result = await ExecuteLoad(request, ct);
                    _completedLoads.Enqueue(result);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _completedLoads.Enqueue(new CompletedLoad
                    {
                        Id = request.Id,
                        Path = request.Path,
                        AssetType = request.AssetType,
                        Success = false,
                        Error = ex.Message,
                    });
                }
            }
        }
        catch (OperationCanceledException) { /* expected on shutdown */ }
        Logger.Debug($"Asset worker #{workerId} stopped.");
    }

    private async Task<CompletedLoad> ExecuteLoad(LoadRequest request, CancellationToken ct)
    {
        string ext = request.Path.Extension;
        if (!_loaders.TryGetValue(ext, out var loader))
        {
            return new CompletedLoad
            {
                Id = request.Id,
                Path = request.Path,
                AssetType = request.AssetType,
                Success = false,
                Error = $"No loader for extension '{ext}'",
            };
        }

        // Find stream from sources
        Stream? stream = null;
        foreach (var (_, reader) in _sources)
        {
            if (!reader.Exists(request.Path)) continue;
            stream = await reader.ReadAsync(request.Path, ct);
            break;
        }

        if (stream is null)
        {
            return new CompletedLoad
            {
                Id = request.Id,
                Path = request.Path,
                AssetType = request.AssetType,
                Success = false,
                Error = $"Asset not found in any source: {request.Path}",
            };
        }

        // Track dependencies loaded by this asset
        var dependencies = new List<AssetPath>();
        using var ctx = new AssetLoadContext(stream, request.Path, depPath =>
        {
            dependencies.Add(depPath);
            // Ensure the dependency is queued for loading
            string depKey = depPath.ToString();
            if (_pathToId.TryGetValue(depKey, out var dep))
                return dep.Id;
            var depId = AssetId.Next();
            _pathToId[depKey] = (depId, typeof(object));
            _states[depId] = LoadState.Loading;
            _idToPath[depId] = depPath;
            _loadQueue.Writer.TryWrite(new LoadRequest(depId, depPath, typeof(object)));
            return depId;
        });

        var result = await loader.LoadUntypedAsync(ctx, ct);

        if (!result.Success)
        {
            return new CompletedLoad
            {
                Id = request.Id,
                Path = request.Path,
                AssetType = request.AssetType,
                Success = false,
                Error = result.Error,
            };
        }

        Logger.Debug($"Asset loaded: {request.Path} → {request.Id} ({request.AssetType.Name})");

        return new CompletedLoad
        {
            Id = request.Id,
            Path = request.Path,
            AssetType = request.AssetType,
            Asset = result.Asset,
            SubAssets = result.SubAssets,
            Dependencies = dependencies.Count > 0 ? dependencies : null,
            Success = true,
            IsReload = request.IsReload,
        };
    }

    // ── Diagnostics ──────────────────────────────────────────────

    /// <summary>Returns a snapshot of all tracked asset paths and their load states.</summary>
    public IReadOnlyDictionary<string, LoadState> GetAllStates()
    {
        var result = new Dictionary<string, LoadState>();
        foreach (var kv in _pathToId)
        {
            _states.TryGetValue(kv.Value.Id, out var state);
            result[kv.Key] = state;
        }
        return result;
    }

    // ── Disposal ─────────────────────────────────────────────────

    /// <summary>
    /// Cancels background workers, stops watchers, and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Logger.Info("AssetServer shutting down...");

        DisableWatching();
        _cts.Cancel();
        _loadQueue.Writer.Complete();

        try
        {
            Task.WaitAll(_workers, TimeSpan.FromSeconds(5));
        }
        catch (AggregateException) { /* workers may throw on cancellation */ }

        _cts.Dispose();
        Logger.Info("AssetServer shut down.");
    }

    // ── Internal types ───────────────────────────────────────────

    private readonly record struct LoadRequest(AssetId Id, AssetPath Path, Type AssetType, bool IsReload)
    {
        public LoadRequest(AssetId id, AssetPath path, Type assetType) : this(id, path, assetType, false) { }
    }

    private sealed class CompletedLoad
    {
        public required AssetId Id { get; init; }
        public required AssetPath Path { get; init; }
        public required Type AssetType { get; init; }
        public object? Asset { get; init; }
        public Dictionary<string, object>? SubAssets { get; init; }
        public List<AssetPath>? Dependencies { get; init; }
        public required bool Success { get; init; }
        public string? Error { get; init; }
        public bool IsReload { get; init; }
    }
}
