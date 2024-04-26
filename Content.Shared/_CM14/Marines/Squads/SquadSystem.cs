using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Inventory;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Marines.Squads;

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

    public void SetSquad(EntityUid marine, Entity<SquadTeamComponent?> team, JobComponent? job)
    {
        if (!Resolve(team, ref team.Comp))
            return;

        var member = EnsureComp<SquadMemberComponent>(marine);
        member.Squad = team;
        member.Background = team.Comp.Background;
        member.BackgroundColor = team.Comp.Color;

        foreach (var item in _inventory.GetHandOrInventoryEntities(marine))
        {
            if (team.Comp.AccessLevel != default &&
                TryComp(item, out AccessComponent? access))
            {
                access.Tags.Add(team.Comp.AccessLevel);
                Dirty(item, access);
            }

            if (job != null &&
                _prototypes.TryIndex(job.Prototype, out var jobProto) &&
                TryComp(item, out IdCardComponent? idCard))
            {
                _id.TryChangeJobTitle(item, $"{Name(team)} {jobProto.LocalizedName}", idCard);
            }
        }

        Dirty(marine, member);
    }
}
