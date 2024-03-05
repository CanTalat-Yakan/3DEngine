#include "Include\Common.hlsli"

Texture2D texture0 : register(t0);
sampler sampler0 : register(s2);

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