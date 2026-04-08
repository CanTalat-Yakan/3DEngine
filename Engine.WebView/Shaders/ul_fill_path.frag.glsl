#version 450

// Ultralight GPU driver - FillPath fragment shader (path/tessellation rendering)

layout(location = 0) in vec4 ex_Color;
layout(location = 1) in vec2 ex_TexCoord;

layout(location = 0) out vec4 outColor;

void main()
{
    // Path rendering uses the color directly - the tessellator outputs
    // coverage in the alpha channel of the vertex color.
    outColor = ex_Color;
}

