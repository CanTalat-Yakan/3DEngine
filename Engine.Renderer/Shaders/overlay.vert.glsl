#version 450

layout(location = 0) out vec2 vUV;

// Full-screen triangle from vertex index (no vertex buffer needed).
// Vertices: (−1,−1), (3,−1), (−1,3) — covers the entire clip space.
void main()
{
    vec2 pos = vec2((gl_VertexIndex << 1) & 2, gl_VertexIndex & 2);
    vUV = pos;
    gl_Position = vec4(pos * 2.0 - 1.0, 0.0, 1.0);
}

