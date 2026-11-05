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

float3 ModelColor = float3(1, 1, 1);

float Time = 0;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float3 Normal : TEXCOORD0;
	float4 WorldPosition : TEXCOORD1;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
    
	float4 worldPosition = mul(input.Position, World);
    
	float4 viewPosition = mul(worldPosition, View);	
    
	output.Position = mul(viewPosition, Projection);
    
	output.WorldPosition = worldPosition;    
    
	output.Normal = mul(input.Normal, (float3x3) InverseTransposeWorld); 
    
	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 finalColor = CalculateBlinnPhong(input.WorldPosition.xyz, normalize(input.Normal), float4(ModelColor, 1.0));
	return finalColor;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
