using Content.Shared.Access.Systems;
using Content.Shared.Clothing;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Roles.Ranks;

public abstract class SharedRankSystem : EntitySystem
{
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private EntityQuery<RankComponent> _ranksQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<RankComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<RankComponent, ClothingGotUnequippedEvent>(OnUnequip);

        SubscribeLocalEvent<MarineComponent, StartingGearEquippedEvent>(OnStartingGear);
    }

    private void OnEquip(Entity<RankComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var user = args.Wearer;

        SetRank(user, ent.Comp);
    }

    private void OnUnequip(Entity<RankComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemCompDeferred<RankComponent>(args.Wearer);
    }

    private void OnStartingGear(Entity<MarineComponent> ent, ref StartingGearEquippedEvent args)
    {
        var marine = ent.Owner;

        if (_idCardSystem.TryGetIdCard(marine, out var idcard))
        {
            var idCardEntity = idcard.Owner;
            var rankComp = EnsureComp<RankComponent>(idCardEntity);

            if (!_mind.TryGetMind(marine, out var mindId, out _) || !_job.MindTryGetJobId(mindId, out var jobId))
                return;

            foreach (var proto in _prototypes.EnumeratePrototypes<RankPrototype>())
            {
                foreach (var job in proto.Jobs)
                {
                    if (job == jobId)
                    {
                        rankComp.Rank = proto.ID;
                        Dirty(idCardEntity, rankComp);
                        break;
                    }
                    else
                        continue;
                }
            }

            SetRank(marine, rankComp);
        }
    }

    /// <summary>
    ///     Sets a mob's rank from the given RankComponent.
    /// </summary>
    public void SetRank(EntityUid uid, RankComponent from)
    {
        var comp = EnsureComp<RankComponent>(uid);

        comp.Rank = from.Rank;
        Dirty(uid, comp);
    }

    /// <summary>
    ///     Gets the rank of a given mob.
    /// </summary>
    public RankPrototype? GetRank(EntityUid uid)
    {
        if (!_ranksQuery.TryComp(uid, out var rankComponent))
            return null;

        if (_prototypes.TryIndex<RankPrototype>(rankComponent.Rank, out var rankProto) && rankProto != null)
            return rankProto;

        return null;
    }
}