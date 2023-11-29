using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Shared._CM14.Admin;
using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Hive;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CM14.Admin;

public sealed class CMAdminEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    [ValidatePrototypeId<StartingGearPrototype>]
    private const string DefaultHumanoidGear = "RiflemanGear";

    private readonly XenoHiveSystem _hive;
    private readonly MindSystem _mind;
    private readonly StationSpawningSystem _stationSpawning;
    private readonly SharedTransformSystem _transform;
    private readonly XenoSystem _xeno;

    private readonly NetEntity _target;

    public CMAdminEui(EntityUid target)
    {
        IoCManager.InjectDependencies(this);

        _hive = _entities.System<XenoHiveSystem>();
        _mind = _entities.System<MindSystem>();
        _stationSpawning = _entities.System<StationSpawningSystem>();
        _transform = _entities.System<SharedTransformSystem>();
        _xeno = _entities.System<XenoSystem>();

        _target = _entities.GetNetEntity(target);
    }

    public static bool CanUse(IAdminManager admin, ICommonSession player)
    {
        return admin.HasAdminFlag(player, AdminFlags.Fun);
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
            case CMAdminChangeHiveMessage changeHive:
            {
                if (_entities.TryGetEntity(_target, out var target) &&
                    _entities.TryGetEntity(changeHive.Hive.Id, out var hive))
                {
                    _xeno.MakeXeno(target.Value);
                    _xeno.SetHive(target.Value, hive.Value);
                }

                break;
            }
            case CMAdminCreateHiveMessage createHive:
            {
                _hive.CreateHive(createHive.Name);
                StateDirty();
                break;
            }
            case CMAdminTransformHumanoidMessage transformHumanoid:
            {
                if (Player.AttachedEntity is not { } player)
                    break;

                var profile = HumanoidCharacterProfile.RandomWithSpecies(transformHumanoid.SpeciesId);
                var coordinates = _transform.GetMoverCoordinates(player);
                var humanoid = _stationSpawning.SpawnPlayerMob(coordinates, null, profile, null);
                var startingGear = _prototypes.Index<StartingGearPrototype>(DefaultHumanoidGear);
                _stationSpawning.EquipStartingGear(humanoid, startingGear, profile);

                if (_mind.TryGetMind(player, out var mindId, out var mind))
                {
                    _mind.TransferTo(mindId, humanoid, mind: mind);
                    _mind.UnVisit(mindId, mind);
                }

                _entities.DeleteEntity(player);
                break;
            }
            case CMAdminTransformXenoMessage transformXeno:
            {
                if (Player.AttachedEntity is not { } player)
                    break;

                var coordinates = _transform.GetMoverCoordinates(player);
                var newXeno = _entities.SpawnAttachedTo(transformXeno.XenoId, coordinates);

                if (_mind.TryGetMind(player, out var mindId, out var mind))
                {
                    _mind.TransferTo(mindId, newXeno, mind: mind);
                    _mind.UnVisit(mindId, mind);
                }

                _entities.DeleteEntity(player);
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
