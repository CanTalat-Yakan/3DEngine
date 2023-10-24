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

VS_OUTPUT VS(VS_INPUT input)
{
    VS_OUTPUT output;

    output.pos = mul(float4(input.vertex, 1), mul(World, ViewProjection));
    output.normal = mul(float4(input.normal, 0), World);
    output.worldPos = mul(float4(input.vertex, 1), World);
    output.cameraPos = Camera;
    output.uv = input.uv;

    return output;
}

float4 PS(VS_OUTPUT input) : SV_TARGET
{
    float4 col = ObjTexture.Sample(ObjSamplerState, input.uv);

    float3 diffuse = dot(normalize(input.normal), -normalize(float3(0, -1, 0)));

    diffuse = max(diffuse, float3(0.255, 0.295, 0.3255));

    return col * float4(diffuse, 1);
}