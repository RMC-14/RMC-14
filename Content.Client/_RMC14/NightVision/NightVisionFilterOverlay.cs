using System.Numerics;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.NightVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.NightVision;

public sealed partial class NightVisionFilterOverlay : Overlay
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IConfigurationManager _config = default!;

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

            var colorVal = _config.GetCVar(RMCCVars.RMCNightVisionColor);
            var color = ((NightVisionColor) colorVal).ToColor();
            _shader.SetParameter("nv_color", new Vector3(color.R, color.G, color.B));

            handle.UseShader(_shader);
            handle.DrawRect(args.WorldBounds, Color.White);
            handle.UseShader(null);
        }
    }
}
