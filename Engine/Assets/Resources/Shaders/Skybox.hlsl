cbuffer ViewConstantsBuffer : register(b0)
{
    float4x4 ViewProjection;
    float3 Camera;
};

cbuffer PerModelConstantBuffer : register(b1)
{
    float4x4 World;
};

struct appdata
{
    float3 vertex : POSITION;
    
    float2 uv : TEXCOORD;
};

struct VS_OUTPUT
{
    float4 pos : SV_POSITION;
    float3 worldPos : POSITION;
    
    float2 uv : TEXCOORD;
};

//TextureCube SkyboxTexture : register(t0);
Texture2D SkyboxTexture : register(t0);
SamplerState SkyboxSamplerState : register(s0);

VS_OUTPUT VS(appdata v)
{
    VS_OUTPUT o;
    
    o.pos = mul(float4(v.vertex, 1), mul(World, ViewProjection));
    o.worldPos = mul(float4(v.vertex, 1), World);

    o.uv = v.uv;

    return o;
}

float4 PS(VS_OUTPUT i) : SV_TARGET
{
    // Normalize the world position
    float3 normalizedWorldPos = normalize(i.worldPos - Camera);

    // Sample the skybox texture
    //float4 skyColor = SkyboxTexture.Sample(SkyboxSamplerState, normalizedWorldPos);
    float4 skyColor = SkyboxTexture.Sample(SkyboxSamplerState, i.uv);

    return skyColor;
}
