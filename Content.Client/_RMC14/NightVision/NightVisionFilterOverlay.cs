using Content.Shared._RMC14.NightVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.NightVision;

public sealed class NightVisionFilterOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<ShaderPrototype> ShaderId = "RMCNightVision";

    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public NightVisionFilterOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypes.Index(ShaderId).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entity.TryGetComponent(_players.LocalEntity, out NightVisionComponent? nightVision) ||
            nightVision.State == NightVisionState.Off)
        {
            return;
        }

        var handle = args.WorldHandle;
        if (nightVision.Green && ScreenTexture != null)
        {
            _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            handle.UseShader(_shader);
            handle.DrawRect(args.WorldBounds, Color.White);
            handle.UseShader(null);
        }
    }
}
