using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;

using Engine.Components;
using Engine.ECS;
using Engine.SceneSystem;

namespace Editor.Controller;

internal sealed class TreeEntry
{
    public Guid ID;
    public Guid? ParentID;
    public string Name;

    public TreeViewIconNode IconNode;
}

internal sealed class SceneEntry
{
    public Guid ID;
    public string Name;

    public TreeView TreeView;
    public List<TreeEntry> Hierarchy;
    public ObservableCollection<TreeViewIconNode> DataSource;
    public Grid Content;
}

internal sealed partial class Hierarchy
{
    public SceneEntry SceneEntry;
    public List<SceneEntry> SubsceneEntries;

    private ModelView.Hierarchy _hierarchy;
    private StackPanel _stackPanel;

    private TreeEntry _itemInvoked = null;

    public Hierarchy(ModelView.Hierarchy hierarchy, StackPanel stackPanel)
    {
        _hierarchy = hierarchy;
        _stackPanel = stackPanel;

        SceneEntry = new()
        {
            ID = Engine.Kernel.Instance.SystemManager.MainEntityManager.ID,
            Name = Engine.Kernel.Instance.SystemManager.MainEntityManager.Name,
            Hierarchy = new(),
            DataSource = new()
        };
        SubsceneEntries = new();

        CreateDefaultHierarchy();
    }

    public void SetProperties(TreeView treeView)
    {
        if (treeView.SelectedNode is null)
            return;

        var selectedNode = treeView.SelectedNode;

        DeselectTreeViewNodes();
        treeView.SelectedNode = selectedNode;

        var treeViewIconNode = (TreeViewIconNode)selectedNode.Content;
        var entity = GetEntity(treeViewIconNode.TreeEntry);

        Properties.Set(entity);
    }

    private void CreateDefaultHierarchy()
    {
        _stackPanel.Children.Add(CreateSceneHierarchy(SceneEntry).StackInGrid().WrapInExpander("Scene").AddContentFlyout(CreateRootMenuFlyout()));
        _stackPanel.Children.Add(Helper.CreateSeperator());
        _stackPanel.Children.Add(Helper.CreateButton("Add Subscene", (s, e) => ContentDialogCreateNewSubscene()));
        _stackPanel.Children.Add(CreateSubsceneAndHierarchy(out SceneEntry subsceneEntry)
            .StackInGrid().WrapInExpanderWithToggleButton(ref subsceneEntry.Content, subsceneEntry.ID, subsceneEntry.Name, true)
            .AddContentFlyout(CreateSubRootMenuFlyout(subsceneEntry)));
    }

    public void PopulateTree(SceneEntry sceneEntry)
    {
        Scene scene = Engine.Kernel.Instance.SystemManager.GetFromID(sceneEntry.ID);

        scene.EntityManager.EntityList.OnAdd += (s, e) => AddTreeEntry(sceneEntry, (EntityData)e);
        scene.EntityManager.EntityList.OnRemove += (s, e) => RemoveTreeEntry(sceneEntry, (EntityData)e);

        foreach (var entity in scene.EntityManager.EntityList)
            AddTreeEntry(sceneEntry, entity);
    }

    public void DeselectTreeViewNodes()
    {
        SceneEntry.TreeView.SelectedNode = null;

        foreach (var subsceneTreeView in SubsceneEntries)
            subsceneTreeView.TreeView.SelectedNode = null;
    }

    private Grid[] CreateSceneHierarchy(in SceneEntry sceneEntry, Scene scene = null)
    {
        if (scene is null)
            scene = Engine.Kernel.Instance.SystemManager.MainEntityManager;

        var sceneGrid = new Grid[]
        {
            Helper.CreateTreeView(out sceneEntry.TreeView, _hierarchy.Resources["x_TreeViewIconNodeTemplateSelector"] as TreeViewIconNodeTemplateSelector),
            Helper.CreateButton("Create Entity", (s, e) => scene.EntityManager.CreateEntity() )
        };
        sceneEntry.TreeView.ItemsSource = sceneEntry.DataSource;
        sceneEntry.TreeView.PointerPressed += (s, e) => GetInvokedItemAndSetContextFlyout(s, e);
        sceneEntry.TreeView.Tapped += (s, e) => SetProperties((TreeView)s);
        sceneEntry.TreeView.DragItemsCompleted += (s, e) => SetNewParentTreeEntry((TreeViewIconNode)e.NewParentItem, e.Items.Cast<TreeViewIconNode>().ToArray());

        PopulateTree(sceneEntry);

        return sceneGrid;
    }

    private Grid[] CreateSubsceneAndHierarchy(out SceneEntry subsceneEntry, string name = "Subscene", bool enable = true)
    {
        subsceneEntry = new SceneEntry() { ID = Guid.NewGuid(), Name = name, Hierarchy = new(), DataSource = new() };

        var subscene = Engine.Kernel.Instance.SystemManager.AddSubscene(subsceneEntry.ID, name, enable);

        Binding.SetSceneBindings(subscene);

        var subsceneGrid = CreateSceneHierarchy(subsceneEntry, subscene);

        SubsceneEntries.Add(subsceneEntry);

        return subsceneGrid;
    }

