using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Engine.Essentials;

public static class ComponentPoolManager
{
    private static readonly Dictionary<Type, object> _pools = new();

    public static GenericPools<T> GetPool<T>() where T : Component
    {
        Type type = typeof(T);
        if (!_pools.TryGetValue(type, out object poolObj))
        {
            // Create a factory method for the type
            Func<T> factoryMethod = CreateFactoryMethod<T>();
            GenericPools<T> pool = new(factoryMethod);

            _pools[type] = pool;

            return pool;
        }

        return (GenericPools<T>)poolObj;
    }

    public static object GetPool(Type type)
    {
        if (!_pools.TryGetValue(type, out object poolObj))
        {
            // Create a factory method for the type
            var factoryMethod = CreateFactoryMethod(type);
            var poolType = typeof(GenericPools<>).MakeGenericType(type);

            poolObj = Activator.CreateInstance(poolType, factoryMethod);

            _pools[type] = poolObj;
        }

        return poolObj;
    }

    private static Func<T> CreateFactoryMethod<T>() where T : Component =>
        () => (T)Activator.CreateInstance(typeof(T));

    private static Func<object> CreateFactoryMethod(Type type) =>
        () => Activator.CreateInstance(type);
}

public class GenericPools<T> where T : class
{
    private readonly ConcurrentQueue<T> _pool;
    private readonly int _batchSize;
    private readonly Func<T> _factoryMethod;

    public GenericPools(Func<T> factoryMethod, int initialBatchSize = 100, int batchSize = 100)
    {
        _factoryMethod = factoryMethod ?? throw new ArgumentNullException(nameof(factoryMethod));
        _batchSize = batchSize;
        _pool = new();

        AllocateBatch(initialBatchSize);
    }

    private void AllocateBatch(int size)
    {
        for (int i = 0; i < size; i++)
        {
            var item = _factoryMethod();

            _pool.Enqueue(item);
        }
    }

    public T Get()
    {
        // Retrieves an object from the pool.
        // If the pool is empty, a new batch is allocated.
        if (!_pool.TryDequeue(out T item))
        {
            AllocateBatch(_batchSize);

            _pool.TryDequeue(out item);
        }

        return item;
    }

    public void Return(T item) =>
        _pool.Enqueue(item);
}
