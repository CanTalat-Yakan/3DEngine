cbuffer ViewConstantsBuffer : register(b0)
{
    float4x4 ViewProjection;
    float3 Camera;
};

cbuffer PerModelConstantBuffer : register(b1)
{
    float4x4 World;
};

struct VSInput
{
    float4 vertex : POSITION;
    float4 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 uv : UV;
};

struct PSInput
{
    float4 pos : SV_POSITION;
    float3 worldpos : POSITION;
    float3 camerapos : POSITION1;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
    float2 uv : UV;
};