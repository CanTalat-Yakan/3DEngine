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