    private TreeEntry AddTreeEntry(SceneEntry sceneEntry, EntityData entity)
    {
        if (entity.IsHidden)
            return null;

        TreeEntry treeEntry = new() { Name = entity.Name, ID = entity.ID };
        treeEntry.IconNode = new() { Name = treeEntry.Name, TreeEntry = treeEntry, IsExpanded = false };
        treeEntry.IconNode.IsActive = true;
        treeEntry.ParentID = entity.Parent is not null ? entity.Parent.ID : null;

        var components = entity.GetComponents();
        treeEntry.IconNode.Camera = components.OfType<Camera>().Any();
        treeEntry.IconNode.Mesh = components.OfType<Mesh>().Any();
        treeEntry.IconNode.ScriptsCount = components.Length;

        sceneEntry.Hierarchy.Add(treeEntry);

        TreeEntry parent;
        if ((parent = GetParent(treeEntry, sceneEntry)) is not null)
            parent.IconNode.Children.Add(treeEntry.IconNode);
        else
            sceneEntry.DataSource.Add(treeEntry.IconNode);

        return treeEntry;
    }

    private void RemoveTreeEntry(SceneEntry sceneEntry, EntityData entity)
    {
        var treeEntry = GetTreeEntry(entity.ID);

        if (treeEntry is null)
            return;

        TreeEntry parent;
        if ((parent = GetParent(treeEntry)) is null)
            sceneEntry.DataSource.Remove(treeEntry.IconNode);
        else
            parent.IconNode.Children.Remove(treeEntry.IconNode);

        sceneEntry.Hierarchy.Remove(treeEntry);
    }
}

