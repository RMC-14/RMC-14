using Content.Server._RMC14.Marines;
using Content.Server._RMC14.Marines.Roles.Ranks;
using Content.Server.Ghost.Roles.Components;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Roles;
using Content.Shared.Access.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Ghost;

public sealed partial class GhostRoleApplySpecialSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly LoadoutSystem _loadout = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly RankSystem _rank = default!;
    [Dependency] private readonly MarineSystem _marine = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostRoleApplySpecialComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<GhostRoleApplySpecialComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<GhostRoleComponent>(ent, out var ghostRole) ||
            !TryComp(ent, out MetaDataComponent? metaData))
            return;

        if (ghostRole.JobProto is not { } jobProto)
            return;

        if (!_prototype.TryIndex(jobProto, out var job))
            return;

        if (job.StartingGear is { } gear)
            _loadout.Equip(ent, [gear], null);

        if (_inventory.TryGetSlotContainer(ent, "id", out var container, out _))
        {
            foreach (var item in container.ContainedEntities)
            {
                if (TryComp<IdCardComponent>(item, out var card))
                {
                    card.FullName = metaData.EntityName;
                    _meta.SetEntityName(item, $"{metaData.EntityName} ({job.LocalizedName})");
                }
            }
        }

        AddComp(ent, new OriginalRoleComponent() { Job = jobProto });
        foreach (var special in job.Special)
            special.AfterEquip(ent);

        if (ent.Comp.Squad is { } squadProto && _squad.TryEnsureSquad(squadProto, out var squad))
        {
            if (_squad.TryGetSquadLeader(squad, out _))
                RemComp<SquadLeaderComponent>(ent);

            _squad.AssignSquad(ent, squad.Owner, jobProto);
        }

        if (job.Ranks is { } ranks)
        {
            foreach (var rank in ranks)
            {
                if (rank.Value is null || rank.Value.Count == 0)
                {
                    _rank.SetRank(ent, rank.Key);
                    break;
                }
            }
        }

        if (HasComp<MarineComponent>(ent) &&
            job.Icon != "CMJobIconEmpty" &&
            _prototype.TryIndex(job.Icon, out var icon))
            _marine.SetMarineIcon(ent, icon.Icon);

        RemComp<GhostRoleApplySpecialComponent>(ent);
    }
}
