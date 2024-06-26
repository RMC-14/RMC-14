using Content.Server._RMC14.Rules;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Admin;

public sealed class CMAdminEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    [ValidatePrototypeId<StartingGearPrototype>]
    private const string DefaultHumanoidGear = "CMGearRifleman";

    private readonly SharedXenoHiveSystem _hive;
    private readonly MindSystem _mind;
    private readonly StationSpawningSystem _stationSpawning;
    private readonly SharedTransformSystem _transform;
    private readonly XenoSystem _xeno;

    private NetEntity _target;

    public CMAdminEui(EntityUid target)
    {
        IoCManager.InjectDependencies(this);

        _hive = _entities.System<SharedXenoHiveSystem>();
        _mind = _entities.System<MindSystem>();
        _stationSpawning = _entities.System<StationSpawningSystem>();
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

    public override EuiStateBase GetNewState()
    {
        var hives = new List<Hive>();
        var query = _entities.EntityQueryEnumerator<HiveComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var metaData))
        {
            hives.Add(new Hive(_entities.GetNetEntity(uid), metaData.EntityName));
        }

        return new CMAdminEuiState(_target, hives);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case CMAdminChangeHiveMsg changeHive:
            {
                if (_entities.TryGetEntity(_target, out var target) &&
                    _entities.TryGetEntity(changeHive.Hive.Id, out var hive))
                {
                    _xeno.MakeXeno(target.Value);
                    _xeno.SetHive(target.Value, hive.Value);
                }

                break;
            }
            case CMAdminCreateHiveMsg createHive:
            {
                _hive.CreateHive(createHive.Name);
                StateDirty();
                break;
            }
            case CMAdminTransformHumanoidMsg transformHumanoid:
            {
                if (_entities.GetEntity(_target) is not { Valid: true } entity)
                    break;

                var profile = HumanoidCharacterProfile.RandomWithSpecies(transformHumanoid.SpeciesId);
                var coordinates = _transform.GetMoverCoordinates(entity);
                var humanoid = _stationSpawning.SpawnPlayerMob(coordinates, null, profile, null);
                var startingGear = _prototypes.Index<StartingGearPrototype>(DefaultHumanoidGear);
                _stationSpawning.EquipStartingGear(humanoid, startingGear);

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
            case CMAdminTransformXenoMsg transformXeno:
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
