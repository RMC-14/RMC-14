using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Inventory;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Squads;

public sealed class SquadSystem : EntitySystem
{
    [Dependency] private readonly SharedIdCardSystem _id = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SquadMemberComponent, GetMarineIconEvent>(OnSquadRoleGetIcon);
    }

    private void OnSquadRoleGetIcon(Entity<SquadMemberComponent> member, ref GetMarineIconEvent args)
    {
        args.Background = member.Comp.Background;
        args.BackgroundColor = member.Comp.BackgroundColor;
    }

    public void AssignSquad(EntityUid marine, Entity<SquadTeamComponent?> team, JobComponent? job)
    {
        if (!Resolve(team, ref team.Comp))
            return;

        var member = EnsureComp<SquadMemberComponent>(marine);
        member.Squad = team;
        member.Background = team.Comp.Background;
        member.BackgroundColor = team.Comp.Color;
        Dirty(marine, member);

        var grant = EnsureComp<SquadGrantAccessComponent>(marine);
        grant.AccessLevel = team.Comp.AccessLevel;

        if (job != null &&
            _prototypes.TryIndex(job.Prototype, out var jobProto))
        {
            grant.RoleName = $"{Name(team)} {jobProto.LocalizedName}";
        }

        Dirty(marine, grant);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SquadGrantAccessComponent>();
        while (query.MoveNext(out var uid, out var grant))
        {
            foreach (var item in _inventory.GetHandOrInventoryEntities(uid))
            {
                if (grant.AccessLevel is { Id.Length: > 0 } accessLevel &&
                    TryComp(item, out AccessComponent? access))
                {
                    access.Tags.Add(accessLevel);
                    Dirty(item, access);
                }

                if (TryComp(item, out IdCardComponent? idCard))
                {
                    if (grant.RoleName != null)
                        _id.TryChangeJobTitle(item, grant.RoleName, idCard);

                    if (!EnsureComp<IdCardOwnerComponent>(item, out var owner))
                        owner.Id = uid;
                }
            }

            RemCompDeferred<SquadGrantAccessComponent>(uid);
        }
    }
}
