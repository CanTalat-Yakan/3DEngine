cbuffer ViewConstantsBuffer : register(b0)
{
    float4x4 ViewProjection;
    float3 Camera;
};

cbuffer PerModelConstantBuffer : register(b1)
{
    float4x4 World;
};

cbuffer Properties : register(b2)
{
    // Header("This is a Header!")
    float Float;
    int Int;
    // Slider(1, 10)
    int Slider;
    // Space
    float2 Float2;
    float3 Float3;
    // Color
    float4 Float4;
    // Space
    bool Bool;
};

struct appdata
{
    float3 vertex : POSITION;
    float2 uv : TEXCOORD;
    float3 normal : NORMAL;
};

struct VS_OUTPUT
{
    float4 pos : SV_POSITION;
    float3 worldPos : POSITION;
    float3 cameraPos : POSITION1;
    float2 uv : TEXCOORD;
    float3 normal : NORMAL;
};

Texture2D ObjTexture : register(t0);
SamplerState ObjSamplerState : register(s0);

VS_OUTPUT VS(appdata v)
{
    VS_OUTPUT o;
    
    o.pos = mul(float4(v.vertex, 1), mul(World, ViewProjection));
    o.normal = mul(float4(v.normal, 0), World);
    o.worldPos = mul(float4(v.vertex, 1), World);
    o.cameraPos = Camera;
    o.uv = v.uv;

    return o;
}

float4 PS(VS_OUTPUT i) : SV_TARGET
{
    float4 col = ObjTexture.Sample(ObjSamplerState, i.uv);
    float3 diffuse = dot(normalize(i.normal), -normalize(float3(0, -1, 0)));

    diffuse = max(diffuse, float3(0.255, 0.295, 0.3255));

    return col * float4(diffuse, 1);
}
