#include "Include\Common.hlsli"

Texture2D texture0;
sampler sampler0;

PSInputUI VS(VSInputUI input)
{
    PSInputUI output;

    output.pos = mul(ProjectionMatrix, float4(input.pos.xy, 0.f, 1.f));
    output.col = input.col;
    output.uv = input.uv;

    return output;
}

float4 PS(PSInputUI input) : SV_Target
{
    float4 output = input.col * texture0.Sample(sampler0, input.uv);
    clip(output.a - 0.1);

    return output;
}
