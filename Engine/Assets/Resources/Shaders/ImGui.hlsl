#include "Include\Common.hlsli"

Texture2D texture0;
sampler sampler0;

PSInputUI VSMain(VSInputUI input)
{
    PSInputUI output;
    output.pos = mul(ViewProjection, float4(input.vertex.xy, 0.f, 1.f));
    output.col = input.color;
    output.uv = input.texcoord;

    return output;
}

float4 PSMain(PSInputUI input) : SV_Target
{
    //float4 out_col = input.col * texture0.Sample(sampler0, i.uv);

    return float4(1,0,0,1);
}