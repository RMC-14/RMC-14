using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Medical.Refill;
using Content.Shared._RMC14.Vendors;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Medical.Refill;

public sealed class RMCMedicalSupplyLinkSystem : SharedMedicalSupplyLinkSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string FlickId = "rmc_flick_animation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMMedicalSupplyLinkComponent, ComponentStartup>(OnComponentStartup);
    }

    protected override void OnVendorAnchorChanged(Entity<CMAutomatedVendorComponent> vendor, ref AnchorStateChangedEvent args)
    {
        // This should prevent state mismatch when quickly unanchoring and reanchoring
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(vendor);
        while (anchored.MoveNext(out var anchoredId))
        {
            if (!HasComp<CMMedicalSupplyLinkComponent>(anchoredId))
                continue;

            if (_animationPlayer.HasRunningAnimation(anchoredId, FlickId))
                _animationPlayer.Stop(anchoredId, FlickId);

            break;
        }

        base.OnVendorAnchorChanged(vendor, ref args);
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
