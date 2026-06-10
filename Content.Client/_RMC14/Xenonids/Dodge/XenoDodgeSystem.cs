using Content.Shared._RMC14.Xenonids.Dodge;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Client._RMC14.Xenonids.Dodge;

public sealed class XenoDodgeSystem : SharedXenoDodgeSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new XenoDodgeOverlay(EntityManager, _timing, _random));
    }

    protected override void OnActiveDodgeRemove(Entity<XenoActiveDodgeComponent> xeno, ref ComponentRemove args)
    {
        base.OnActiveDodgeRemove(xeno, ref args);

        if (_timing.ApplyingState)
            return;

        if (!TryComp<SpriteComponent>(xeno, out var sprite))
            return;

        _sprite.SetOffset((xeno, sprite), Vector2.Zero);
        xeno.Comp.AfterImages.Clear();
    }
}
