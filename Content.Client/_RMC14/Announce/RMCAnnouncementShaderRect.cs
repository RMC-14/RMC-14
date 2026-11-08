using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Announce;

public sealed class RMCAnnouncementShaderRect : Control
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly Dictionary<string, Color> _colorParameters = new();
    private string? _shaderId;
    private ShaderInstance? _shader;

    public RMCAnnouncementShaderRect()
    {
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Ignore;
        Visible = false;
    }

    public void SetShader(string? id)
    {
        ReleaseShader();
        _shaderId = id != null && _prototype.HasIndex<ShaderPrototype>(id) ? id : null;
        Visible = _shaderId != null;
    }

    public void SetColorParameter(string name, Color? value)
    {
        if (value is { } color)
            _colorParameters[name] = color;
        else
            _colorParameters.Remove(name);

        ReleaseShader();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_shaderId == null || PixelWidth <= 0 || PixelHeight <= 0)
            return;

        _shader ??= _prototype.Index<ShaderPrototype>(_shaderId).InstanceUnique();
        _shader.SetParameter("size", (System.Numerics.Vector2) PixelSize);
        foreach (var (name, color) in _colorParameters)
        {
            _shader.SetParameter(name, color);
        }

        handle.UseShader(_shader);
        handle.DrawRect(PixelSizeBox, Color.White);
        handle.UseShader(null);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        ReleaseShader();
    }

    private void ReleaseShader()
    {
        _shader?.Dispose();
        _shader = null;
    }
}
