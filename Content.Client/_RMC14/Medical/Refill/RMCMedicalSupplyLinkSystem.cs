using Content.Shared._RMC14.Medical.Refill;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Medical.Refill;

/// <summary>
/// Client-side system that sets the initial sprite state for medical supply links.
/// Animation transitions are handled by the RMC animation system via Flick events from the server.
/// </summary>
public sealed class RMCMedicalSupplyLinkSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMMedicalSupplyLinkComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(Entity<CMMedicalSupplyLinkComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var state = ent.Comp.PortConnected
            ? $"{ent.Comp.BaseState}_clamped"
            : $"{ent.Comp.BaseState}_unclamped";

        _sprite.LayerSetRsiState((ent.Owner, sprite), "base", state);
    }
}
