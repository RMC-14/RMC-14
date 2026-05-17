using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Neurocryogenic : RMCChemicalEffect
{
    public override string Abbreviation => "NRC";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Causes knockdown and stun for [color=red]40[/color] seconds.\n" +
               $"When dead, extends revive window by [color=cyan]{PotencyPerSecond * 2.5}[/color] seconds.\n" +
               $"Overdoses lowers body temperature by [color=red]{PotencyPerSecond * 2.5}ºC[/color], down to a minimum of -63.15ºC (-81.67ºF)"; //.\n" +
               //$"Critical overdoses cause [color=red]{PotencyPerSecond * 2.5}[/color] brain damage";
    }

    public override bool CanMetabolize(EntityUid target)
    {
        return true;
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (ProbHundred(10))
        {
            var popup = System<SharedPopupSystem>(args);
            var net = IoCManager.Resolve<INetManager>();
            if (net.IsServer)
                popup.PopupClient("You feel like you have the worst brain freeze ever!", args.TargetEntity, args.TargetEntity);
        }

        var stun = System<SharedStunSystem>(args);
        stun.TryKnockdown(args.TargetEntity, TimeSpan.FromSeconds(40), true);
        stun.TryStun(args.TargetEntity, TimeSpan.FromSeconds(40), true);

        var mobState = System<MobStateSystem>(args);
        if (IsHumanoid(args) && mobState.IsDead(args.TargetEntity))
        {
            var reviveTime = System<RMCUnrevivableSystem>(args);
            reviveTime.AddRevivableTime(args.TargetEntity, TimeSpan.FromSeconds(potency.Float() * 2.5f));
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var sys = System<SharedRMCTemperatureSystem>(args);
        var current = sys.GetTemperature(args.TargetEntity);
        var change = (potency * 2.5 * args.Scale).Float();
        var temp = Math.Max(210, current - change);

        sys.ForceChangeTemperature(args.TargetEntity, temp);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 Brain Damage
    }
    // TODO RMC14 reaction_mob(mob/M, method = TOUCH, volume, potency = 1)
}
