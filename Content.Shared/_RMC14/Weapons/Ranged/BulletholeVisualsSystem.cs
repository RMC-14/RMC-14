using Content.Shared._RMC14.Xenonids.Fortify;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rounding;
using Content.Shared.Stunnable;
using Content.Shared.StatusEffect;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared._RMC14.Weapons.Ranged.Ammo;
using Content.Shared.Humanoid;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class BulletholeVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    // Bullethole overlays
    private const int BulletholeStates = 1;
    private const int MaxBulletholeCount = 8;
    private const string BulletholeRsiPath = "/Textures/_RMC14/Effects/bullethole.rsi";

    public override void Initialize()
    {
        SubscribeLocalEvent<BulletholeVisualsComponent, DamageChangedEvent>(OnVisualsDamageChangedEvent);
    }

    private void OnVisualsDamageChangedEvent(Entity<BulletholeVisualsComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryComp(args.Tool, out BallisticAmmoComponent? ballisticAmmoComponent))
            return;

        if (!TryComp<AppearanceComponent>(ent, out var app))
            return;

        ent.Comp.BulletholeCount++;
        var stateString = $"bhole_reference_{(ent.Comp.BulletholeCount >= 8 ? 8 : ent.Comp.BulletholeCount):00}";
        Logger.Debug($"BulletholeVisuals at {ToPrettyString(ent)} with {ToPrettyString(args.Tool)}: there are {ent.Comp.BulletholeCount} holes -> Sending state {stateString}");
        _appearance.SetData(ent, BulletholeVisualLayers.State, stateString, app);
    }
}
