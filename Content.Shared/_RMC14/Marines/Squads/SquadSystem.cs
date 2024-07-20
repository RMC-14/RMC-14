using System.Collections.Immutable;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Squads;

public sealed class SquadSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedIdCardSystem _id = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public ImmutableArray<EntityPrototype> SquadPrototypes { get; private set; }

    public override void Initialize()
    {
        SubscribeLocalEvent<SquadMemberComponent, GetMarineIconEvent>(OnSquadRoleGetIcon);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        RefreshSquadPrototypes();
    }

    private void OnSquadRoleGetIcon(Entity<SquadMemberComponent> member, ref GetMarineIconEvent args)
    {
        args.Background = member.Comp.Background;
        args.BackgroundColor = member.Comp.BackgroundColor;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            RefreshSquadPrototypes();
    }

    private void RefreshSquadPrototypes()
    {
        var builder = ImmutableArray.CreateBuilder<EntityPrototype>();
        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.HasComponent<SquadTeamComponent>())
                builder.Add(entity);
        }

        builder.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        SquadPrototypes = builder.ToImmutable();
    }

    public bool TryGetSquad(EntProtoId prototype, out Entity<SquadTeamComponent> squad)
    {
        var squadQuery = EntityQueryEnumerator<SquadTeamComponent, MetaDataComponent>();
        while (squadQuery.MoveNext(out var uid, out var team, out var metaData))
        {
            if (metaData.EntityPrototype?.ID != prototype.Id)
                continue;

            squad = (uid, team);
            return true;
        }

        squad = default;
        return false;
    }

    public bool TryEnsureSquad(EntProtoId id, out Entity<SquadTeamComponent> squad)
    {
        if (!_prototypes.TryIndex(id, out var prototype) ||
            !prototype.HasComponent<SquadTeamComponent>(_compFactory))
        {
            squad = default;
            return false;
        }

        if (TryGetSquad(id, out squad))
            return true;

        var squadEnt = Spawn(id);
        if (!TryComp(squadEnt, out SquadTeamComponent? squadComp))
        {
            Log.Error($"Squad entity prototype {id} had {nameof(SquadTeamComponent)}, but none found on entity {ToPrettyString(squadEnt)}");
            return false;
        }

        squad = (squadEnt, squadComp);
        return true;
    }

    public int GetSquadMembersAlive(Entity<SquadTeamComponent> team)
    {
        var count = 0;
        var members = EntityQueryEnumerator<SquadMemberComponent>();
        while (members.MoveNext(out var uid, out var member))
        {
            if (member.Squad == team && !_mobState.IsDead(uid))
                count++;
        }

        return count;
    }

    public void AssignSquad(EntityUid marine, Entity<SquadTeamComponent?> team, ProtoId<JobPrototype>? job)
    {
        if (!Resolve(team, ref team.Comp))
            return;

        var member = EnsureComp<SquadMemberComponent>(marine);
        member.Squad = team;
        member.Background = team.Comp.Background;
        member.BackgroundColor = team.Comp.Color;
        Dirty(marine, member);

        var grant = EnsureComp<SquadGrantAccessComponent>(marine);
        grant.AccessLevels = team.Comp.AccessLevels;

        if (_prototypes.TryIndex(job, out var jobProto))
        {
            grant.RoleName = $"{Name(team)} {jobProto.LocalizedName}";
        }
        else if (_mind.TryGetMind(marine, out var mindId, out _) &&
                 _job.MindTryGetJobName(mindId, out var name))
        {
            MarineSetTitle(marine, $"{Name(team)} {name}");
        }

        Dirty(marine, grant);
    }

    private void MarineSetTitle(EntityUid marine, string title)
    {
        foreach (var item in _inventory.GetHandOrInventoryEntities(marine))
        {
            if (TryComp(item, out IdCardComponent? idCard))
                _id.TryChangeJobTitle(item, title, idCard);
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SquadGrantAccessComponent>();
        while (query.MoveNext(out var uid, out var grant))
        {
            if (grant.RoleName != null)
                MarineSetTitle(uid, grant.RoleName);

            foreach (var item in _inventory.GetHandOrInventoryEntities(uid))
            {
                if (grant.AccessLevels.Length > 0 &&
                    TryComp(item, out AccessComponent? access))
                {
                    foreach (var level in grant.AccessLevels)
                    {
                        access.Tags.Add(level);
                    }

                    Dirty(item, access);
                }

                if (HasComp<IdCardComponent>(item) &&
                    !EnsureComp<IdCardOwnerComponent>(item, out var owner))
                {
                    owner.Id = uid;
                }
            }

            RemCompDeferred<SquadGrantAccessComponent>(uid);
        }
    }
}
