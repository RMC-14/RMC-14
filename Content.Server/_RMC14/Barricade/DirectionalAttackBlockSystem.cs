using System.Linq;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Shared._RMC14.Barricade;
using Content.Shared.Damage;

namespace Content.Server._RMC14.Barricade;

public sealed class DirectionalAttackBlockSystem : SharedDirectionalAttackBlockSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DirectionalAttackBlockerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DirectionalAttackBlockerComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<DirectionalAttackBlockerComponent, FailedBlockAttemptEvent>(OnBarricadeFailedBlock);
    }

    private void OnMapInit(Entity<DirectionalAttackBlockerComponent> ent, ref MapInitEvent args)
    {
        if(!TryComp(ent, out DestructibleComponent? destructible))
            return;

        var trigger = (DamageTrigger?) destructible.Thresholds.LastOrDefault(threshold => threshold.Trigger is DamageTrigger)?.Trigger;
        if(trigger == null)
            return;

        ent.Comp.MaxHealth = trigger.Damage;
        Dirty(ent);
    }

    private void OnDamageChanged(Entity<DirectionalAttackBlockerComponent> ent, ref DamageChangedEvent args)
    {
        ent.Comp.BlockRoll = Random.NextFloat(0f, 1f);
        Dirty(ent);
    }

    private void OnBarricadeFailedBlock(Entity<DirectionalAttackBlockerComponent> ent, ref FailedBlockAttemptEvent args)
    {
        ent.Comp.BlockRoll = Random.NextFloat(0f, 1f);
        Dirty(ent);
    }
}
