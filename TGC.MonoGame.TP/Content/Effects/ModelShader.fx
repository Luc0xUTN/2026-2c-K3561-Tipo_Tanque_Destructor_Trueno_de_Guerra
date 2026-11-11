#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

#include "Lightning.fxh"
#include "Shadows.fxh"

float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 InverseTransposeWorld; 


float2 TexturePositions = float2(0,0);

float Time = 0;

// Deformación por impacto de proyectil -- puntos en espacio LOCAL (mismo
// espacio que input.Position, antes de World).
//
// Esto vive en una TÉCNICA APARTE (DeformedDrawing, ver más abajo), no en
// MainVS/BasicColorDrawing -- que queda BYTE A BYTE igual a como estaba
// antes de esta feature. Historial de por qué: cualquier intento de que el
// MISMO vertex shader decidiera en runtime si deformar o no (un uniform
// ImpactCount en 0 para Turret/Cannon/ruedas) terminaba rompiendo esas
// mallas -- primero con un `if` que no se saltaba de verdad, después
// (sospecha descartada) con una máscara 0/1 sin branch, después bajando la
// cantidad de impactos desenrollados. Nada de eso alcanzó: el caño seguía
// sin verse, incluso ANTES de que nadie recibiera un disparo (con
// ImpactCount=0 en TODA la partida), lo que confirma que el problema no
// era la lógica de la deformación en sí, sino que el solo HECHO de que
// Turret/Cannon pasaran por un vertex shader con este código de más
// (aunque matemáticamente fuera un no-op) ya les rompía algo. La única
// forma de que esto sea 100% inofensivo para Turret/Cannon/ruedas es que
// ni siquiera compartan el mismo vertex shader -- de ahí la técnica aparte.
//
// float4, no float3 -- el compilador de SM3.0 (fxc, perfil vs_3_0) tira
// "relative address needs replicate swizzle" al indexar un array de float3
// con una variable de loop (direccionamiento relativo). Con float4 (que
// coincide con el tamaño real de un registro de constantes) compila bien;
// el .w de cada elemento no se usa.
// ImpactCount es float, no int -- un uniform "int" mezclado con los demás
// globals terminó corrompiendo el constant buffer entero en pruebas previas.
//
// ImpactRadius/ImpactStrength son constantes de compilación (no uniforms)
// -- no necesitan variar en tiempo real (se ajustan por feel, no por tanque
// ni por frame), así que quedan hardcodeadas acá en vez de subirse desde
// Tank.cs, evitando sumar más uniforms de los estrictamente necesarios.
#define MAX_IMPACTS 8
float4 ImpactPoints[MAX_IMPACTS];
float ImpactCount;
// Escala del bone Hull de ESTE tanque (ver el comentario de ApplyImpact) --
// un valor fijo que Tank.cs calcula UNA sola vez en LoadContent, no algo
// leído de "World" (que cambia de malla a malla dentro del mismo
// DrawMeshes -- Cannon tiene su propia escala no uniforme, ya documentada
// en GetCanonFiringDirection, y usarla por error acá hacía desaparecer el
// caño).
float ImpactLocalScale;
// Historial de ajuste (por feel, jugando):
// - 0.7 / 0.15 (original): imperceptible -- el radio tan ancho diluía el
//   empuje sobre un área grande, se notaba como una ondulación suave.
// - 0.55 / 0.4: MUY fuerte -- perforaba la chapa del casco (triángulos
//   degenerados, agujero hacia el cielo detrás), no una abolladura.
// - Actual: radio chico (cráter puntual, concentrado) + fuerza moderada
//   (perceptible sin romper la malla). Si sigue sin notarse o se rompe de
//   nuevo, tocar acá.
static const float ImpactRadius = 0.5;
static const float ImpactStrength = 0.22;

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
    float3 Normal : TEXCOORD1; 
    float4 WorldPosition : TEXCOORD2; 
    float4 LightPosition : TEXCOORD3;
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

