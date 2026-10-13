using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Tanks;

namespace TGC.MonoGame.TP.Cameras;

// Responsabilidad: seguir al tanque y definir hacia dónde se está apuntando.
// Es la única que consume el mouse: el juego le pasa la puntería resultante a la torreta y al cañón.
//
// Cámara : el mouse mueve libremente la puntería (el retículo), y la cámara se
// acomoda detrás del tanque "persiguiendo" esa puntería con retraso. Mientras la cámara no llegó,
// el retículo queda corrido del centro; cuando la cámara se alinea, el retículo vuelve al centro.
// La torreta y el cañón también persiguen la puntería, todavía más lento.
//
// La posición de la cámara sólo depende del yaw y de una altura fija (nunca del pitch), así que
// apuntar hacia arriba no la hunde en el terreno.
public class OrbitalCamera : BaseCamera
{
    // Altura de la cámara sobre la base del tanque. No depende del pitch.
    private const float CameraHeight = 3f;

    // Distancia mínima entre la cámara y la superficie del terreno.
    private const float MinGroundClearance = 2f;

    // Distancia (métrica, sólo importa la dirección) hacia la que mira la cámara.
    private const float LookDistance = 100f;

    // Qué tan rápido la cámara alcanza a la puntería (1/s). Más alto = menos retraso.
    private const float FollowSharpness = 6f;

    // La cámara no acompaña toda la elevación: si no, al apuntar muy arriba el tanque se va de
    // cuadro. Hasta este ángulo el retículo queda centrado; más allá, queda pegado al borde.
    private const float MaxCameraPitch = 0.25f;

    private readonly Entity _target;

    private readonly float _radius;

    private readonly float _centerX;
    private readonly float _centerY;

    // Permite consultar la altura real del terreno para no dejar que la cámara lo atraviese.
    private readonly Func<float, float, float> _getTerrainHeight;

    private float _sensitivity;

    // Orientación real de la cámara, retrasada respecto de la puntería (Yaw/Pitch).
    private float _cameraYaw;
    private float _cameraPitch;

    public OrbitalCamera(Entity target, float radius, float centerX, float centerY, float sensitivity,
        Func<float, float, float> getTerrainHeight)
    {
        _target = target;
        _radius = radius;
        _centerX = centerX;
        _centerY = centerY;
        _sensitivity = sensitivity;
        _getTerrainHeight = getTerrainHeight;
    }

    public override void Initialize()
    {
        _cameraYaw = Yaw;
        _cameraPitch = Pitch;
        this.UpdateView();
    }

    // Dirección (unitaria) a la que apunta una cámara con el yaw/pitch indicados. Usa la misma
    // convención que el resto del juego (a yaw 0 apunta a +Z, y pitch positivo = hacia arriba).
    private static Vector3 Direction(float yaw, float pitch)
    {
        float cosPitch = MathF.Cos(pitch);
        return new Vector3(MathF.Sin(yaw) * cosPitch, MathF.Sin(pitch), MathF.Cos(yaw) * cosPitch);
    }

    private void UpdateView()
    {
        Vector3 pivot = _target.GetPosition();

        Vector3 horizontalForward = new Vector3(MathF.Sin(_cameraYaw), 0f, MathF.Cos(_cameraYaw));
        Vector3 position = pivot + Vector3.Up * CameraHeight - horizontalForward * _radius;

        // Red de seguridad: si el terreno cercano es más alto que la cámara, la subimos.
        float ground = _getTerrainHeight(position.X, position.Z) + MinGroundClearance;
        if (position.Y < ground)
            position.Y = ground;

        Vector3 lookAt = position + Direction(_cameraYaw, _cameraPitch) * LookDistance;

        this.SetView(position, lookAt, Vector3.Up);
    }

    public override void Update(KeyboardState keyboardState, MouseState mouseState, float elapsedTime)
    {
        // El mouse está capturado y el juego lo recentera en cada frame, así que este delta es
        // el movimiento realizado durante el frame.
        float deltaX = mouseState.X - _centerX;
        float deltaY = mouseState.Y - _centerY;

        // Mismos sentidos que el giro del casco (A/D) y la elevación: mouse a la derecha gira a la
        // derecha, mouse hacia arriba apunta hacia arriba.
        Yaw = MathHelper.WrapAngle(Yaw - deltaX * _sensitivity);
        Pitch -= deltaY * _sensitivity;

        // Con el free look se puede mirar en cualquier dirección. En el resto del tiempo la puntería
        // queda acotada al rango de elevación real del cañón, para que la cámara nunca se vaya
        // más lejos de donde el cañón puede llegar.
        Pitch = IsFreeLook
            ? MathHelper.Clamp(Pitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f)
            : Math.Clamp(Pitch, Canon.MinimumAimPitch, Canon.MaximumAimPitch);

        // La cámara persigue a la puntería con retraso (suavizado exponencial). En modo normal no
        // acompaña toda la elevación (para no perder de vista el tanque); en free look sí.
        float cameraPitchTarget = IsFreeLook
            ? Pitch
            : Math.Clamp(Pitch, -MaxCameraPitch, MaxCameraPitch);

        float t = 1f - MathF.Exp(-FollowSharpness * elapsedTime);
        _cameraYaw += MathHelper.WrapAngle(Yaw - _cameraYaw) * t;
        _cameraPitch += (cameraPitchTarget - _cameraPitch) * t;

        this.UpdateView();
    }

    public void SetSensitivity(float sensitivity)
    {
        this._sensitivity = sensitivity;
    }
}