internal sealed partial class Hierarchy
{
    private MenuFlyout CreateDefaultMenuFlyout()
    {
        MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Cut", Icon = new SymbolIcon(Symbol.Cut) },
                new MenuFlyoutItem() { Text = "Copy", Icon = new SymbolIcon(Symbol.Copy) },
                new MenuFlyoutItem() { Text = "Paste", Icon = new SymbolIcon(Symbol.Paste) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Rename", Icon = new SymbolIcon(Symbol.Rename) },
                new MenuFlyoutItem() { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Create Entity" },
                new MenuFlyoutItem() { Text = "Create Child Entity" },
            };
        items[0].Click += (s, e) => CopyToClipboard(_itemInvoked.ID, DataPackageOperation.Move);
        items[0].KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.X });
        items[1].Click += (s, e) => CopyToClipboard(_itemInvoked.ID, DataPackageOperation.Copy);
        items[1].KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.C });
        items[2].Click += (s, e) => PasteEntityFromClipboardAsnyc(_itemInvoked.ID);

        items[3].Click += (s, e) => ContentDialogRenameTreeEntry(_itemInvoked);
        items[3].KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.F2 });
        items[4].Click += (s, e) => ContentDialogDeleteTreeEntry(_itemInvoked);
        items[4].KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Delete });

        items[5].Click += (s, e) =>
        {
            var entity = GetEntity(_itemInvoked);
            entity.Scene.EntityManager.CreateEntity(entity.Parent);
        };
        items[6].Click += (s, e) =>
        {
            var entity = GetEntity(_itemInvoked);
            entity.Scene.EntityManager.CreateEntity(entity);
        };

        MenuFlyout menuFlyout = new();
        foreach (var item in items)
        {
            menuFlyout.Items.Add(item);

            if (item.Text == "Paste"
                || item.Text == "Delete")
                menuFlyout.Items.Add(new MenuFlyoutSeparator());
        }

        menuFlyout = AppendDynamicMenuFlyoutSubItems(menuFlyout);

        return menuFlyout;
    }

    private MenuFlyout CreateRootMenuFlyout()
    {
        MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Save", Icon = new SymbolIcon(Symbol.Save) },
                new MenuFlyoutItem() { Text = "Show in Files", Icon = new SymbolIcon(Symbol.Document) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Paste", Icon = new SymbolIcon(Symbol.Paste) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Create Entity" },
            };
        items[2].Click += (s, e) => PasteEntityFromClipboardAsync(SceneEntry);

        items[3].Click += (s, e) => Engine.Kernel.Instance.SystemManager.MainEntityManager.EntityManager.CreateEntity();

        MenuFlyout menuFlyout = new();
        foreach (var item in items)
        {
            menuFlyout.Items.Add(item);

            if (item.Text == "Show in Files"
                || item.Text == "Paste")
                menuFlyout.Items.Add(new MenuFlyoutSeparator());
        }

        menuFlyout = AppendDynamicMenuFlyoutSubItems(menuFlyout, SceneEntry);

        return menuFlyout;
    }

    private MenuFlyout CreateSubRootMenuFlyout(SceneEntry sceneEntry = null)
    {
        MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Save", Icon = new SymbolIcon(Symbol.Save) },
                new MenuFlyoutItem() { Text = "Show in Files", Icon = new SymbolIcon(Symbol.Document) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Paste", Icon = new SymbolIcon(Symbol.Paste) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Rename", Icon = new SymbolIcon(Symbol.Rename) },
                new MenuFlyoutItem() { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Unload" },
                new MenuFlyoutItem() { Text = "Load" },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Create Entity" },
            };
        items[2].Click += (s, e) => PasteEntityFromClipboardAsync(sceneEntry);

        items[3].Click += (s, e) => ContentDialogRenameSubscene(sceneEntry);
        items[3].KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.F2 });
        items[4].Click += (s, e) => ContentDialogDeleteSubscene(sceneEntry);
        items[4].KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Delete });

        items[7].Click += (s, e) => Engine.Kernel.Instance.SystemManager.GetFromID(sceneEntry.ID).EntityManager.CreateEntity();

        MenuFlyout menuFlyout = new();
        foreach (var item in items)
        {
            menuFlyout.Items.Add(item);

            if (item.Text == "Show in Files"
                || item.Text == "Paste"
                || item.Text == "Delete"
                || item.Text == "Load")
                menuFlyout.Items.Add(new MenuFlyoutSeparator());
        }

        menuFlyout = AppendDynamicMenuFlyoutSubItems(menuFlyout, sceneEntry);

        return menuFlyout;
    }

    private MenuFlyout AppendDynamicMenuFlyoutSubItems(MenuFlyout menuFlyout, SceneEntry sceneEntry = null)
    {
        MenuFlyoutItem item;

        MenuFlyoutSubItem objectSubItem = new() { Text = "Mesh" };
        foreach (var type in Enum.GetNames(typeof(PrimitiveTypes)))
        {
            item = new() { Text = type.ToString().FormatString() };
            item.Click += (s, e) =>
            {
                if (sceneEntry is not null)
                    Engine.Kernel.Instance.SystemManager.GetFromID(sceneEntry.ID).EntityManager.CreatePrimitive((PrimitiveTypes)Enum.Parse(typeof(PrimitiveTypes), type));
                else if (_itemInvoked is not null)
                {
                    var entity = GetEntity(_itemInvoked);
                    entity.Scene.EntityManager.CreatePrimitive((PrimitiveTypes)Enum.Parse(typeof(PrimitiveTypes), type), entity);
                }
                else
                    Engine.Kernel.Instance.SystemManager.MainEntityManager.EntityManager.CreatePrimitive((PrimitiveTypes)Enum.Parse(typeof(PrimitiveTypes), type));
            };

            objectSubItem.Items.Add(item);
        }

        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        menuFlyout.Items.Add(objectSubItem);
        menuFlyout.Items.Add(item = new MenuFlyoutItem() { Text = "Camera" });
        item.Click += (s, e) =>
        {
            if (sceneEntry is not null)
                Engine.Kernel.Instance.SystemManager.GetFromID(sceneEntry.ID).EntityManager.CreateCamera();
            else if (_itemInvoked is not null)
            {
                var entity = GetEntity(_itemInvoked);
                entity.Scene.EntityManager.CreateCamera("Camera", Tags.MainCamera.ToString(), entity);
            }
            else
                Engine.Kernel.Instance.SystemManager.MainEntityManager.EntityManager.CreateCamera();
        };

        return menuFlyout;
    }
}

internal sealed partial class Hierarchy
{
    private async void ContentDialogCreateNewSubscene()
    {
        TextBox subsceneName;

        ContentDialog dialog = new()
        {
            XamlRoot = _hierarchy.XamlRoot,
            Title = "Create new Subscene",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = subsceneName = new TextBox() { Text = "Subscene" },
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // \w is equivalent of [0 - 9a - zA - Z_]."
            if (!string.IsNullOrEmpty(subsceneName.Text))
                if (!Regex.Match(subsceneName.Text, @"^[\w\-.]+$").Success)
                {
                    new ContentDialog()
                    {
                        XamlRoot = _hierarchy.XamlRoot,
                        Title = "A subscene can't contain any of the following characters",
                        CloseButtonText = "Close",
                        DefaultButton = ContentDialogButton.Close,
                        Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                    }.CreateDialogAsync();

                    return;
                }

            subsceneName.Text = subsceneName.Text.IncrementNameIfExists(Engine.Kernel.Instance.SystemManager.SubEntityManagers.ToArray().Select(Scene => Scene.Name).ToArray());

            _stackPanel.Children.Add(CreateSubsceneAndHierarchy(out SceneEntry subsceneEntry, subsceneName.Text)
                .StackInGrid().WrapInExpanderWithToggleButton(ref subsceneEntry.Content, subsceneEntry.ID, subsceneName.Text, true)
                .AddContentFlyout(CreateSubRootMenuFlyout(subsceneEntry)));
        }
    }

