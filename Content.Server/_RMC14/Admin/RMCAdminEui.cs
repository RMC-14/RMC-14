using System.Linq;
using Content.Server._RMC14.Rules;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Mind;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Admin;

public sealed class RMCAdminEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    private readonly RMCAdminSystem _rmcAdmin;
    private readonly SharedXenoHiveSystem _hive;
    private readonly MindSystem _mind;
    private readonly SquadSystem _squad;
    private readonly SharedTransformSystem _transform;
    private readonly XenoSystem _xeno;

    private NetEntity _target;

    public RMCAdminEui(EntityUid target)
    {
        IoCManager.InjectDependencies(this);

        _rmcAdmin = _entities.System<RMCAdminSystem>();
        _hive = _entities.System<SharedXenoHiveSystem>();
        _mind = _entities.System<MindSystem>();
        _squad = _entities.System<SquadSystem>();
        _transform = _entities.System<SharedTransformSystem>();
        _xeno = _entities.System<XenoSystem>();

        _target = _entities.GetNetEntity(target);
    }

    public override void Opened()
    {
        _admin.OnPermsChanged += OnAdminPermsChanged;
        StateDirty();
    }

    public override void Closed()
    {
        _admin.OnPermsChanged -= OnAdminPermsChanged;
    }

    public static EuiStateBase CreateState(IEntityManager entities)
    {
        var squadSys = entities.System<SquadSystem>();
        var hives = new List<Hive>();
        var hiveQuery = entities.EntityQueryEnumerator<HiveComponent, MetaDataComponent>();
        while (hiveQuery.MoveNext(out var uid, out _, out var metaData))
        {
            hives.Add(new Hive(entities.GetNetEntity(uid), metaData.EntityName));
        }

        var squads = new List<Squad>();
        foreach (var squadProto in squadSys.SquadPrototypes)
        {
            var exists = false;
            var members = 0;
            if (squadSys.TryGetSquad(squadProto, out var squad))
            {
                exists = true;
                members = squadSys.GetSquadMembersAlive(squad);
            }

            squads.Add(new Squad(squadProto, exists, members));
        }

        var mobState = entities.System<MobStateSystem>();
        var xenos = new List<Xeno>();
        var xenoQuery = entities.EntityQueryEnumerator<XenoComponent, MetaDataComponent>();
        while (xenoQuery.MoveNext(out var uid, out _, out var metaData))
        {
            if (metaData.EntityPrototype is not { } proto)
                continue;

            if (mobState.IsDead(uid))
                continue;

            xenos.Add(new Xeno(proto));
        }

        var marines = 0;
        var marinesQuery = entities.EntityQueryEnumerator<MarineComponent>();
        while (marinesQuery.MoveNext(out var uid, out _))
        {
            if (mobState.IsDead(uid))
                continue;

            marines++;
        }

        var marinesPerXeno = entities.System<CMDistressSignalRuleSystem>().MarinesPerXeno.ToDictionary();

        return new RMCAdminEuiState(hives, squads, xenos, marines, marinesPerXeno);
    }

    public override EuiStateBase GetNewState()
    {
        return CreateState(_entities);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case RMCAdminChangeHiveMsg changeHive:
            {
                if (_entities.TryGetEntity(_target, out var target) &&
                    _entities.TryGetEntity(changeHive.Hive.Id, out var hive))
                {
                    _xeno.MakeXeno(target.Value);
                    _xeno.SetHive(target.Value, hive.Value);
                }

                break;
            }
            case RMCAdminCreateHiveMsg createHive:
            {
                _hive.CreateHive(createHive.Name);
                StateDirty();
                break;
            }
            case RMCAdminTransformHumanoidMsg transformHumanoid:
            {
                if (_entities.GetEntity(_target) is not { Valid: true } entity)
                    break;

                var humanoid = _rmcAdmin.RandomizeMarine(entity, transformHumanoid.SpeciesId);
                if (_mind.TryGetMind(entity, out var mindId, out var mind))
                {
                    _mind.TransferTo(mindId, humanoid, mind: mind);
                    _mind.UnVisit(mindId, mind);
                }

                _entities.DeleteEntity(entity);
                _entities.EnsureComponent<MarineComponent>(humanoid);
                _target = _entities.GetNetEntity(humanoid);
                StateDirty();
                break;
            }
            case RMCAdminTransformXenoMsg transformXeno:
            {
                if (_entities.GetEntity(_target) is not { Valid: true } entity)
                    break;

                var coordinates = _transform.GetMoverCoordinates(entity);
                var newXeno = _entities.SpawnAttachedTo(transformXeno.XenoId, coordinates);
                if (_entities.TryGetComponent(entity, out XenoComponent? xeno))
                    _xeno.SetHive(newXeno, xeno.Hive);
                else if (_entities.EntityQuery<CMDistressSignalRuleComponent>().TryFirstOrDefault(out var ruleComponent))
                    _xeno.SetHive(newXeno, ruleComponent.Hive);

                if (_mind.TryGetMind(entity, out var mindId, out var mind))
                {
                    _mind.TransferTo(mindId, newXeno, mind: mind);
                    _mind.UnVisit(mindId, mind);
                }

                _entities.DeleteEntity(entity);
                _target = _entities.GetNetEntity(newXeno);
                StateDirty();
                break;
            }
            case RMCAdminCreateSquadMsg createSquad:
            {
                _squad.TryEnsureSquad(createSquad.SquadId, out _);
                StateDirty();
                break;
            }
            case RMCAdminAddToSquadMsg addToSquad:
            {
                if (!_entities.TryGetEntity(_target, out var target) ||
                    !_squad.TryEnsureSquad(addToSquad.SquadId, out var squad))
                {
                    break;
                }

                _squad.AssignSquad(target.Value, (squad, squad), null);
                StateDirty();
                break;
            }
        }
    }

    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        if (!_admin.HasAdminFlag(Player, AdminFlags.Fun))
        {
            Close();
            return;
        }

        StateDirty();
    }
}
