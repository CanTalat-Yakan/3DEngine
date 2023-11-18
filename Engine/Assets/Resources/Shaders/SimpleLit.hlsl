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

Texture2D ObjTexture : register(t0);
SamplerState ObjSamplerState : register(s0);

PSInput VSMain(VSInput input)
{
    PSInput output;

    output.pos = mul(input.vertex, mul(World, ViewProjection));
    output.normal = mul(input.normal, World);
    output.tangent = mul(input.tangent, World);
    output.worldpos = mul(input.vertex, World);
    output.camerapos = Camera;
    output.uv = input.uv;

    return output;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    //float4 col = ObjTexture.Sample(ObjSamplerState, input.uv);
    float3 diffuse = dot(normalize(input.normal), -normalize(float3(0, -1, 0)));

    diffuse = max(diffuse, float3(0.255, 0.295, 0.3255));

    return float4(diffuse, 1);
    //return col * float4(diffuse, 1);
}