    private async void ContentDialogRenameSubscene(SceneEntry sceneEntry)
    {
        TextBox fileName;

        ContentDialog dialog = new()
        {
            XamlRoot = _hierarchy.XamlRoot,
            Title = "Rename",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = fileName = new TextBox() { Text = sceneEntry.Name },
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // \w is equivalent of [0 - 9a - zA - Z_]."
            if (!string.IsNullOrEmpty(fileName.Text))
                if (!Regex.Match(fileName.Text, @"^[\w\-.]+$").Success)
                {
                    new ContentDialog()
                    {
                        XamlRoot = _hierarchy.XamlRoot,
                        Title = "An entity can't contain any of the following characters",
                        CloseButtonText = "Close",
                        DefaultButton = ContentDialogButton.Close,
                        Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                    }.CreateDialogAsync();

                    return;
                }

            Scene scene = Engine.Kernel.Instance.SystemManager.GetFromID(sceneEntry.ID);

            scene.Name = fileName.Text;
            sceneEntry.Name = fileName.Text;
        }
    }

    private async void ContentDialogRenameTreeEntry(TreeEntry treeEntry)
    {
        TextBox fileName;

        ContentDialog dialog = new()
        {
            XamlRoot = _hierarchy.XamlRoot,
            Title = "Rename",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = fileName = new TextBox() { Text = treeEntry.Name },
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // \w is equivalent of [0 - 9a - zA - Z_]."
            if (!string.IsNullOrEmpty(fileName.Text))
                if (!Regex.Match(fileName.Text, @"^[\w\-.]+$").Success)
                {
                    new ContentDialog()
                    {
                        XamlRoot = _hierarchy.XamlRoot,
                        Title = "An entity can't contain any of the following characters",
                        CloseButtonText = "Close",
                        DefaultButton = ContentDialogButton.Close,
                        Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                    }.CreateDialogAsync();

                    return;
                }

            GetEntity(treeEntry.ID).Name = fileName.Text;
            treeEntry.Name = fileName.Text;
            treeEntry.IconNode.TreeEntry = treeEntry;
        }
    }

    private async void ContentDialogDeleteSubscene(SceneEntry sceneEntry)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = _hierarchy.XamlRoot,
            Title = "Delete " + sceneEntry.Name,
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            sceneEntry.DataSource.Clear();
            sceneEntry.Hierarchy.Clear();
            sceneEntry.Content.Children.Clear();
            _stackPanel.Children.Remove(sceneEntry.Content);

            Engine.Kernel.Instance.SystemManager.RemoveSubscene(sceneEntry.ID);
        }
    }

    private async void ContentDialogDeleteTreeEntry(TreeEntry treeEntry)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = _hierarchy.XamlRoot,
            Title = "Delete " + treeEntry.Name,
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            SceneEntry sceneEntry = GetSceneEntry(treeEntry);
            Scene scene = Engine.Kernel.Instance.SystemManager.GetFromID(sceneEntry.ID);

            scene.EntityManager.Destroy(GetEntity(treeEntry.ID));

            Binding.Remove(treeEntry.ID);

            foreach (var iconNode in treeEntry.IconNode.Children)
            {
                scene.EntityManager.Destroy(GetEntity(iconNode.TreeEntry.ID));
                sceneEntry.DataSource.Remove(iconNode);
            }

            sceneEntry.DataSource.Remove(treeEntry.IconNode);
        }
    }
}

internal sealed partial class Hierarchy
{
    private TreeEntry GetParent(TreeEntry treeEntry, SceneEntry sceneEntry = null)
    {
        if (treeEntry.ParentID is null)
            return null;

        List<TreeEntry> hierarchy;
        if (sceneEntry is not null)
            hierarchy = sceneEntry.Hierarchy;
        else
            hierarchy = GetSceneEntry(treeEntry).Hierarchy;

        foreach (var entry in hierarchy)
            if (entry.ID == treeEntry.ParentID.Value)
                return entry;

        return null;
    }

    private TreeEntry[] GetChildren(TreeEntry treeEntry, SceneEntry sceneEntry = null)
    {
        List<TreeEntry> list = new List<TreeEntry>();

        List<TreeEntry> hierarchy;
        if (sceneEntry is not null)
            hierarchy = sceneEntry.Hierarchy;
        else
            hierarchy = GetSceneEntry(treeEntry).Hierarchy;

        foreach (var entry in hierarchy)
            if (entry.ParentID is not null)
                if (entry.ParentID.Value == treeEntry.ID)
                    list.Add(entry);

        return list.ToArray();
    }

    private TreeEntry GetTreeEntry(Guid guid, SceneEntry sceneEntry = null)
    {
        if (sceneEntry is not null)
            foreach (var entry in sceneEntry.Hierarchy)
                if (entry.ID == guid)
                    return entry;

        foreach (var entry in SceneEntry.Hierarchy)
            if (entry.ID == guid)
                return entry;

        if (SubsceneEntries is not null)
            foreach (var subSceneEntry in SubsceneEntries)
                foreach (var entry in subSceneEntry.Hierarchy)
                    if (entry.ID == guid)
                        return entry;

        return null;
    }

