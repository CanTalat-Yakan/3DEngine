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

PSInput VS(VSInput input)
{
    PSInput output;

    output.pos = mul(float4(input.pos, 1), mul(World, ViewProjection));
    output.normal = mul(float4(input.normal, 0), World);
    output.tangent = mul(float4(input.tangent, 0), World);
    output.worldpos = mul(float4(input.pos, 1), World);
    output.camerapos = Camera;
    output.uv = input.uv;

    return output;
}

float4 PS(PSInput input) : SV_TARGET
{
    return float4(0.255, 0.295, 0.3255, 1);
}