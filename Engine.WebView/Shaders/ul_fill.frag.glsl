#version 450

// Ultralight GPU driver - Fill fragment shader (quad rendering)
// Ported from Ultralight AppCore GLSL shaders for Vulkan.

layout(location = 0) in vec4 ex_Color;
layout(location = 1) in vec2 ex_TexCoord;
layout(location = 2) in vec2 ex_ObjCoord;
layout(location = 3) in vec4 ex_Data0;
layout(location = 4) in vec4 ex_Data1;
layout(location = 5) in vec4 ex_Data2;
layout(location = 6) in vec4 ex_Data3;
layout(location = 7) in vec4 ex_Data4;
layout(location = 8) in vec4 ex_Data5;
layout(location = 9) in vec4 ex_Data6;

layout(location = 0) out vec4 outColor;

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

layout(set = 0, binding = 1) uniform sampler2D Texture1;
layout(set = 0, binding = 2) uniform sampler2D Texture2;

// Scalar accessors (8 scalars packed into 2 vec4s)
float Scalar0() { return Scalar[0].x; }
float Scalar1() { return Scalar[0].y; }
float Scalar2() { return Scalar[0].z; }
float Scalar3() { return Scalar[0].w; }
float Scalar4() { return Scalar[1].x; }
float Scalar5() { return Scalar[1].y; }
float Scalar6() { return Scalar[1].z; }
float Scalar7() { return Scalar[1].w; }

// ── Shader types (from UlGpuState.ShaderType logic in Ultralight) ──

// SDF (Signed Distance Field) utilities
float sdRoundRect(vec2 p, vec2 b, float r) {
    vec2 d = abs(p) - b + r;
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0)) - r;
}

float antialias(float d, float width, float median) {
    return smoothstep(median - width, median + width, d);
}

float samp(vec2 uv, float width, float median) {
    return antialias(texture(Texture1, uv).a, width, median);
}

// Clipping
float clipMask() {
    if (ClipSize == 0u) return 1.0;

    float clip = 0.0;
    for (uint i = 0u; i < ClipSize; i++) {
        mat4 clipMat = Clip[i];
        vec2 origin  = clipMat[0].xy;
        vec2 size    = clipMat[0].zw;
        float radius_tl = clipMat[1].x;
        float radius_tr = clipMat[1].y;
        float radius_br = clipMat[1].z;
        float radius_bl = clipMat[1].w;

        vec2 p = ex_ObjCoord - origin;

        // Pick the corner radius based on which quadrant we're in
        float radius;
        if (p.x < size.x * 0.5) {
            radius = (p.y < size.y * 0.5) ? radius_tl : radius_bl;
        } else {
            radius = (p.y < size.y * 0.5) ? radius_tr : radius_br;
        }

        vec2 halfSize = size * 0.5;
        p = p - halfSize;

        float d = sdRoundRect(p, halfSize, radius);
        float alpha = antialias(-d, 0.5, 0.0);

        // Intersect all clip rects
        clip = (i == 0u) ? alpha : min(clip, alpha);
    }
    return clip;
}

void main()
{
    // Determine fill type from ex_Data0.zw (shader sub-type encoding)
    // Ultralight encodes the fill type in data fields.
    // The fill type is encoded in ex_Data0.z:
    //   0 = Solid color
    //   1 = Image (textured)
    //   2 = Pattern (nine-patch or tiled)

    float fillType = ex_Data0.z;
    float clipAlpha = clipMask();

    vec4 color;

    if (fillType < 0.5)
    {
        // ── Fill type 0: Solid color ──
        color = ex_Color;
    }
    else if (fillType < 1.5)
    {
        // ── Fill type 1: Image / texture fill ──
        color = texture(Texture1, ex_TexCoord) * ex_Color;
    }
    else
    {
        // ── Fill type 2: Pattern / nine-patch ──
        vec2 uv = ex_TexCoord;
        color = texture(Texture1, uv);

        // Apply tint
        if (ex_Data0.w > 0.5) {
            color = vec4(ex_Color.rgb * color.a, color.a);
        }
    }

    // Apply clipping
    color.a *= clipAlpha;

    // Pre-multiplied alpha output
    outColor = color;
}

