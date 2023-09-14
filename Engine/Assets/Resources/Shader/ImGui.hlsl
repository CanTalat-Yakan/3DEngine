cbuffer vertexBuffer : register(b0) 
{
    float4x4 ProjectionMatrix; 
};

struct VS_INPUT
{
    float2 pos : POSITION;
    float4 col : COLOR0;
    float2 uv  : TEXCOORD0;
};

struct PS_INPUT
{
    float4 pos : SV_POSITION;
    float4 col : COLOR0;
    float2 uv  : TEXCOORD0;
};

sampler sampler0;
Texture2D texture0;

PS_INPUT VS(VS_INPUT i)
{
    PS_INPUT output;
    output.pos = mul(ProjectionMatrix, float4(i.pos.xy, 0.f, 1.f));
    output.col = i.col;
    output.uv  = i.uv;

    return output;
}

float4 PS(PS_INPUT i) : SV_Target
{
    float4 out_col = i.col * texture0.Sample(sampler0, i.uv);
    out_col.a *= i.col.a; // Multiply output alpha by input alpha for transparency

    return out_col;
}