using System.Linq;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;
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

        internal int _vertexCount { get => MeshInfo.Vertices.Count; }
        internal int _vertexStride { get => Unsafe.SizeOf<Vertex>(); }
        internal int _indexCount { get => MeshInfo.Indices.Count; }
        internal int _indexStride { get => Unsafe.SizeOf<int>(); }

        private Renderer _d3d { get => Renderer.Instance; }

        public override void OnRegister() =>
            MeshSystem.Register(this);

        public Mesh(MeshInfo _info)
        {
            MeshInfo = _info;

            CreateBuffer();
        }

        public override void OnRender()
        {
            Material.Set(_entity.Transform.ConstantsBuffer);

            _d3d.Draw(
                _vertexBuffer, _vertexStride,
                _indexBuffer, _indexCount);
        }

        internal void CreateBuffer()
        {
            #region //Create VertexBuffer
            _vertexBuffer = _d3d.Device.CreateBuffer(
                MeshInfo.Vertices.ToArray(),
                BindFlags.VertexBuffer);
            #endregion

            #region //Create IndexBuffer
            _indexBuffer = _d3d.Device.CreateBuffer(
                MeshInfo.Indices.ToArray(),
                BindFlags.IndexBuffer);
            #endregion
        }
    }
}
