using Content.Shared._RMC14.Xenonids;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Roles.Ranks;

public abstract class SharedRankSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RankComponent, ExaminedEvent>(OnRankExamined);
    }

    private void OnRankExamined(Entity<RankComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(SharedRankSystem), 1))
        {
            var user = ent.Owner;
            var rank = GetRankString(user, hasPaygrade: true);

            if (rank != null)
            {
                var finalString = Loc.GetString("rmc-rank-component-examine", ("user", user), ("rank", rank));
                args.PushMarkup(finalString);
            }
        }
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

    /// <summary>
    ///     Gets the rank name of a given mob.
    /// </summary>
    public string? GetRankString(EntityUid uid, bool isShort = false, bool hasPaygrade = false)
    {
        var rank = GetRank(uid);

        if (rank == null)
            return null;

        if (isShort)
        {
            if (rank.FemalePrefix == null || rank.MalePrefix == null)
                return rank.Prefix;

            if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearance))
                return rank.Prefix;

            var genderPrefix = humanoidAppearance.Gender switch
            {
                Gender.Female => rank.FemalePrefix,
                Gender.Male => rank.MalePrefix,
                _ => rank.Prefix,
            };

            return genderPrefix;
        }

        if (hasPaygrade && rank.Paygrade != null)
            return $"({Loc.GetString(rank.Paygrade)}) {Loc.GetString(rank.Name)}";

        return rank.Name;
    }

    /// <summary>
    ///     Gets the prefix rank name. (ex. Maj John Marine)
    /// </summary>
    public string? GetSpeakerRankName(EntityUid uid)
    {
        var rank = GetRankString(uid, true);
        if (rank == null)
            return null;

        return rank + " " + Name(uid);
    }

    /// <summary>
    ///     Gets the prefix full name. (ex. Major John Marine)
    /// </summary>
    public string? GetSpeakerFullRankName(EntityUid uid)
    {
        var rank = GetRankString(uid);
        if (rank == null)
            return null;

        return rank + " " + Name(uid);
    }
}
