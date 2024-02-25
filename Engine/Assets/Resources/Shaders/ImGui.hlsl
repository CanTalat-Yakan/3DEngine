#include "Include\Common.hlsli"

sampler sampler0;
Texture2D texture0;

PS_INPUT_UI VS(VS_INPUT_UI input)
{
    PS_INPUT_UI output;

    output.pos = mul(ProjectionMatrix, float4(input.pos.xy, 0.f, 1.f));
    output.col = input.col;
    output.uv = input.uv;

    return output;
}

float4 PS(PS_INPUT_UI input) : SV_Target
{
    float4 output = input.col * texture0.Sample(sampler0, input.uv);
    clip(output.a - 0.1);

    return output;
}