    private SceneEntry GetSceneEntry(TreeEntry treeEntry)
    {
        foreach (var entry in SceneEntry.Hierarchy)
            if (entry.ID == treeEntry.ID)
                return SceneEntry;

        if (SubsceneEntries is not null)
            foreach (var subSceneEntry in SubsceneEntries)
                foreach (var entry in subSceneEntry.Hierarchy)
                    if (entry.ID == treeEntry.ID)
                        return subSceneEntry;

        return null;
    }

    public void GetEntries(out TreeEntry treeEntry, out SceneEntry sceneEntry, Guid guid)
    {
        treeEntry = null;
        sceneEntry = null;

        foreach (var entry in SceneEntry.Hierarchy)
            if (entry.ID == guid)
            {
                treeEntry = entry;
                sceneEntry = SceneEntry;

                return;
            }

        if (SubsceneEntries is not null)
            foreach (var subSceneEntry in SubsceneEntries)
                foreach (var entry in subSceneEntry.Hierarchy)
                    if (entry.ID == guid)
                    {
                        treeEntry = entry;
                        sceneEntry = subSceneEntry;

                        return;
                    }
    }

    /// <summary>
    /// This method sets the parent TreeEntry of the given TreeViewIconNodes to the given newParent,
    /// and updates the parent Entity of each affected Entity accordingly.
    /// </summary>
    public void SetNewParentTreeEntry(TreeViewIconNode newParent, params TreeViewIconNode[] treeViewIconNodes)
    {
        // If newParent is null, do nothing.
        if (newParent is null)
            return;

        // Iterate over the given TreeViewIconNodes and update their parent TreeEntry and parent Entity.
        foreach (var node in treeViewIconNodes)
        {
            (node.TreeEntry).ParentID = (newParent.TreeEntry).ID;
            GetEntity(node.TreeEntry).Parent = GetEntity(newParent.TreeEntry);
        }
    }

    private void GetInvokedItemAndSetContextFlyout(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint((UIElement)sender).Properties;
        if (properties.IsRightButtonPressed)
        {
            var dc = ((FrameworkElement)e.OriginalSource).DataContext;
            if (dc is null)
                return;

            _itemInvoked = ((TreeViewIconNode)dc).TreeEntry;

            ((TreeView)sender).ContextFlyout = CreateDefaultMenuFlyout();
            ((TreeView)sender).ContextFlyout.Opened += (s, e) => ((TreeView)sender).ContextFlyout = null;
        }
    }
}

internal sealed partial class Hierarchy
{
    /// <summary>
    /// This method returns an Entity object with the specified GUID, optionally searching in a specific SceneEntry.
    /// </summary>
    public EntityData GetEntity(Guid guid, SceneEntry sceneEntry = null)
    {
        EntityData entity;

        // If sceneEntry is specified, get the entity with the given GUID from that scene.
        if (sceneEntry is not null)
            entity = Engine.Kernel.Instance.SystemManager.GetFromID(sceneEntry.ID).EntityManager.GetFromID(guid);
        // Otherwise, search through all subscenes of the scene manager.
        else
        {
            entity = Engine.Kernel.Instance.SystemManager.MainEntityManager.EntityManager.GetFromID(guid);

            if (entity is null)
                foreach (var subscene in Engine.Kernel.Instance.SystemManager.SubEntityManagers)
                    if (entity is null)
                        entity = subscene.EntityManager.GetFromID(guid);
                    else break;
        }

        return entity;
    }

    /// <summary>
    /// This method returns an Entity object corresponding to the given TreeEntry and optionally searching in a specific SceneEntry.
    /// </summary>
    public EntityData GetEntity(TreeEntry entry, SceneEntry sceneEntry = null)
    {
        // If entry is null, return null.
        if (entry is null)
            return null;

        // Return the entity with the ID of the given TreeEntry, optionally searching in the specified SceneEntry.
        return GetEntity(entry.ID, sceneEntry);
    }

    /// <summary>
    /// This method retrieves an Entity object corresponding to the given TreeEntry and optionally searching in a specific SceneEntry,
    /// and stores it in the out parameter 'entity'.
    /// </summary>
    public void GetEntity(out EntityData entity, TreeEntry entry, SceneEntry sceneEntry = null) =>
        entity = GetEntity(entry.ID, sceneEntry);

    /// <summary>
    /// This method retrieves an Entity object with the specified GUID, optionally searching in a specific SceneEntry,
    /// and stores it in the out parameter 'entity'.
    /// </summary>
    public void GetEntity(out EntityData entity, Guid guid, SceneEntry sceneEntry = null) =>
        entity = GetEntity(guid, sceneEntry);

