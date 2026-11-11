
#if OPENGL
    #define VS_PROFILE vs_3_0
    #define PS_PROFILE ps_3_0
#else
    #define VS_PROFILE vs_4_0_level_9_1
    #define PS_PROFILE ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;

float AlphaThreshold = 0.1;
bool UseAlphaTest = false;

texture ModelTexture;

sampler2D TextureSampler = sampler_state
{
    Texture = <ModelTexture>;
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VertexInput
{
    float4 Position : POSITION0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct VertexOutput
{
    float4 Position : POSITION0;
    float Depth : TEXCOORD0;
    float2 TextureCoordinate : TEXCOORD1;
};

VertexOutput ShadowVS(VertexInput input)
{
    VertexOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    float4 clipPosition = mul(viewPosition, Projection);

    output.Position = clipPosition;
    output.Depth = clipPosition.z / clipPosition.w;
    output.TextureCoordinate = input.TextureCoordinate;

    return output;
}

float4 ShadowPS(VertexOutput input) : COLOR0
{
    float alpha = tex2D(TextureSampler, input.TextureCoordinate).a;

    // Si el alpha test está habilitado, descarta los píxeles transparentes.
    clip(lerp(1.0, alpha - AlphaThreshold, UseAlphaTest));

    float depth = input.Depth * 0.5 + 0.5;
    depth = saturate(depth);

    return float4(depth, depth, depth, 1.0);
}

technique ShadowDrawing
{
    pass Pass0
    {
        VertexShader = compile VS_PROFILE ShadowVS();
        PixelShader = compile PS_PROFILE ShadowPS();
    }
}
