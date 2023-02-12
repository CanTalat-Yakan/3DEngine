using System.Runtime.CompilerServices;
using Vortice.Direct3D11;
using Editor.Controller;
using Engine.Data;
using Engine.ECS;
using Engine.Utilities;

namespace Engine.Components
{
    internal class Mesh : Component
    {
        public MeshInfo MeshInfo;
        public Material Material;

        internal ID3D11Buffer _vertexBuffer;
        internal ID3D11Buffer _indexBuffer;

        internal int _vertexCount => MeshInfo.Vertices.Count;
        internal int _vertexStride => Unsafe.SizeOf<Vertex>();
        internal int _indexCount => MeshInfo.Indices.Count;
        internal int _indexStride => Unsafe.SizeOf<int>();

        private Renderer _d3d => Renderer.Instance;

        public override void OnRegister() =>
            // Register the component with the MeshSystem.
            MeshSystem.Register(this);

        public Mesh(MeshInfo _info)
        {
            //Assign the provided MeshInfo to the local MeshInfo variable.
            MeshInfo = _info;

            //Call the CreateBuffer method to initialize the vertex and index buffer.
            CreateBuffer();
        }

        public override void OnRender()
        {
            // Set the material's constant buffer to the entity's transform constant buffer.
            Material.Set(Entity.Transform.GetConstantBuffer());

            // Draw the mesh using the Direct3D context.
            _d3d.Draw(
                _vertexBuffer, _vertexStride,
                _indexBuffer, _indexCount);

            // Increment the vertex, index and draw call count in the profiler.
            Profiler.Vertices += _vertexCount;
            Profiler.Indices += _indexCount;
            Profiler.DrawCalls++;
        }

        internal void CreateBuffer()
        {
            //Create a VertexBuffer using the MeshInfo's vertices
            //and bind it with VertexBuffer flag.
            _vertexBuffer = _d3d.Device.CreateBuffer(
                MeshInfo.Vertices.ToArray(),
                BindFlags.VertexBuffer);

            //Create an IndexBuffer using the MeshInfo's indices
            //and bind it with IndexBuffer flag.
            _indexBuffer = _d3d.Device.CreateBuffer(
                MeshInfo.Indices.ToArray(),
                BindFlags.IndexBuffer);
        }
    }
}
