using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Announce;

public sealed class RMCAnnouncementPortraitView : Control
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly Dictionary<string, Color> _colorParameters = new();
    private readonly Dictionary<string, float> _floatParameters = new();
    private EntityUid? _entity;
    private IRenderTexture? _target;
    private string? _shaderId;
    private ShaderInstance? _shader;

    public float Zoom = 3;

    public RMCAnnouncementPortraitView()
    {
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Ignore;
    }

    public void SetEntity(EntityUid? entity)
    {
        _entity = entity;
    }

    public void SetShader(string? id)
    {
        ReleaseShader();
        _shaderId = id != null && _prototype.HasIndex<ShaderPrototype>(id) ? id : null;
    }

    public void SetColorParameter(string name, Color? value)
    {
        if (value is { } color)
            _colorParameters[name] = color;
        else
            _colorParameters.Remove(name);
    }

    public void SetFloatParameter(string name, float value)
    {
        _floatParameters[name] = value;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_entity is not { } uid ||
            !_entities.TryGetComponent(uid, out SpriteComponent? sprite) ||
            PixelWidth <= 0 ||
            PixelHeight <= 0)
        {
            return;
        }

        var size = PixelSize;
        if (_target == null || _target.Size != size)
        {
            _target?.Dispose();
            _target = _clyde.CreateRenderTarget(
                size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                new TextureSampleParameters { Filter = false },
                nameof(RMCAnnouncementPortraitView));
        }

        _entities.System<SpriteSystem>().ForceUpdate(uid);

        var center = new Vector2(size.X / 2f, size.X / 2f);
        var scale = new Vector2(Zoom * UIScale);
        handle.RenderInRenderTarget(_target, () =>
        {
            handle.SetTransform(Matrix3x2.Identity);
            handle.DrawEntity(uid, center, scale, Angle.Zero, Angle.Zero, Direction.South, sprite);
        }, Color.Transparent);

        if (_shaderId != null)
        {
            _shader ??= _prototype.Index<ShaderPrototype>(_shaderId).InstanceUnique();
            _shader.SetParameter("size", (Vector2) size);
            _shader.SetParameter("pixelSize", Zoom * UIScale);
            foreach (var (name, value) in _floatParameters)
            {
                _shader.SetParameter(name, value);
            }

            foreach (var (name, color) in _colorParameters)
            {
                _shader.SetParameter(name, color);
            }

            handle.UseShader(_shader);
        }

        handle.DrawTextureRect(_target.Texture, PixelSizeBox);
        handle.UseShader(null);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        ReleaseShader();
        _target?.Dispose();
        _target = null;
    }

    private void ReleaseShader()
    {
        _shader?.Dispose();
        _shader = null;
    }
}
