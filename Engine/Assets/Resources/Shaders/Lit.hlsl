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

//// Directional light structure
//struct DirectionalLight
//{
//    float3 direction;
//};

//// Point light structure
//struct PointLight
//{
//    float3 position;
//    float3 direction;
//};

//cbuffer LightingConstantBuffer : register(b2)
//{
//    DirectionalLight directionalLight;
//    PointLight pointLights[];
//};

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
    float3 camPos : POSITION1;
    float2 ruv : POSITION2;
    float2 uv : TEXCOORD;
    float3 normal : NORMAL;
};












//// GGX microfacet distribution function
//float GGX(float3 normal, float3 halfway, float roughness)
//{
//    float alpha = roughness * roughness;
//    float NdotH = saturate(dot(normal, halfway));
//    float denom = NdotH * NdotH * (alpha - 1.0f) + 1.0f;
//    return alpha / (denom * denom);
//}

//// Schlick fresnel function
//float SchlickFresnel(float fresnel, float NdotH)
//{
//    return fresnel + (1.0f - fresnel) * pow(1.0f - NdotH, 5.0f);
//}

//// Helper functions for the Cook-Torrance diffuse lighting model and the 
//float3 CookTorranceDiffuse(float3 normal, float3 lightDir, float3 albedo)
//{
//    // Calculate the diffuse lighting contribution
//    float NdotL = saturate(dot(normal, lightDir));
//    return albedo * NdotL;
//}

//// GGX microfacet specular lighting model
//float3 GGXMicrofacetSpecular(float3 normal, float3 lightDir, float3 cameraDir, float roughness, float fresnel)
//{
//    // Calculate the halfway vector
//    float3 halfway = normalize(lightDir + cameraDir);

//    // Calculate the specular lighting contribution
//    float NdotH = saturate(dot(normal, halfway));
//    float G = GGX(normal, halfway, roughness);
//    float F = SchlickFresnel(fresnel, NdotH);
//    return G * F / (4.0f * dot(normal, lightDir) * dot(normal, cameraDir));
//}

//// Sample reflection in the reflection map
//float3 SampleReflectionMap(float3 reflectionVector, float2 texCoord)
//{
//    // Calculate the texture coordinates to use for the reflection
//    float2 reflectionTexCoord = reflectionVector.xy * 0.5f + 0.5f;

//    // Sample the texture using the reflection texture coordinates
//    return tex2D(gReflectionMapSampler, reflectionTexCoord).rgb;
//}

//// Input and output structures for the vertex and pixel shaders
//struct PBRVertexInput
//{
//    float3 position : POSITION;
//    float3 normal : NORMAL;
//    float2 texCoord : TEXCOORD;
//};

//struct PBRVertexOutput
//{
//    float4 position : SV_POSITION;
//    float3 normal : NORMAL;
//    float2 texCoord : TEXCOORD;
//};

//struct PBRPixelInput
//{
//    float3 position : SV_POSITION;
//    float3 normal : NORMAL;
//    float2 texCoord : TEXCOORD;
//};

//struct PBRPixelOutput
//{
//    float4 color : SV_TARGET;
//};

//// Vertex shader
//PBRVertexOutput PBRVertexShader(PBRVertexInput input)
//{
//    PBRVertexOutput output;

//    // Transform the position and normal from model space to clip space
//    output.position = mul(gWorldViewProj, float4(input.position, 1.0f));
//    output.normal = mul(gWorld, input.normal);

//    // Pass through the texture coordinates
//    output.texCoord = input.texCoord;

//    return output;
//}

//// Pixel shader
//PBRPixelOutput PBRPixelShader(PBRPixelInput input)
//{
//    PBRPixelOutput output;

//    // Initialize the final color to black
//    output.color = float4(0.0f, 0.0f, 0.0f, 1.0f);

//    // Calculate the diffuse and specular lighting contributions for the directional light
//    float3 diffuse = CookTorranceDiffuse(input.normal, directionalLight.direction, gAlbedo);
//    float3 specular = GGXMicrofacetSpecular(input.normal, directionalLight.direction, gCameraDirection, gRoughness, gFresnel);

//    // Add the diffuse and specular lighting contributions for the directional light to the final color
//    output.color += float4(diffuse + specular, 1.0f);

//    // Loop over all of the point lights in the scene
//    for (int i = 0; i < pointLights.Length; i++)
//    {
//        // Calculate the diffuse lighting contribution for this point light
//        diffuse = CookTorranceDiffuse(input.normal, pointLights[i].direction, gAlbedo);

