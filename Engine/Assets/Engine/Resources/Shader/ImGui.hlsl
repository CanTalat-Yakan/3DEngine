cbuffer vertexBuffer : register(b2)
{
    float4x4 ProjectionMatrix;
};

struct appdata
{
    float2 pos : POSITION;
    float4 col : COLOR0;
    float2 uv  : TEXCOORD0;
};

struct VS_OUTPUT
{
    float4 pos : SV_POSITION;
    float4 col : COLOR0;
    float2 uv  : TEXCOORD0;
};

Texture2D texture0 : register(t0);
sampler sampler0 : register(s0);

VS_OUTPUT VS(appdata v)
{
    VS_OUTPUT o;

    o.pos = mul(ProjectionMatrix, float4(v.pos.xy, 0.f, 1.f));
    o.col = v.col;
    o.uv = v.uv;

    return o;
}

float4 PS(VS_OUTPUT i) : SV_Target
{
    float4 out_col = i.col * texture0.Sample(sampler0, i.uv);

    return out_col;
}