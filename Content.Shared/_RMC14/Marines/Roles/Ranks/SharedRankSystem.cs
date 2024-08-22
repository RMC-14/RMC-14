using Content.Shared.Clothing;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Roles.Ranks;

public abstract class SharedRankSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RankComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<RankComponent, ClothingGotUnequippedEvent>(OnUnequip);
    }

    private void OnEquip(Entity<RankComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var rank = ent.Comp.Rank;

        if (rank != null)
            SetRank(args.Wearer, rank.Value);
    }

    private void OnUnequip(Entity<RankComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemCompDeferred<RankComponent>(args.Wearer);
    }

    /// <summary>
    ///     Sets a mob's rank from the given RankPrototype.
    /// </summary>
    public void SetRank(EntityUid uid, RankPrototype from)
    {
        SetRank(uid, from.ID);
    }

    /// <summary>
    ///     Sets a mob's rank from the given RankPrototype.
    /// </summary>
    public void SetRank(EntityUid uid, ProtoId<RankPrototype> from)
    {
        var comp = EnsureComp<RankComponent>(uid);
        comp.Rank = from;
        Dirty(uid, comp);
    }

    /// <summary>
    ///     Gets the rank of a given mob.
    /// </summary>
    public RankPrototype? GetRank(EntityUid uid)
    {
        if (TryComp<RankComponent>(uid, out var component))
            return GetRank(component);

        return null;
    }

    /// <summary>
    ///     Gets a RankPrototype from a RankComponent.
    /// </summary>
    public RankPrototype? GetRank(RankComponent component)
    {
        if (_prototypes.TryIndex<RankPrototype>(component.Rank, out var rankProto) && rankProto != null)
            return rankProto;

        return null;
    }

    public string? GetSpeakerRankName(EntityUid uid)
    {
        var rank = GetRank(uid);
        if (rank == null)
            return null;

        var shortRank = rank.ShortenedName;
        var finalName = shortRank + " " + Name(uid);

        return finalName;
    }

    public string? GetSpeakerFullRankName(EntityUid uid)
    {
        var rank = GetRank(uid);
        if (rank == null)
            return null;

        var rankName = rank.Name;
        var finalName = rankName + " " + Name(uid);

        return finalName;
    }
}