//        // Calculate the specular lighting contribution for this point light
//        specular = GGXMicrofacetSpecular(input.normal, pointLights[i].direction, gCameraDirection, gRoughness, gFresnel);

//        // Add the diffuse and specular lighting contributions for this point light to the final color
//        output.color += float4(diffuse + specular, 1.0f);
//    }

//    return output;
//}

//// Pixel shader
//PBRPixelOutput PBRPixelShader(PBRPixelInput input)
//{
//    PBRPixelOutput output;

//    // Initialize the final color to black
//    output.color = float4(0.0f, 0.0f, 0.0f, 1.0f);

//    // Calculate the reflection vector
//    float3 reflectionVector = reflect(gCameraDirection, input.normal);

//    // Calculate the lighting contribution from the reflection
//    float3 reflection = SampleReflectionMap(reflectionVector, input.texCoord);

//    // Add the reflection contribution to the final color
//    output.color += float4(reflection, 1.0f);

//    // Calculate the diffuse and specular lighting contributions for the directional light
//    float3 diffuse = CookTorranceDiffuse(input.normal, directionalLight.direction, gAlbedo);
//    float3 specular = GGXMicrofacetSpecular(input.normal, directionalLight.direction, gCameraDirection, gRoughness, gFresnel);

//    // Add the diffuse and specular lighting contributions for the directional light to the final color
//    output.color += float4(diffuse + specular, 1.0f);

//    // Loop over all of the point lights in the scene
//    for (int i = 0; i < pointLights.Length; i++)
//    {
//        // Calculate the diffuse lighting contribution for this point light
//        diffuse = CookTorranceDiffuse(input.normal, pointLights[i].direction, gAlbedo);

//        // Calculate the specular lighting contribution for this point light
//        specular = GGXMicrofacetSpecular(input.normal, pointLights[i].direction, gCameraDirection, gRoughness, gFresnel);

//        // Add the diffuse and specular lighting contributions for this point light to the final color
//        output.color += float4(diffuse + specular, 1.0f);
//    }

//    return output;
//}




















float PI = 3.14159265359;

float2 uvReflection(float3 _v, float3 _n)
{
    float3 r = reflect(_v, _n);
    float m = 2.0 * sqrt(
        pow(r.x, 2.0) +
        pow(r.y, 2.0) +
        pow(r.z + 1.0, 2.0));

    float2 result = r.xy / m + 0.5;
    result.y = 1.0 - result.y;


    return result;
}
float distributionGGX(float _NdotH, float _roughness)
{
    float a = _roughness * _roughness;
    float a2 = a * a;
    float denom = _NdotH * _NdotH * (a2 - 1.0) + 1.0;
    denom = PI * denom * denom;

    return a2 / max(denom, 0.0000001); //prevent divide by zero
}
float geometrySmith(float _NdotV, float _NdotL, float _roughness)
{
    float r = _roughness + 1.0;
    float k = (r * r) / 8.0;
    float ggx1 = _NdotV / (_NdotV * (1.0 - k) + k); //Schlick GGX
    float ggx2 = _NdotL / (_NdotL * (1.0 - k) + k);

    return ggx1 * ggx2;
}
float3 fresnelSchlick(float _HdotV, float3 _baseReflectivity)
{
    //baseReflectivity in range 0 to 1
    //return range of baseReflectivity to 1
    //increases as HdotV decreases (more reflectivtiy when surface viewed at larger angles)
    return _baseReflectivity + (1.0 - _baseReflectivity) * pow(1.0 - _HdotV, 5.0);
}
float lightCalculation(float3 L, float3 H, float3 C, VS_OUTPUT i, float3 albedo, float metallic, float roughness, float ao, float3 baseReflectivity)
{
    float3 N = normalize(i.normal);
    float3 V = normalize(i.camPos - i.worldPos);
    
    //reflectance equation
    float3 Lo = float(0.0);
    {
        //Cook-Torrance BRDF
        float NdotV = max(dot(N, V), 0.0000001); //prevent divide by zero
        float NdotL = max(dot(N, L), 0.0000001);
        float HdotV = max(dot(H, L), 0.0);
        float NdotH = max(dot(N, H), 0.0);

        float D = distributionGGX(NdotH, roughness); //larger the more micro-facets alignes to H (normala distribution)
        float G = geometrySmith(NdotV, NdotL, roughness); //smaller the more micro-facets shadowed by othe micro-facets
        float F = fresnelSchlick(HdotV, baseReflectivity); //proportion of specular reflectance

        float3 specular = D * G * F;
        specular /= 4.0 * NdotV * NdotL;

        //for energy conservation, the diffuse and specular light can't
        //be above 1.0 (unless the surface emits light); to preserve this
        //relationship the diffuse component (kD) should equaal 1.0 - kS.
        float3 kD = float(1.0) - F; //F equals kS;

        //multiply kD ny the inverse metalness such thata only non-metals
        //have diffuse lighting, or a liner blend if partly metal
        //(pure metals have no diffuse light).
        kD *= 1.0 - metallic;

        //note tht 1) angel of light to surface affects specular, not just diffuse
        //         2) we mix albedo with diffuse, but not specular
        Lo += (kD * albedo / PI + specular) * C * NdotL;
    }

    return Lo;
}
float directionalLight(VS_OUTPUT i, float3 albedo, float metallic, float roughness, float ao, float3 baseReflectivity)
{
    float3 V = normalize(i.camPos - i.worldPos);
    
    //calculate per-light radiance
    float3 L = -normalize(float3(0.0, -1.0, 0.0));
    float3 H = normalize(V + L);
    float3 C = float(0.9);

    return lightCalculation(L, H, C, i, albedo, metallic, roughness, ao, baseReflectivity);
}
float pointLight(VS_OUTPUT i, float3 albedo, float metallic, float roughness, float ao, float3 baseReflectivity)
{
    float3 V = normalize(i.camPos - i.worldPos);
    
    //calculate per-light radiance
    float3 L = normalize(float(0.0) - i.worldPos);
    float3 H = normalize(V + L);

    float distance = length(float(0.0) - i.worldPos);
    float attenuation = 1.0 / distance * distance;
    float3 radiance = float(0.1) * attenuation;
    
    return lightCalculation(L, H, radiance, i, albedo, metallic, roughness, ao, baseReflectivity);
}

