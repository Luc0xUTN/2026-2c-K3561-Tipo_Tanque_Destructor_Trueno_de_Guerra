using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class ShadowMapRenderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly RenderTarget2D _shadowMap;

    private RenderTargetBinding[] _previousRenderTargets;
    private Viewport _previousViewport;
    private bool _isRendering;

    public Texture2D ShadowMap => _shadowMap;
    public int Resolution { get; }
    public float TexelSize => 1.0f / Resolution;

    public Matrix View { get; private set; }
    public Matrix Projection { get; private set; }
    public Matrix LightViewProjection => View * Projection;
    public BoundingFrustum LightFrustum { get; private set; }

    public ShadowMapRenderer(GraphicsDevice device, int resolution = 4096)
    {
        _device = device;
        Resolution = resolution;

        _shadowMap = new RenderTarget2D(
            device,
            resolution,
            resolution,
            false,
            SurfaceFormat.Single,
            DepthFormat.Depth24,
            0,
            RenderTargetUsage.DiscardContents
        );
    }

    public void ConfigureLightCamera(
        Vector3 lightPosition,
        Vector3 target,
        float width,
        float height,
        float nearPlane,
        float farPlane)
    {
        View = Matrix.CreateLookAt(lightPosition, target, Vector3.Up);
        Projection = Matrix.CreateOrthographic(width, height, nearPlane, farPlane);
        LightFrustum = new BoundingFrustum(LightViewProjection);
    }

    public void BeginShadowPass()
    {
        if (_isRendering)
            throw new InvalidOperationException("El Shadow Pass ya está iniciado.");

        _previousRenderTargets = _device.GetRenderTargets();
        _previousViewport = _device.Viewport;

        _device.SetRenderTarget(_shadowMap);
        _device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);

        _isRendering = true;
    }

    public void EndShadowPass()
    {
        if (!_isRendering)
            throw new InvalidOperationException("No hay un Shadow Pass iniciado.");

        _device.SetRenderTargets(_previousRenderTargets);
        _device.Viewport = _previousViewport;

        _isRendering = false;
    }

    public void Dispose()
    {
        _shadowMap?.Dispose();
    }
}