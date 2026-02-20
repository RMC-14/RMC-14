using Content.Shared.Cuffs;
using Content.Shared.Flash;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;

namespace Content.Shared._RMC14.Policing;

public sealed class RMCPolicingSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCImmuneToInterFactionPolicingComponent, FlashAttemptEvent>(OnInterFactionFlashAttempt);
        SubscribeLocalEvent<RMCImmuneToInterFactionPolicingComponent, GetPolicingResistanceEvent>(OnInterFactionPolicingResistance);
        SubscribeLocalEvent<RMCImmuneToInterFactionPolicingComponent, TargetHandcuffedEvent>(OnInterFactionHandcuffed);
    }

    public bool CanBePoliced(EntityUid target, EntityUid? user)
    {
        if (user == null)
            return true;

        if (HasComp<RMCImmuneToInterFactionPolicingComponent>(target)
            && TryComp<NpcFactionMemberComponent>(user, out var userFaction)
            && !_npcFaction.IsMemberOfAny(target, userFaction.Factions))
        {
            return false;
        }

        return true;
    }

    private void OnInterFactionFlashAttempt(Entity<RMCImmuneToInterFactionPolicingComponent> ent, ref FlashAttemptEvent args)
    {
        if (!HasComp<RMCPolicingToolComponent>(ent.Owner))
            return;

        if (CanBePoliced(ent.Owner, args.User))
            return;

        args.Cancelled = true;
    }

    private void OnInterFactionPolicingResistance(Entity<RMCImmuneToInterFactionPolicingComponent> ent, ref GetPolicingResistanceEvent args)
    {
        args.Multiplier = ent.Comp.EffectivenessMultiplier;
    }

    private void OnInterFactionHandcuffed(Entity<RMCImmuneToInterFactionPolicingComponent> ent, ref TargetHandcuffedEvent args)
    {
        if (ent.Comp.RemoveOnCuffed)
            RemCompDeferred<RMCImmuneToInterFactionPolicingComponent>(ent.Owner);
    }
}

[ByRefEvent]
public sealed class GetPolicingResistanceEvent : EntityEventArgs
{
    public float Multiplier = 1f;
}