Texture2D ObjTexture : register(t0);
SamplerState ObjSamplerState : register(s0);

VS_OUTPUT VS(appdata v)
{
    VS_OUTPUT o;

    o.pos = mul(float4(v.vertex, 1), mul(ModelView, ViewProjection));
    o.normal = mul(float4(v.normal, 0), ModelView);
    o.worldPos = mul(float4(v.vertex, 1), ModelView);
    o.camPos = WorldCamPos;
    o.uv = v.uv;
    o.ruv = uvReflection(normalize(o.worldPos - WorldCamPos), normalize(v.normal));

    return o;
}

float4 PS(VS_OUTPUT i) : SV_TARGET
{
    float3 reflection = ObjTexture.Sample(ObjSamplerState, i.ruv).rgb;
    //float3 albedo = float3(1, 0.745, 0.235);
    //float3 albedo = float3(0.1295, 0.8545, 0.345);
    float3 albedo = float(1);
    float metallic = 0.5;
    float roughness = 0.5;
    float ao = 1;

    //calculate reflectance at normal incidence; if dia-electric (like plastic) use baseReflectivity
    //of 0.04 and if it's  metal, use the albedo color as baseReflectivity (metallic workflow)
    float3 baseReflectivity = lerp(float(0.04), albedo, metallic);

    //lighting Calculatin
    float3 Lo = float(0.0);
    //Lo += directionalLight(i, albedo, metallic, roughness, ao, baseReflectivity);
    //Lo += pointLight(i, albedo, metallic, roughness, ao, baseReflectivity);


    //ambient Lighting Sky
    float3 N = normalize(i.normal);
    float3 V = normalize(i.camPos - i.worldPos);
    float NdotV = max(dot(N, V), 0.0000001); //prevent divide by zero
    float3 F = fresnelSchlick(NdotV, baseReflectivity);
    float3 kD = (1.0 - F) * (1.0 - metallic);
    float3 diffuse = reflection * albedo * kD;
    float3 ambient = diffuse * ao;

    //ambient Lighting Color
    //float3 ambient = float3(0.255, 0.295, 0.3255) * albedo * reflection;

    float3 color = ambient + Lo;

    //HDR tonemapping
    color = color / (color + float(1.0));
    //gamma correction
    color = pow(color, float(1.0 / 2.2));


    return float4(color, 1.0);
}
