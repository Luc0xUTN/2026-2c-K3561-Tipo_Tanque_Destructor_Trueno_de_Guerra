#ifndef SHADOWS_FXH
#define SHADOWS_FXH

float4x4 LightViewProjection;
texture ShadowMap;

sampler ShadowSampler = sampler_state
{
    Texture = <ShadowMap>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

static const float2 PoissonDisk[16] = {
    float2(-0.94201624, -0.39906216), float2(0.94558609, -0.76890725),
    float2(-0.09418410, -0.92938870), float2(0.34495938, 0.29387760),
    float2(-0.91588581, 0.45771432),  float2(-0.81544232, -0.87912464),
    float2(-0.38277543, 0.27676845),  float2(0.97484398, 0.75648379),
    float2(0.44323325, -0.97511554),  float2(0.53742981, -0.47373420),
    float2(-0.26496911, -0.41893023), float2(0.79197514, 0.19090188),
    float2(-0.24188840, 0.99706507),  float2(-0.81409955, 0.91437590),
    float2(0.19984126, 0.78641367),   float2(0.14383161, -0.14100790)
};

float CalculateShadow(float4 lightPosition)
{
    float3 projected = lightPosition.xyz / lightPosition.w;

    float3 shadowCoords;
    shadowCoords.x = projected.x * 0.5 + 0.5;
    shadowCoords.y = -projected.y * 0.5 + 0.5;
    shadowCoords.z = projected.z * 0.5 + 0.5;

    if (shadowCoords.x < 0.0 || shadowCoords.x > 1.0 ||
        shadowCoords.y < 0.0 || shadowCoords.y > 1.0 ||
        shadowCoords.z < 0.0 || shadowCoords.z > 1.0)
    {
        return 0.0;
    }

    float bias = 0.0006;
    float currentDepth = shadowCoords.z - bias;

    float texelSize = 1.0 / 4096.0;
    float spread = texelSize * 2.0;

    float shadow = 0.0;

    [unroll]
    for (int i = 0; i < 16; ++i)
    {
        float2 offset = PoissonDisk[i] * spread;
        float closestDepth = tex2D(ShadowSampler, shadowCoords.xy + offset).r;
        shadow += (currentDepth > closestDepth) ? 1.0 : 0.0;
    }

    return shadow / 16.0;
}

#endif