using System.Net.NetworkInformation;
using Content.Shared._RMC14.Medical.Refill;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Medical.Refill;

public sealed class RMCMedicalSupplyLinkSystem : SharedMedicalSupplyLinkSystem
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

        var state = ent.Comp.ConnectedPort
            ? $"{ent.Comp.BaseState}_clamped"
            : $"{ent.Comp.BaseState}_unclamped";

        _sprite.LayerSetRsiState((ent.Owner, sprite), ent.Comp.BaseLayerKey, state);
    }
}