    /// <summary>
    /// This method retrieves two Scene objects corresponding to the given SceneEntry objects, and stores them in the out parameters 'sourceScene' and 'targetScene'.
    /// </summary>
    public void GetScenes(out Scene sourceScene, out Scene targetScene, SceneEntry sourceSceneEntry, SceneEntry targetSceneEntry)
    {
        sourceScene = null;
        targetScene = null;

        if (sourceSceneEntry is not null)
            sourceScene = Engine.Kernel.Instance.SystemManager.GetFromID(sourceSceneEntry.ID);
        if (targetSceneEntry is not null)
            targetScene = Engine.Kernel.Instance.SystemManager.GetFromID(targetSceneEntry.ID);
    }
}

internal sealed partial class Hierarchy
{
    /// <summary>
    /// This method copies the given GUID to the clipboard with the given operation.
    /// </summary>
    private void CopyToClipboard(Guid guid, DataPackageOperation requestedOpertion)
    {
        // Create a new DataPackage object and set its text to the given GUID.
        DataPackage data = new();
        data.SetText(guid.ToString());

        // Set the requested operation for the DataPackage.
        data.RequestedOperation = requestedOpertion;

        // Set the clipboard content to the DataPackage.
        Clipboard.SetContent(data);
    }

    /// <summary>
    /// This method resets the clipboard to only paste an entity once.
    /// </summary>
    private void ResetClipboard()
    {
        // Create a new DataPackage object and set its text to the given GUID.
        DataPackage data = new();

        // Set the requested operation for the DataPackage.
        data.RequestedOperation = DataPackageOperation.None;

        // Set the clipboard content to the DataPackage.
        Clipboard.SetContent(data);
    }

    /// <summary>
    /// This method pastes an entity from the clipboard to an Entity with the given Guid.
    /// </summary>
    public async void PasteEntityFromClipboardAsnyc(Guid guid)
    {
        // Get the content from the clipboard.
        DataPackageView dataPackageView = Clipboard.GetContent();

        // Check if the content has text.
        if (dataPackageView.Contains(StandardDataFormats.Text))
        {
            // Get the text content.
            var sourceText = await dataPackageView.GetTextAsync();

            // Check if the text is a valid GUID.
            if (Guid.TryParse(sourceText, out Guid sourceGuid))
                // Call the PasteEntity method to paste the entity.
                PasteEntity(sourceGuid, guid, dataPackageView.RequestedOperation);
        }
    }

    /// <summary>
    /// This method pastes an entity from clipboard to a specific scene.
    /// </summary>
    public async void PasteEntityFromClipboardAsync(SceneEntry sceneEntry)
    {
        // Get the content from the clipboard.
        DataPackageView dataPackageView = Clipboard.GetContent();

        // Check if the content has text.
        if (dataPackageView.Contains(StandardDataFormats.Text))
        {
            // Get the text content.
            var sourceText = await dataPackageView.GetTextAsync();

            // Check if the text is a valid GUID.
            if (Guid.TryParse(sourceText, out Guid sourceGuid))
                // Call the PasteEntity method to paste the entity.
                PasteEntity(sourceGuid, sceneEntry, dataPackageView.RequestedOperation);
        }
    }

