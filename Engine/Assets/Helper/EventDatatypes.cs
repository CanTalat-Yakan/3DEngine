using System.Collections.Generic;

namespace Engine.Helper;

public sealed class EventList<T> : List<T>
{
    // Event that is raised when an item is added to the list.
    public event EventHandler<T> OnAdd;
    // Event that is raised when an item is removed from the list.
    public event EventHandler<T> OnRemove;

    public void Add(T item, bool invokeEvent = true)
    {
        // Adds an item to the list.
        base.Add(item);

        if (OnAdd is not null)
            if (invokeEvent)
                // Raises the OnAddEvent event.
                OnAdd(this, item);
    }

    public void Remove(T item, bool invokeEvent = true)
    {
        if (OnRemove is not null)
            if (invokeEvent)
                // Raises the OnRemoveEvent event.
                OnRemove(this, item);

        // Removes an item from the list.
        base.Remove(item);
    }
}

public sealed class EventDictionary<TKey, TValue> : Dictionary<TKey, TValue>
{
    // Event that is raised when an item is added to the dictionary.
    public event EventHandler<KeyValuePair<TKey, TValue>> OnAdd;

    // Event that is raised when an item is removed from the dictionary.
    public event EventHandler<KeyValuePair<TKey, TValue>> OnRemove;

    public void Add(TKey key, TValue value, bool invokeEvent = true)
    {
        // Adds an item to the dictionary.
        base.Add(key, value);

        if (OnAdd is not null && invokeEvent)
            // Raises the OnAdd event.
            OnAdd(this, new KeyValuePair<TKey, TValue>(key, value));
    }

    public bool Remove(TKey key, bool invokeEvent = true)
    {
        // Tries to get the value associated with the key.
        if (TryGetValue(key, out TValue value))
        {
            if (OnRemove is not null && invokeEvent)
                // Raises the OnRemove event before removing.
                OnRemove(this, new KeyValuePair<TKey, TValue>(key, value));

            // Removes the item from the dictionary.
            return base.Remove(key);
        }

        return false;
    }

    // You can also override the indexer to raise events when setting or removing items.
    public new TValue this[TKey key]
    {
        get => base[key];
        set
        {
            if (ContainsKey(key))
                // Item exists, so remove the old one first.
                Remove(key, invokeEvent: false);

            // Add the new item and raise the event.
            Add(key, value);
        }
    }
}