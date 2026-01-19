using Content.Shared._RMC14.NightVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Maths;
using Robust.Shared.IoC;

namespace Content.Client._RMC14.NightVision;

public sealed class NightVisionFilterOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<ShaderPrototype> ShaderId = "RMCNightVision";
    private static readonly Dictionary<NightVisionColor, Color> NightVisionColors = new()
    {
        { NightVisionColor.Green, new Color(0.22f, 1.0f, 0.08f) },    // #39FF14
        { NightVisionColor.Orange, new Color(1.0f, 0.8f, 0.4f) },     // #FFCC66
        { NightVisionColor.White, new Color(0.83f, 0.83f, 0.83f) },   // #D3D3D3
        { NightVisionColor.Yellow, new Color(1.0f, 1.0f, 0.4f) },     // #FFFF66
        { NightVisionColor.Red, new Color(1.0f, 0.2f, 0.2f) },        // #FF3333
        { NightVisionColor.Blue, new Color(0.4f, 0.8f, 1.0f) },       // #66CCFF
    };

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
        if (!TryGetNightVisionComponent(out var nightVision))
            return;

        var color = GetNightVisionColor(nightVision!.Color);
        var handle = args.WorldHandle;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture!);
        _shader.SetParameter("nv_color", new Vector3(color.R, color.G, color.B));

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    private bool TryGetNightVisionComponent(out NightVisionComponent? nightVision)
    {
        nightVision = null;

        return _players.LocalPlayer?.ControlledEntity is { } playerEntity &&
               _entity.TryGetComponent(playerEntity, out nightVision) &&
               nightVision?.State != NightVisionState.Off &&
               ScreenTexture != null;
    }

    private static Color GetNightVisionColor(NightVisionColor color)
    {
        return NightVisionColors.TryGetValue(color, out var value) ? value : NightVisionColors[NightVisionColor.Green];
    }
}
