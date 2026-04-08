#version 450

// Ultralight GPU driver - Fill vertex shader (quad rendering)
// Vertex format: Vbf2F4Ub2F2F28F
//   float[2]  position
//   ubyte[4]  color (normalized)
//   float[2]  tex coord
//   float[2]  object coord
//   float[4]  data0
//   float[4]  data1
//   float[4]  data2
//   float[4]  data3
//   float[4]  data4
//   float[4]  data5
//   float[4]  data6

layout(location = 0) in vec2  in_Position;
layout(location = 1) in vec4  in_Color;
layout(location = 2) in vec2  in_TexCoord;
layout(location = 3) in vec2  in_ObjCoord;
layout(location = 4) in vec4  in_Data0;
layout(location = 5) in vec4  in_Data1;
layout(location = 6) in vec4  in_Data2;
layout(location = 7) in vec4  in_Data3;
layout(location = 8) in vec4  in_Data4;
layout(location = 9) in vec4  in_Data5;
layout(location = 10) in vec4 in_Data6;

layout(location = 0) out vec4 ex_Color;
layout(location = 1) out vec2 ex_TexCoord;
layout(location = 2) out vec2 ex_ObjCoord;
layout(location = 3) out vec4 ex_Data0;
layout(location = 4) out vec4 ex_Data1;
layout(location = 5) out vec4 ex_Data2;
layout(location = 6) out vec4 ex_Data3;
layout(location = 7) out vec4 ex_Data4;
layout(location = 8) out vec4 ex_Data5;
layout(location = 9) out vec4 ex_Data6;

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
    ex_ObjCoord = in_ObjCoord;
    ex_Data0    = in_Data0;
    ex_Data1    = in_Data1;
    ex_Data2    = in_Data2;
    ex_Data3    = in_Data3;
    ex_Data4    = in_Data4;
    ex_Data5    = in_Data5;
    ex_Data6    = in_Data6;

    vec4 pos = Transform * vec4(in_Position, 0.0, 1.0);

    // Map from Ultralight pixel coords to Vulkan NDC [-1,1]
    float vw = State.x;
    float vh = State.y;
    pos.x = (2.0 * pos.x / vw) - 1.0;
    pos.y = (2.0 * pos.y / vh) - 1.0;

    gl_Position = pos;
}

