using System.Linq;
using Content.Server.Construction;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Shared._RMC14.Barricade;
using Content.Shared._RMC14.Barricade.Components;

namespace Content.Server._RMC14.Barricade;

public sealed class BarbedSystem : SharedBarbedSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarbedComponent, ConstructionChangeEntityEvent>(OnBarbedEntityConstructionChange);
        SubscribeLocalEvent<BarbedComponent, BarbedStateChangedEvent>(OnBarbedStateChanged);
    }

    private void OnBarbedEntityConstructionChange(EntityUid ent, BarbedComponent comp, ConstructionChangeEntityEvent args)
    {
        var newComp = EnsureComp<BarbedComponent>(args.New);
        newComp.IsBarbed = comp.IsBarbed;
        UpdateBarricade((args.New, newComp), true);
    }

    private void OnBarbedStateChanged(Entity<BarbedComponent>ent, ref BarbedStateChangedEvent args)
    {
        if(!TryComp(ent, out DestructibleComponent? destructible))
            return;

        var trigger = (DamageTrigger?) destructible.Thresholds.LastOrDefault(threshold => threshold.Trigger is DamageTrigger)?.Trigger;
        if(trigger == null)
            return;

        if(ent.Comp.IsBarbed)
            trigger.Damage += ent.Comp.MaxHealthIncrease;
        else
            trigger.Damage -= ent.Comp.MaxHealthIncrease;
    }
}
