#include "Include\Common.hlsli"

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
    float3 topColor = float3(0.81, 0.89, 0.95);
    float3 middleColor = float3(0.44, 0.51, 0.58);
    float3 bottomColor = float3(0.09, 0.09, 0.09);
    
    float3 viewDir = normalize(input.worldpos - input.camerapos);
    float dotUp = dot(viewDir, float3(0, 1, 0));
    float3 skyColor;

    if (dotUp > 0.5) // Close to top
        skyColor = lerp(middleColor, topColor, (dotUp - 0.5) / 0.9);
    else if (dotUp < -0.7) // Close to bottom
        skyColor = lerp(middleColor, bottomColor, (-dotUp - 0.7) / 0.3);
    else // Middle
        skyColor = middleColor;

    return float4(skyColor, 1);
}