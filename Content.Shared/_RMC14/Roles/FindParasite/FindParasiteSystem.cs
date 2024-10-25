using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using Content.Shared.Coordinates;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Roles.FindParasite;
public sealed partial class FindParasiteSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _host = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly AreaSystem _areas = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMGhostComponent, FindParasiteActionEvent>(FindParasites);

        SubscribeLocalEvent<CMGhostComponent, GetAllActiveParasiteSpawnersMessage>(GetAllActiveParasiteSpawners);
        SubscribeLocalEvent<CMGhostComponent, FollowParasiteSpawnerMessage>(FollowParasiteSpawner);
        SubscribeLocalEvent<CMGhostComponent, TakeParasiteRoleMessage>(TakeParasiteRole);
    }

    private void FindParasites(Entity<CMGhostComponent> ghostEnt, ref FindParasiteActionEvent args)
    {
        var ent = args.Performer;

        _ui.OpenUi(args.Action, XenoFindParasiteUI.Key, ent, _net.IsClient);
    }

    private void GetAllActiveParasiteSpawners(Entity<CMGhostComponent> ghostEnt, ref GetAllActiveParasiteSpawnersMessage args)
    {
        var comp = ghostEnt.Comp;
        var eggs = EntityQueryEnumerator<XenoEggComponent>();
        var parasiteThrowers = EntityQueryEnumerator<XenoParasiteThrowerComponent>();

        var spawners = new List<NetEntity>();
        while (eggs.MoveNext(out var ent, out var egg))
        {
            if (egg.State != XenoEggState.Grown)
            {
                continue;
            }

            var netEnt = _entities.GetNetEntity(ent);
            spawners.Add(netEnt);
        }

        while (parasiteThrowers.MoveNext(out var ent, out var parasiteThrower))
        {
            if (parasiteThrower.CurParasites <= parasiteThrower.ReservedParasites &&
                parasiteThrower.CurParasites > 0)
            {
                continue;
            }
            spawners.Add(_entities.GetNetEntity(ent));
        }

        foreach (var spawner in spawners)
        {
            var ent = _entities.GetEntity(spawner);
            var name = MetaData(ent).EntityName;
            var areaName = Loc.GetString("xeno-ui-default-area-name");
            if (_areas.TryGetArea(ent.ToCoordinates(), out AreaComponent? area, out _, out var areaEnt) &&
                areaEnt is EntityUid)
            {
                areaName = MetaData(areaEnt.Value).EntityName;
            }
            name = Loc.GetString("xeno-ui-find-parasite-item",
                    ("itemName", name), ("areaName", areaName));

            comp.ActiveParasiteSpawners.Add(name, spawner);
        }

        if (comp.FindParasiteEntity is null)
        {
            return;
        }
        _ui.SetUiState(comp.FindParasiteEntity.Value, XenoFindParasiteUI.Key, null);

    }
    private void FollowParasiteSpawner(Entity<CMGhostComponent> ghostEnt, ref FollowParasiteSpawnerMessage args)
    {
        var netEnt = args.Entity;
        var ent = _entities.GetEntity(netEnt);

        if (!TryComp(ent, out GhostComponent? ghostComp) ||
            !TryComp(ent, out ActorComponent? actComp) ||
            !_net.IsServer)
        {
            return;
        }

        var followCommand = "follow " + args.Spawner.Id;
        _host.ExecuteCommand(actComp.PlayerSession, followCommand);
    }

    private void TakeParasiteRole(Entity<CMGhostComponent> ghostEnt, ref TakeParasiteRoleMessage args)
    {
        var netEnt = args.Entity;
        var ent = _entities.GetEntity(netEnt);

        var netSpawner = args.Spawner;
        var spawner = _entities.GetEntity(netSpawner);

        var ev = new GetVerbsEvent<ActivationVerb>(ent, spawner, null, null, false, false, new());
        RaiseLocalEvent(ent, ev);

        foreach (var action in ev.Verbs)
        {
            if (action.Text != Loc.GetString("rmc-xeno-egg-ghost-verb") || action.Act is null)
            {
                continue;
            }
            action.Act.Invoke();
            break;
        }
    }
}
