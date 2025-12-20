using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Mobs.Systems;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Server._RMC14.Explosion;

public sealed partial class RMCLandmineSystem : SharedRMCLandmineSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCLandmineComponent, StepTriggeredOffEvent>(HandleStepOffTriggered);
        SubscribeLocalEvent<RMCLandmineComponent, StepTriggerAttemptEvent>(HandleStepTriggerAttempt);
        SubscribeLocalEvent<RMCLandmineComponent, StartCollideEvent>(OnStartCollide);
    }

    private void HandleStepOffTriggered(Entity<RMCLandmineComponent> ent, ref StepTriggeredOffEvent args)
    {
        if (_mobState.IsDead(args.Tripper))
            return;

        _trigger.Trigger(ent, args.Tripper);
    }

    private void HandleStepTriggerAttempt(Entity<RMCLandmineComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;

        if (!ent.Comp.Armed)
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.Faction != null && GunIff.IsInFaction(args.Tripper, ent.Comp.Faction.Value))
            args.Cancelled = true;
    }

    private void OnStartCollide(Entity<RMCLandmineComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<XenoProjectileComponent>(args.OtherEntity))
            return;

        ent.Comp.ShotStacks++;
        Dirty(ent);

        if (ent.Comp.ShotStacks >= ent.Comp.ShotStackLimit)
            _trigger.Trigger(ent, args.OtherEntity);
    }
}
