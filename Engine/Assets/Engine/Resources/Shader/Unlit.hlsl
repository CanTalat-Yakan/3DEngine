cbuffer ViewConstantsBuffer : register(b0)
{
    matrix ViewProjection;
    matrix View;
    matrix Projection;
    float3 WorldCamPos;
};
cbuffer PerModelConstantBuffer : register(b1)
{
    matrix ModelView;
};

struct appdata
{
    float3 vertex : POSITION;
    float2 uv : TEXCOORD;
    float3 normal : NORMAL;
};

Texture2D ObjTexture : register(t0);
SamplerState ObjSamplerState : register(s0);

struct VS_OUTPUT
{
    float4 pos : SV_POSITION;
    float3 worldPos : POSITION;
    float3 camPos : POSITION1;
    float2 uv : TEXCOORD;
    float3 normal : NORMAL;
};


VS_OUTPUT VS(appdata v)
{
    VS_OUTPUT o;
    
    o.pos = mul(float4(v.vertex, 1), mul(ModelView, ViewProjection));
    o.normal = mul(float4(v.normal, 0), ModelView);
    o.worldPos = mul(float4(v.vertex, 1), ModelView);
    o.camPos = WorldCamPos;
    o.uv = v.uv;

    return o;
}

float4 PS(VS_OUTPUT i) : SV_TARGET
{
    float4 col = ObjTexture.Sample(ObjSamplerState, i.uv);

    return col;
}
