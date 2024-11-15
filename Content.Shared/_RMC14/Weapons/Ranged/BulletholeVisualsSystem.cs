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

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class BulletholeVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BulletholeVisualsComponent, DamageChangedEvent>(OnVisualsDamageChangedEvent);
    }

    private void OnVisualsDamageChangedEvent(Entity<BulletholeVisualsComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryComp(args.Tool, out BallisticAmmoComponent? ballisticAmmoComponent))
            return;

        ent.Comp.BulletholeCount++;
        Logger.Debug($"BulletholeVisuals at {ToPrettyString(ent)} with {ToPrettyString(args.Tool)}: there are {ent.Comp.BulletholeCount} holes");
    }
}
