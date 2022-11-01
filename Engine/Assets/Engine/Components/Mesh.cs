using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;
using Engine.Data;
using Engine.Utilities;

namespace Engine.Components
{
    internal class Mesh
    {
        Renderer d3d;

        internal ID3D11Buffer vertexBuffer;
        internal ID3D11Buffer indexBuffer;

        internal int vertexCount;
        internal int vertexStride;
        internal int indexCount;
        internal int indexStride;


        internal Mesh(MeshInfo _obj)
        {
            #region //Get Instance of DirectX
            d3d = Renderer.Instance;
            #endregion

            #region //Set Variables
            vertexCount = _obj.Vertices.Count();
            vertexStride = Unsafe.SizeOf<Vertex>();

            indexCount = _obj.Indices.Count();
            indexStride = Unsafe.SizeOf<int>();
            #endregion

            #region //Create VertexBuffer
            vertexBuffer = d3d.device.CreateBuffer(
                _obj.Vertices.ToArray(),
                BindFlags.VertexBuffer);
            #endregion

            #region //Create IndexBuffer
            indexBuffer = d3d.device.CreateBuffer(
                _obj.Indices.ToArray(),
                BindFlags.IndexBuffer);
            #endregion
        }

        internal void Render()
        {
            d3d.RenderMesh(
                vertexBuffer, vertexStride,
                indexBuffer, indexCount);
        }
    }
}
