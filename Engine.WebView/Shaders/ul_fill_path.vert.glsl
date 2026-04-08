#version 450

// Ultralight GPU driver - FillPath vertex shader (path/tessellation rendering)
// Vertex format: Vbf2F4Ub2F
//   float[2]  position
//   ubyte[4]  color (normalized)
//   float[2]  tex coord

layout(location = 0) in vec2 in_Position;
layout(location = 1) in vec4 in_Color;
layout(location = 2) in vec2 in_TexCoord;

layout(location = 0) out vec4 ex_Color;
layout(location = 1) out vec2 ex_TexCoord;

layout(set = 0, binding = 0) uniform Uniforms {
    vec4  State;        // viewport_width, viewport_height, scale, unused
    mat4  Transform;
    vec4  Scalar[2];    // 8 scalars packed as 2 vec4s
    vec4  Vector[8];
    uint  ClipSize;
    uint  _pad0;
    uint  _pad1;
    uint  _pad2;
    mat4  Clip[8];
};

void main()
{
    ex_Color    = in_Color;
    ex_TexCoord = in_TexCoord;

    vec4 pos = Transform * vec4(in_Position, 0.0, 1.0);

    // Map from Ultralight pixel coords to Vulkan NDC [-1,1]
    float vw = State.x;
    float vh = State.y;
    pos.x = (2.0 * pos.x / vw) - 1.0;
    pos.y = (2.0 * pos.y / vh) - 1.0;

    gl_Position = pos;
}

