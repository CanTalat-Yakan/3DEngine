#version 450

// Fullscreen triangle - no vertex buffer needed.
// Generates a triangle covering the entire screen using gl_VertexIndex.

layout(location = 0) out vec2 vUV;

void main()
{
    // Produces: (0,0), (2,0), (0,2) which covers the [-1,1] NDC quad.
    vUV = vec2((gl_VertexIndex << 1) & 2, gl_VertexIndex & 2);
    gl_Position = vec4(vUV * 2.0 - 1.0, 0.0, 1.0);
    // Vulkan NDC Y points downward, matching Ultralight's top-left origin - no flip needed.
}

