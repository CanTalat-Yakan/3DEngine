#version 450

layout(location = 0) in vec2 vUV;
layout(location = 0) out vec4 outColor;

layout(set = 0, binding = 1) uniform sampler2D browserTexture;

void main()
{
    // Sample the browser surface. Ultralight renders BGRA, which we swizzle on upload,
    // so at this point the texture is standard RGBA.
    vec4 texel = texture(browserTexture, vUV);

    // Pre-multiplied alpha compositing – Ultralight outputs pre-multiplied alpha.
    outColor = texel;
}