// Empuja position a lo largo de normal según el impacto MAX_IMPACTS[index],
// si todavía está activo (index < ImpactCount). "index" es siempre una
// constante literal en cada uno de los 8 llamados de MainVS de abajo (nunca
// una variable) -- a propósito: el compilador de efectos de MonoGame para
// SM3.0 no soporta indexar un array uniform con una variable de loop
// ("relative address needs replicate swizzle"), pero si el índice es
// literal en el call site, se resuelve en tiempo de compilación sin
// direccionamiento relativo real.
//
// SIN "if/return" -- a propósito. Ya nos pasó una vez que este compilador
// no respetaba un branch dependiente de datos (el indexado de arriba), y
// cuando eso volvió a pasar acá el caño de los enemigos desaparecía (el
// branch que debía saltear el cálculo para Turret/Cannon no se saltaba de
// verdad, y terminaba corriendo igual). En vez de confiar en que el
// compilador salte el cálculo, se calcula SIEMPRE (es barato) y se
// multiplica el resultado final por una máscara 0/1 -- no hay nada que
// "no saltear": con active=0 el resultado es exactamente position sin
// cambios, sin depender de que ninguna rama se evite.
float3 ApplyImpact(float3 position, float3 normal, int index)
{
    float active = ((float) index < ImpactCount) ? 1.0 : 0.0;

    // El bone Hull (Panzer Y T90 -- confirmado leyendo la matriz cruda,
    // no con Matrix.Decompose, que ya mintió una vez con esto en otra
    // investigación -- ver memoria del proyecto) trae pegada una escala
    // de ~100 en su transform absoluto. input.Position (y por lo tanto
    // ImpactPoints, calculado invirtiendo esa misma matriz en Tank.cs)
    // vive en un espacio ~100 veces más chico que "metros reales".
    // ImpactRadius/ImpactStrength están pensados en metros -- sin
    // corregir esto, el radio queda gigante comparado contra la malla
    // (abolla TODO el casco en vez de un punto) y el desplazamiento, al
    // multiplicarse por esta escala al pasar por World más abajo,
    // termina siendo >10 unidades de error real en vez de los 0.15
    // pensados. ImpactLocalScale (ver su declaración) es ESA escala,
    // fija y propia del Hull -- no se lee de World.
    float localRadius = ImpactRadius / ImpactLocalScale;

    float3 impactPoint = ImpactPoints[index].xyz;
    float dist = distance(position, impactPoint);
    float falloff = saturate(1.0 - dist / localRadius);
    falloff = falloff * falloff * (3.0 - 2.0 * falloff); // smoothstep
    return position - normal * (ImpactStrength / ImpactLocalScale) * falloff * active;
}

// MainVS -- SIN NINGÚN CAMBIO respecto de antes de esta feature. Dibuja
// Turret/Cannon/ruedas/orugas (y el Hull cuando no tiene impactos). No lee
// ni un solo uniform de deformación.
VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    float4 worldPosition = mul(input.Position, World);

    float4 viewPosition = mul(worldPosition, View);

    output.Position = mul(viewPosition, Projection);

    output.WorldPosition = worldPosition;
    output.LightPosition = mul(worldPosition, LightViewProjection);
    output.Normal = mul(input.Normal, (float3x3) InverseTransposeWorld);

    output.TextureCoordinate = input.TextureCoordinate;

    return output;
}

// DeformedVS -- copia de MainVS + el empuje por impacto, en SU PROPIO
// vertex shader (no una rama condicional adentro de MainVS). Tank.cs solo
// selecciona esta técnica al dibujar el Hull de un tanque con impactos
// (ver DeformedDrawing más abajo) -- Turret/Cannon/ruedas nunca pasan por
// acá, ni compilado ni en tiempo de ejecución.
VertexShaderOutput DeformedVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    float3 localPosition = input.Position.xyz;
    float3 localNormal = normalize(input.Normal);

    localPosition = ApplyImpact(localPosition, localNormal, 0);
    localPosition = ApplyImpact(localPosition, localNormal, 1);
    localPosition = ApplyImpact(localPosition, localNormal, 2);
    localPosition = ApplyImpact(localPosition, localNormal, 3);
    localPosition = ApplyImpact(localPosition, localNormal, 4);
    localPosition = ApplyImpact(localPosition, localNormal, 5);
    localPosition = ApplyImpact(localPosition, localNormal, 6);
    localPosition = ApplyImpact(localPosition, localNormal, 7);

    float4 worldPosition = mul(float4(localPosition, 1.0), World);

    float4 viewPosition = mul(worldPosition, View);

    output.Position = mul(viewPosition, Projection);

    output.WorldPosition = worldPosition;
    output.LightPosition = mul(worldPosition, LightViewProjection);
    output.Normal = mul(input.Normal, (float3x3) InverseTransposeWorld);

    output.TextureCoordinate = input.TextureCoordinate;

    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 textureColor = tex2D(textureSampler, input.TextureCoordinate + TexturePositions);

    float4 finalcolor = CalculateBlinnPhong(input.WorldPosition.xyz, normalize(input.Normal), textureColor);

    float shadow = CalculateShadow(input.LightPosition);
    finalcolor.rgb *= (1.0 - shadow * 0.7);

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

technique DeformedDrawing
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL DeformedVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
