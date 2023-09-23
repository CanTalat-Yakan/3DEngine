using System.Collections.Generic;

namespace Engine.Editor
{
    public class Binding
    {
        public static Queue<Guid> GetRemovedScenes => _removedScenes;
        private static Queue<Guid> _removedScenes = new();
        public static Queue<Scene> GetScenes => _addedScenes;
        private static Queue<Scene> _addedScenes = new();

        public static void SetBinding(Scene scene) =>
            _addedScenes.Enqueue(scene);

        public static void Remove(Guid guid) =>
            _removedScenes.Enqueue(guid);

        public static Scene DequeueAddedScenes()
        {
            if (_addedScenes.Count > 0)
                return _addedScenes.Dequeue();
            else
                return null;
        }

        public static Guid? DequeueRemovedScenes()
        {
            if (_removedScenes.Count > 0)
                return _removedScenes.Dequeue();
            else
                return null;
        }
    }
}