    /// <summary>
    /// This method is used to paste an entity into a target entity as a child with a requested operation.
    /// </summary>
    public void PasteEntity(Guid sourceEntityGuid, Guid targetEntityGuid, DataPackageOperation requestedOperation)
    {
        // Prevent pasting the entity into itself.
        if (sourceEntityGuid.Equals(targetEntityGuid))
            return;

        // Get the source tree entry, entity and target tree entry from their respective GUIDs.
        GetEntries(out TreeEntry sourceTreeEntry, out SceneEntry sourceSceneEntry, sourceEntityGuid);
        GetEntity(out EntityData sourceEntity, sourceEntityGuid, sourceSceneEntry);

        GetEntries(out TreeEntry targetTreeEntry, out SceneEntry targetSceneEntry, targetEntityGuid);
        GetEntity(out EntityData targetEntity, targetEntityGuid, targetSceneEntry);

        // Get the source and target scenes from their respective entries.
        GetScenes(out Scene sourceScene, out Scene targetScene, sourceSceneEntry, targetSceneEntry);

        // Check if every information for the operation is obtained.
        if (sourceTreeEntry is null ||
            sourceSceneEntry is null ||
            sourceEntity is null ||
            targetTreeEntry is null ||
            targetSceneEntry is null ||
            targetEntity is null)
        {
            Output.Log($"""
                Couldn't {requestedOperation} from one entity to another.
                    Source TreeEntry: {sourceTreeEntry?.Name}
                    Source SceneEntry: {sourceSceneEntry?.Name}
                    Source Entity: {sourceEntity?.Name}

                    Target TreeEntry: {targetTreeEntry?.Name}
                    Target SceneEntry: {targetSceneEntry?.Name}
                    Target Entity: {targetEntity?.Name}
                """);

            return;
        }

        // Check if the source entity is not null.
        if (sourceEntity is not null)
        {
            // Check the requested operation.
            if (requestedOperation == DataPackageOperation.Move)
            {
                // Migrate the icon node and entity recursively to the target scene.
                MigrateIconNodeBetweenTreeEntries(sourceTreeEntry, sourceSceneEntry, targetTreeEntry, null);
                MigrateEntityBetweenScenesRecursively(sourceScene, targetScene, sourceTreeEntry);

                // Iterate through each child icon node of the source tree entry.
                foreach (var childIconNode in sourceTreeEntry.IconNode.Children)
                    MigrateTreeEntryBetweenHierarchies(childIconNode.TreeEntry, sourceSceneEntry, sourceTreeEntry, null);

                // Move the source entity to the target entity.
                sourceTreeEntry.ParentID = targetTreeEntry.ID;
                sourceEntity.Parent = targetEntity;

                ResetClipboard();
            }
            else if (requestedOperation == DataPackageOperation.Copy)
            {
                // Duplicate the source entity to the target entity and get the new tree entry.
                var newEntity = Engine.Kernel.Instance.SystemManager.GetFromID(sourceSceneEntry.ID).EntityManager.Duplicate(sourceEntity, targetEntity);
                var newTreeEntry = GetTreeEntry(newEntity.ID, targetSceneEntry);

                // Iterate through each child icon node of the source tree entry.
                foreach (var childIconNode in sourceTreeEntry.IconNode.Children)
                {
                    // Get the child entity for the child icon node.
                    GetEntity(out EntityData childEntity, sourceScene.EntityManager.GetFromID(childIconNode.TreeEntry.ID).ID, sourceSceneEntry);
                    // Recursively paste the child entity and its children to the new entity.
                    PasteEntity(childEntity.ID, newEntity.ID, DataPackageOperation.Copy);
                }

                // Migrate the icon node and entity recursively to the target scene.
                MigrateIconNodeBetweenTreeEntries(newTreeEntry, sourceSceneEntry, targetTreeEntry, null);
                MigrateEntityBetweenScenesRecursively(sourceScene, targetScene, newTreeEntry);
            }
        }
    }

    /// <summary>
    /// This method is used to paste an entity into a target scene entry with a requested operation.
    /// </summary>
    public void PasteEntity(Guid sourceEntityGuid, SceneEntry targetSceneEntry, DataPackageOperation requestedOperation)
    {
        // Get the source tree entry, entity and target tree entry from their respective GUIDs.
        GetEntries(out TreeEntry sourceTreeEntry, out SceneEntry sourceSceneEntry, sourceEntityGuid);
        GetEntity(out EntityData sourceEntity, sourceEntityGuid, sourceSceneEntry);

        // Get the source and target scene.
        GetScenes(out Scene sourceScene, out Scene targetScene, sourceSceneEntry, targetSceneEntry);

        // Check if every information for the operation is obtained.
        if (sourceTreeEntry is null ||
            sourceSceneEntry is null ||
            sourceEntity is null)
        {
            Output.Log($"""
                Couldn't {requestedOperation} entity to another scene!
                    Source TreeEntry: {sourceTreeEntry?.Name}
                    Source SceneEntry: {sourceSceneEntry?.Name}
                    Source Entity: {sourceEntity?.Name}
                """);

            return;
        }

        // Check if the source entity exists.
        if (sourceEntity is not null)
            // If the requested operation is a move.
            if (requestedOperation == DataPackageOperation.Move)
            {
                // Migrate the icon node and entity recursively to the target scene.
                MigrateIconNodeBetweenTreeEntries(sourceTreeEntry, sourceSceneEntry, null, targetSceneEntry);
                MigrateEntityBetweenScenesRecursively(sourceScene, targetScene, sourceTreeEntry);

                // Iterate through each child icon node of the source tree entry.
                foreach (var childIconNode in sourceTreeEntry.IconNode.Children)
                    MigrateTreeEntryBetweenHierarchies(childIconNode.TreeEntry, sourceSceneEntry, sourceTreeEntry, null);

                // Set the Parent and IDparent of the sourceTreeEntry to null.
                sourceTreeEntry.ParentID = null;
                sourceEntity.Parent = null;

                ResetClipboard();
            }
            // If the requested operation is a copy.
            else if (requestedOperation == DataPackageOperation.Copy)
            {
                // Duplicate the source entity to the target scene and get the new tree entry.
                var newEntity = Engine.Kernel.Instance.SystemManager.GetFromID(sourceSceneEntry.ID).EntityManager.Duplicate(sourceEntity);
                var newTreeEntry = GetTreeEntry(newEntity.ID, targetSceneEntry);

                // Iterate through each child icon node of the source tree entry.
                foreach (var childIconNode in sourceTreeEntry.IconNode.Children)
                {
                    // Get the child entity for the child icon node.
                    GetEntity(out EntityData childEntity, sourceScene.EntityManager.GetFromID(childIconNode.TreeEntry.ID).ID, sourceSceneEntry);
                    // Recursively paste the child entity and its children to the new entity.
                    PasteEntity(childEntity.ID, newEntity.ID, DataPackageOperation.Copy);
                }

                // Migrate the icon node and entity recursively to the target scene.
                MigrateIconNodeBetweenTreeEntries(newTreeEntry, sourceSceneEntry, null, targetSceneEntry);
                MigrateEntityBetweenScenesRecursively(sourceScene, targetScene, newTreeEntry);
            }
    }
}

