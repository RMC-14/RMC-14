using System.Diagnostics.CodeAnalysis;
using Content.Server.GameTicking.Events;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid.Components;
using Content.Server.Spawners.Components;
using Content.Shared._RMC14.Cryostorage;
using Content.Shared._RMC14.Intel.Tech;
using Content.Shared.Humanoid.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Intel.Tech;

public sealed class ServerTechSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly EntProtoId CombatTechProto = "RMCNewGhostFoxtrotCombatTech";
    private static readonly EntProtoId FireteamLeaderProto = "RMCNewGhostFoxtrotFireteamLeader";
    private static readonly EntProtoId HospitalCorpsmanProto = "RMCNewGhostFoxtrotHospitalCorpsman";
    private static readonly EntProtoId RiflemanProto = "RMCNewGhostFoxtrotRifleman";
    private static readonly EntProtoId SmartGunOperatorProto = "RMCNewGhostFoxtrotSmartGunOperator";
    private static readonly EntProtoId SquadLeaderProto = "RMCNewGhostFoxtrotSquadLeader";
    private static readonly EntProtoId WeaponsSpecialistProto = "RMCNewGhostFoxtrotWeaponsSpecialist";

    private bool _cryoMarinesPurchased = false;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TechCryoMarinesEvent>(OnTechCryoMarines);
        SubscribeLocalEvent<TechCryoSpecEvent>(OnTechCryoSpec);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<RespawnOnCryoComponent, EnteredCryostorageEvent>(OnEnteredCryostorageEvent);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        _cryoMarinesPurchased = false;
    }

    private void OnTechCryoMarines(TechCryoMarinesEvent ev)
    {
        // TODO RMC14 this should spawn you as your character but with random name
        SpawnCryo(_cryoMarinesPurchased ? FireteamLeaderProto : SquadLeaderProto, 1);
        SpawnCryo(CombatTechProto, 1);
        SpawnCryo(HospitalCorpsmanProto, 1);
        SpawnCryo(RiflemanProto, 2);

        _cryoMarinesPurchased = true;
    }

    private void OnTechCryoSpec(TechCryoSpecEvent ev)
    {
        SpawnCryo(WeaponsSpecialistProto, 1);
    }

    private bool GetGhostDirectly(EntityPrototype spawner, [NotNullWhen(true)] out GhostRoleComponent? ghost)
    {
        ghost = default;

        if (!spawner.TryGetComponent(out ghost, _componentFactory))
            return false;

        return true;
    }

    private bool GetGhostViaHumonoid(EntityPrototype spawner, [NotNullWhen(true)] out GhostRoleComponent? ghost)
    {
        ghost = default;

        if (!spawner.TryGetComponent<RandomHumanoidSpawnerComponent>(out var human, _componentFactory) ||
            human.SettingsPrototypeId is null ||
            !_proto.TryIndex<RandomHumanoidSettingsPrototype>(human.SettingsPrototypeId, out var settings) ||
            settings.Components is null ||
            !settings.Components.TryGetComponent("GhostRole", out var ghostI))
            return false;

        ghost = (GhostRoleComponent)ghostI;
        return true;
    }

    private void SpawnCryo(EntProtoId spawnerId, uint amount)
    {
        if (!_proto.TryIndex(spawnerId, out var spawner))
            return;

        if (!GetGhostDirectly(spawner, out var ghost) &&
            !GetGhostViaHumonoid(spawner, out ghost))
            return;

        var spawners = AllEntityQuery<SpawnPointComponent>();
        List<EntityUid> valid = [];
        while (spawners.MoveNext(out var uid, out var comp))
            if (comp.Job == ghost.JobProto)
                valid.Add(uid);

        if (valid.Count == 0)
            return;

        for (var i = 0; i < amount; i++)
        {
            var choice = _random.Pick(valid);
            Spawn(spawnerId, _transform.GetMapCoordinates(choice));
        }
    }

    private void OnEnteredCryostorageEvent(Entity<RespawnOnCryoComponent> ent, ref EnteredCryostorageEvent args)
    {
        SpawnCryo(ent.Comp.Spawner, 1);
    }
}
