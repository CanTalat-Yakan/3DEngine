using System.Collections.Generic;
using System.IO;

namespace Engine.Utilities
{
    public enum PrimitiveTypes
    {
        Plane,
        Quad,
        Cube,
        Sphere,
        Icosphere,
        Cylinder,
        Torus,
        Tree,
        Suzanne,
        Duck,
    }

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

    public sealed class EntityManager
    {
        public EventList<Entity> EntityList = new();
        public Entity Sky;

        private static Material _materialDefault;
        private static Material _materialSky;
        private static Material _materialSkyLight;

        //private static readonly string SHADER_LIT = @"Lit.hlsl";
        private static readonly string SHADER_SIMPLELIT = @"SimpleLit.hlsl";
        private static readonly string SHADER_UNLIT = @"Unlit.hlsl";

        private static readonly string IMAGE_DEFAULT = @"dark.png";
        private static readonly string IMAGE_SKY = @"SkyGradient.png";
        private static readonly string IMAGE_SKY_LIGHT = @"SkyGradient_Light.png";

        private static readonly string PRIMITIVES = @"Primitives";

        public EntityManager()
        {
            // Create a new material with the default shader and default image.
            if (_materialDefault is null)
                _materialDefault = new(SHADER_SIMPLELIT, IMAGE_DEFAULT);

            // Create a new material with the unlit shader and sky image.
            if (_materialSky is null)
                _materialSky = new(SHADER_UNLIT, IMAGE_SKY);

            // Create a new material with the unlit shader and a light version of the sky image.
            if (_materialSkyLight is null)
                _materialSkyLight = new(SHADER_UNLIT, IMAGE_SKY_LIGHT);
        }

        public Entity Duplicate(Entity refEntity, Entity parent = null)
        {
            // Create a clone of the reference entity.
            Entity clonedEntity = refEntity.Clone();
            // Set the parent of the cloned entity.
            clonedEntity.Parent = parent;

            // Add the cloned entity to the list of entities.
            EntityList.Add(clonedEntity);

            // Return the cloned entity.
            return clonedEntity;
        }

        public Entity CreateEntity(Entity parent = null, string name = "New Entity", string tag = "Untagged")
        {
            // Create a new Entity instance with the specified name, parent, and tag.
            Entity newEntity = new()
            {
                Name = name,
                Parent = parent,
                Tag = tag,
            };

            // Add the new entity to the EntityList.
            EntityList.Add(newEntity);

            // Return the new entity.
            return newEntity;
        }

        public Entity CreatePrimitive(PrimitiveTypes type = PrimitiveTypes.Cube, Entity parent = null)
        {
            // Create a new entity with the specified name and parent.
            Entity newEntity = new()
            {
                Name = type.ToString().FormatString(),
                Parent = parent,
            };

            // Add a mesh component to the entity using the specified primitive type.
            var mesh = (Mesh)newEntity.AddComponent<Mesh>();
            mesh.SetMeshInfo(ModelLoader.LoadFile(Path.Combine(PRIMITIVES, type.ToString()) + ".obj"));

            // Add the new entity to the entity list.
            EntityList.Add(newEntity);

            // Return the new entity.
            return newEntity;
        }

        public Entity CreateCamera(string name = "Camera", string tag = "Untagged", Entity parent = null)
        {
            // Create a new Entity with the given name, parent, and tag.
            Entity newEntity = new()
            {
                Name = name,
                Parent = parent,
                Tag = tag,
            };

            // Add a Camera component to the Entity.
            newEntity.AddComponent<Camera>();

            // Add the new Entity to the EntityList.
            EntityList.Add(newEntity);

            // Return the new Entity.
            return newEntity;
        }

        public void CreateSky()
        {
            // Create new Sky entity.
            Sky = new Entity()
            {
                Name = "Sky", // Set entity name to "Sky".
                Tag = EditorTags.SceneSky.ToString(), // Set entity tag to SceneSky.
            };
            // Set scale of the Sky's transform.
            Sky.Transform.LocalScale = new Vector3(-1000, 1000, 1000);

            // Add Mesh component to Sky entity.
            var skyMesh = (Mesh)Sky.AddComponent<Mesh>();
            skyMesh.SetMeshInfo(ModelLoader.LoadFile(Path.Combine(PRIMITIVES, PrimitiveTypes.Sphere.ToString()) + ".obj"));

            // Set material of Sky's Mesh component.
            skyMesh.SetMaterial(_materialSky);

            // Add Sky entity to EntityList.
            EntityList.Add(Sky);
        }

        public void SetTheme(bool light) =>
            // Switch the material of the sky component to either the default or light sky material.
            Sky.GetComponent<Mesh>().SetMaterial(light ? _materialSkyLight : _materialSky);

        public static MeshInfo GetDefaultMeshInfo() =>
            // Set mesh info to a cube from the resources.
            ModelLoader.LoadFile(Path.Combine(PRIMITIVES, "Cube.obj"));

        public static Material GetDefaultMaterial() =>
            // Set mesh info to a cube from the resources.
            _materialDefault;

        public Entity GetFromID(Guid id)
        {
            // Loop through all entities in the EntityList.
            foreach (var entity in EntityList)
                // Check if the entity is not null.
                if (entity is not null)
                    // Check if the entity's ID matches the given ID.
                    if (entity.ID == id)
                        // Return the entity if its ID matches the given ID.
                        return entity;

            // Return null if the entity is not found.
            return null;
        }

        public Entity GetFromTag(string tag)
        {
            // Iterate over all entities in the EntityList.
            foreach (var entity in EntityList)
                // Check if the current entity is not null.
                if (entity is not null)
                    // Check if the tag of the current entity matches the given tag.
                    if (entity.Tag.ToString() == tag)
                        // Return the entity if a match is found.
                        return entity;

            // Return null if no matching entity is found.
            return null;
        }

        public void Destroy(Entity entity)
        {
            // Remove all components from the entity.
            entity.RemoveComponents();
            // Remove the entity from the entity list.
            EntityList.Remove(entity);
        }
    }
}