internal sealed partial class Hierarchy
{
    /// <summary>
    /// This method is used to migrate an icon node to a new parent and scene.
    /// </summary>
    public void MigrateIconNodeBetweenTreeEntries(TreeEntry sourceTreeEntry, SceneEntry sourceSceneEntry, TreeEntry targetTreeEntry, SceneEntry targetSceneEntry)
    {
        // Check if the scenes are different, since there is no need to migrate entities if they are in the same scene.
        if (sourceSceneEntry.Equals(targetSceneEntry))
            return;

        // Get the parent of the source tree entry's icon node.
        var parent = GetParent(sourceTreeEntry);

        // If the parent is not null, remove the source tree entry's icon node from the parent's children list.
        if (parent is not null)
            parent.IconNode.Children.Remove(sourceTreeEntry.IconNode);
        // If the parent is null, remove the source tree entry's icon node from the source scene entry's data source.
        else
            sourceSceneEntry.DataSource.Remove(sourceTreeEntry.IconNode);

        // If the target tree entry is not null, add the source tree entry's icon node to the target tree entry's children list.
        if (targetTreeEntry is not null)
            targetTreeEntry.IconNode.Children.Add(sourceTreeEntry.IconNode);
        // and if the target scene entry is not null, add the source tree entry's icon node to the target scene entry's root list.
        else if (targetSceneEntry is not null)
            targetSceneEntry.DataSource.Add(sourceTreeEntry.IconNode);

        // Add the tree entry into the hierarchy of the respective scene entry.
        sourceSceneEntry.Hierarchy.Remove(sourceTreeEntry);
        if (targetTreeEntry is not null)
            GetSceneEntry(targetTreeEntry).Hierarchy.Add(sourceTreeEntry);
        // and if the target scene entry is not null, add the source tree entry to the target scene entry's hierarchy.
        else if (targetSceneEntry is not null)
            targetSceneEntry.Hierarchy.Add(sourceTreeEntry);
    }

    /// <summary>
    /// This method migrates entities from a source scene to a target scene recursively,
    /// updating the entity lists and the scene of the migrated entities as needed.
    /// </summary>
    public void MigrateEntityBetweenScenesRecursively(Scene sourceScene, Scene targetScene, params TreeEntry[] treeEntries)
    {
        // Check if the scenes are different, since there is no need to migrate entities if they are in the same scene.
        if (sourceScene.Equals(targetScene))
            return;

        // Iterate over the list of tree entries provided.
        foreach (var treeEntry in treeEntries)
        {
            // If the current tree entry has children, call this method recursively with the child tree entries.
            if (treeEntry.IconNode.Children.Count != 0)
                MigrateEntityBetweenScenesRecursively(
                    sourceScene,
                    targetScene,
                    treeEntry.IconNode.Children
                        .Select(TreeViewIconNode => TreeViewIconNode.TreeEntry)
                        .ToArray());

            // Add the current entity to the target scene's entity list, removing it from the source scene's entity list.
            targetScene.EntityManager.EntityList.Add(sourceScene.EntityManager.GetFromID(treeEntry.ID), false);
            sourceScene.EntityManager.EntityList.Remove(sourceScene.EntityManager.GetFromID(treeEntry.ID), false);

            // Update the migrated entity's scene reference to the target scene.
            targetScene.EntityManager.GetFromID(treeEntry.ID).Scene = targetScene;
        }
    }

    public void MigrateTreeEntryBetweenHierarchies(TreeEntry sourceTreeEntry, SceneEntry sourceSceneEntry, TreeEntry targetTreeEntry, SceneEntry targetSceneEntry)
    {
        // Add the tree entry into the hierarchy of the respective scene entry.
        sourceSceneEntry.Hierarchy.Remove(sourceTreeEntry);

        // If the target tree entry is not null, add the source tree entry to the target tree entry's hierarchy,
        if (targetTreeEntry is not null)
            GetSceneEntry(targetTreeEntry).Hierarchy.Add(sourceTreeEntry);
        // and if the target scene entry is not null, add the source tree entry to the target scene entry's hierarchy.
        else if (targetSceneEntry is not null)
            targetSceneEntry.Hierarchy.Add(sourceTreeEntry);

        // Iterate through each child icon node of the source tree entry.
        foreach (var childIconNode in sourceTreeEntry.IconNode.Children)
            MigrateTreeEntryBetweenHierarchies(childIconNode.TreeEntry, sourceSceneEntry, sourceTreeEntry, null);
    }
}
