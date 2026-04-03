#version 420

layout(binding = 0, std140) uniform Uniforms
{
    vec4 State;
    mat4 Transform;
    vec4 Scalar4[2];
    vec4 Vector[8];
    mat4 Clip[8];
    uint ClipSize;
} _31;

layout(location = 1) out vec2 ex_ObjectCoord;
layout(location = 2) in vec2 in_TexCoord;
layout(location = 0) in vec2 in_Position;
layout(location = 0) out vec4 ex_Color;
layout(location = 1) in vec4 in_Color;

void main()
{
    ex_ObjectCoord = in_TexCoord;
    gl_Position = _31.Transform * vec4(in_Position, 0.0, 1.0);
    ex_Color = in_Color;
}

