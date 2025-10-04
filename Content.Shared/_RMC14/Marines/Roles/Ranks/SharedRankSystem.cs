using System.Linq;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Dataset;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Roles.Ranks;

public abstract class SharedRankSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

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

    /// <summary>
    /// Returns the entities with the highest rank among the passed entities.
    /// Uses the specified rank hierarchy.
    /// </summary>
    /// <param name="entities">List of entities to be compared.</param>
    /// <param name="rankHierarchyId">ID of the dataset prototype with the order of rank precedence determined by the index. A non-empty <see cref="DatasetPrototype.Values"/> is expected.</param>
    /// <returns>
    /// List of entities with the highest rank. May be null if no entity has a valid rank. Will also return null and an error if the method is passed a dataset with empty values.
    /// </returns>
    public List<EntityUid>? GetEntitiesWithHighestRank(List<EntityUid> entities, ProtoId<DatasetPrototype> rankHierarchyId)
    {
        var result = new List<EntityUid>();

        if (!_prototypes.TryIndex<DatasetPrototype>(rankHierarchyId, out var rankHierarchy))
            return null;

        var rankOrder = rankHierarchy.Values.ToList();
        if (rankOrder.Count == 0)
        {
            // The dataset cannot be empty, the person forgot to add values ​​to it
            Logger.Error($"The rank hierarchy dataset '{rankHierarchyId}' has an invalid value: empty. The highest rank cannot be determined.");
            return null;
        }

        var rankScores = new Dictionary<EntityUid, int>();
        var highestRank = -1;

        foreach (var candidate in entities)
        {
            if (!TryComp<RankComponent>(candidate, out var rankComp) || rankComp.Rank == null)
                continue;

            if (!_prototypes.TryIndex<RankPrototype>(rankComp.Rank, out var rankProto))
                continue;

            var rankIndex = rankOrder.IndexOf(rankProto.ID);
            if (rankIndex == -1)
                continue;

            rankScores[candidate] = rankIndex;

            if (rankIndex > highestRank)
                highestRank = rankIndex;
        }

        if (highestRank == -1) // No valid ranks found
            return null;

        result = rankScores
            .Where(pair => pair.Value == highestRank)
            .Select(pair => pair.Key)
            .ToList();

        return result;
    }

    /// <summary>
    /// Checks for invalid rank.
    /// </summary>
    /// <param name="entity">The entity being checked.</param>
    /// <param name="invalidRankId">Invalid rank</param>
    /// <returns>
    /// Returns <c>true</c> if the rank could not be obtained or an invalid rank was found when the <c>invalidRankId</c> parameter is active.
    /// </returns>
    public bool HasInvalidRank(EntityUid entity, ProtoId<RankPrototype> invalidRankId = default)
    {
        if (!_entMan.TryGetComponent<RankComponent>(entity, out var rankComp))
            return true;

        if (rankComp.Rank == null)
            return true;

        if (invalidRankId != default && rankComp.Rank == invalidRankId)
            return true;

        return false;
    }

}
