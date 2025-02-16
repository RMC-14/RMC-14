using Content.Shared._RMC14.Weapons.Ranged.Auto;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._RMC14.Weapons.Ranged.Auto;

public sealed class ShowAutoFireOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;

    private readonly GunToggleableAutoFireSystem _autoFire;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public ShowAutoFireOverlay()
    {
        IoCManager.InjectDependencies(this);

        _autoFire = _entity.System<GunToggleableAutoFireSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, _autoFire.Shape.Vertices, Color.Red.WithAlpha(0.5f));
    }
}
