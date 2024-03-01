#include "Include\Common.hlsli"

cbuffer Properties : register(b10)
{
    // Color
    float4 Color;
    // Header("This is a Header!")
    float Float;
    int Int;
    // Slider(1, 10)
    float Slider;
    // Space
    float2 Float2;
    float3 Float3;
    float4 Float4;
    // Space
    bool Bool;
};

Texture2D texture0 : register(t0);
sampler sampler0 : register(s0);

PSInput VS(VSInput input)
{
    PSInput output;

    output.pos = mul(float4(input.pos, 1), mul(World, ViewProjection));
    output.uv = input.uv;

    return output;
}

float4 PS(PSInput input) : SV_TARGET
{
    float4 col = texture0.Sample(sampler0, input.uv);

    return col;
}