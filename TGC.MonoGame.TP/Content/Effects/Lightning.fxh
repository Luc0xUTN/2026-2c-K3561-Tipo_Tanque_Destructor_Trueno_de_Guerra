float3 LightPosition;
float3 AmbientColor; 
float3 DiffuseColor; 
float3 SpecularColor; 

float KAmbient; 
float KDiffuse;
float KSpecular;
float Shininess; 

float3 CameraPosition;

float4 CalculateBlinnPhong(float3 vPosition, float3 vNormal, float4 color)
{
    float3 vLight = normalize(LightPosition - vPosition); 
    float3 vView = normalize(CameraPosition - vPosition); 
    float3 vHalfway = normalize(vLight + vView);  
    
    float3 ambientLight = KAmbient * AmbientColor;
    
    float3 diffuseLight = KDiffuse * DiffuseColor * saturate(dot(vNormal, vLight)); 
    
    float3 specularLight = KSpecular * SpecularColor * pow(saturate(dot(vHalfway, vNormal)),Shininess);
    
    float4 finalColor = float4(saturate(ambientLight + diffuseLight) * color.rgb + specularLight, color.a); 
    
    return finalColor;
}