using System.Linq;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;
using Engine.Data;
using Engine.Utilities;

namespace Engine.Components
{
    internal class MeshComponent
    {
        public ID3D11Buffer VertexBuffer;
        public ID3D11Buffer IndexBuffer;

        public int VertexCount;
        public int VertexStride;
        public int IndexCount;
        public int IndexStride;

        private Renderer _d3d;

        public MeshComponent(MeshInfo _obj)
        {
            #region //Get Instance of DirectX
            _d3d = Renderer.Instance;
            #endregion

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

        public void Render()
        {
            _d3d.RenderMesh(
                VertexBuffer, VertexStride,
                IndexBuffer, IndexCount);
        }
    }
}
