#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

#include "Lightning.fxh"

float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 InverseTransposeWorld; 


float2 TexturePositions = float2(0,0); 

float Time = 0;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL;
    float2 TextureCoordinate : TEXCOORD0; 
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TextureCoordinate : TEXCOORD0;
    float3 Normal :TEXCOORD1; 
    float4 WorldPosition : TEXCOORD2; 
};

texture ModelTexture;
sampler2D textureSampler = sampler_state
{
    Texture = (ModelTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};



VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    
    float4 worldPosition = mul(input.Position, World);
    
    float4 viewPosition = mul(worldPosition, View);	
    
    output.Position = mul(viewPosition, Projection);
    
    output.WorldPosition = worldPosition;    
    
    output.Normal = mul(input.Normal, (float3x3) InverseTransposeWorld); 
    
    output.TextureCoordinate = input.TextureCoordinate;
    
    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 textureColor = tex2D(textureSampler, input.TextureCoordinate + TexturePositions);
    
    float4 finalcolor = CalculateBlinnPhong(input.WorldPosition.xyz, normalize(input.Normal), textureColor); 
    
    return finalcolor;
}

technique BasicColorDrawing
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
