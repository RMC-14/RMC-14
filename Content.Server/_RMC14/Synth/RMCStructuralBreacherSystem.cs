using Content.Shared._RMC14.Synth;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;

namespace Content.Server._RMC14.Synth;

/// <summary>
/// Applies synth breaching bonuses on top of normal melee damage.
/// </summary>
public sealed class RMCStructuralBreacherSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCStructuralBreacherComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<RMCStructuralBreacherComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (ent.Comp.RequiresSynth && !HasComp<SynthComponent>(args.User))
        {
            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("rmc-synth-item-too-heavy", ("item", ent.Owner)), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        // Normal melee already handled the swing. This pass only adds the CM synth hammer structural bonus.
        foreach (var target in args.HitEntities)
        {
            if (ent.Comp.Whitelist != null && !_whitelist.IsValid(ent.Comp.Whitelist, target))
                continue;

            _damageable.TryChangeDamage(target,
                ent.Comp.BonusDamage,
                ignoreResistances: false,
                interruptsDoAfters: true,
                origin: args.User,
                tool: ent.Owner);
        }
    }
}
