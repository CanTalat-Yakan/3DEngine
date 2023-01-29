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
        public Material Material;

        internal int VertexCount;
        internal int VertexStride;
        internal int IndexCount;
        internal int IndexStride;

        private Renderer _d3d { get => Renderer.Instance; }

        private ID3D11Buffer VertexBuffer;
        private ID3D11Buffer IndexBuffer;

        public override void Register() =>
            MeshSystem.Register(this);

        public Mesh(MeshInfo _obj)
        {
            #region //Set Variables
            VertexCount = _obj.Vertices.Count();
            VertexStride = Unsafe.SizeOf<Vertex>();

            IndexCount = _obj.Indices.Count();
            IndexStride = Unsafe.SizeOf<int>();
            #endregion

            #region //Create VertexBuffer
            VertexBuffer = _d3d.Device.CreateBuffer(
                _obj.Vertices.ToArray(),
                BindFlags.VertexBuffer);
            #endregion

            #region //Create IndexBuffer
            IndexBuffer = _d3d.Device.CreateBuffer(
                _obj.Indices.ToArray(),
                BindFlags.IndexBuffer);
            #endregion
        }

        public override void Render()
        {
            Material.Set(_entity.Transform.ConstantsBuffer);

            _d3d.Draw(
                VertexBuffer, VertexStride,
                IndexBuffer, IndexCount);
        }
    }
}
