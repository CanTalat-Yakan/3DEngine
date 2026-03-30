#version 450

layout(set = 0, binding = 0) uniform CameraUniform
{
    mat4 View;
    mat4 Projection;
} uCamera;

vec2 positions[3] = vec2[](
    vec2(0.0, -0.5),
    vec2(0.5, 0.5),
    vec2(-0.5, 0.5)
);

void main()
{
    vec4 worldPos = vec4(positions[gl_VertexIndex], 0.0, 1.0);
    gl_Position = uCamera.Projection * uCamera.View * worldPos;
}

