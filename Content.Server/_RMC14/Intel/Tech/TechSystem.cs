using Content.Server.GameTicking.Events;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid.Components;
using Content.Server.Spawners.Components;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Intel.Tech;
using Content.Shared.Humanoid.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Intel.Tech;

public sealed class ServerTechSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly EntProtoId CombatTechProto = "RMCRandomHumanoidFoxtrotCombatTech";
    private static readonly EntProtoId FireteamLeaderProto = "RMCRandomHumanoidFoxtrotFireteamLeader";
    private static readonly EntProtoId HospitalCorpsmanProto = "RMCRandomHumanoidFoxtrotHospitalCorpsman";
    private static readonly EntProtoId RiflemanProto = "RMCRandomHumanoidFoxtrotRifleman";
    private static readonly EntProtoId SmartGunOperatorProto = "RMCRandomHumanoidFoxtrotSmartGunOperator";
    private static readonly EntProtoId SquadLeaderProto = "RMCRandomHumanoidFoxtrotSquadLeader";
    private static readonly EntProtoId WeaponsSpecialistProto = "RMCRandomHumanoidFoxtrotWeaponsSpecialist";

    private bool _cryoMarinesPurchased = false;
    private int _cryoMarinesScale;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TechCryoMarinesEvent>(OnTechCryoMarines);
        SubscribeLocalEvent<TechCryoSpecEvent>(OnTechCryoSpec);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);

        Subs.CVar(_config, RMCCVars.RMCCryoMarinesScale, v => _cryoMarinesScale = v, true);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        _cryoMarinesPurchased = false;
    }

    private void OnTechCryoMarines(TechCryoMarinesEvent ev)
    {
        var players = (float)_playerManager.PlayerCount;
        var scale = players / _cryoMarinesScale;

        // TODO RMC14 this should spawn you as your character but with random name
        SpawnCryo(_cryoMarinesPurchased ? FireteamLeaderProto : SquadLeaderProto, 1);
        SpawnCryo(CombatTechProto, Scale(scale, 1));
        SpawnCryo(HospitalCorpsmanProto, Scale(scale, 1));
        SpawnCryo(RiflemanProto, Scale(scale, 2));

        _cryoMarinesPurchased = true;
    }

    private void OnTechCryoSpec(TechCryoSpecEvent ev)
    {
        SpawnCryo(WeaponsSpecialistProto, 1);
    }

    private void SpawnCryo(EntProtoId spawnerId, int amount)
    {
        if (!_proto.TryIndex(spawnerId, out var spawner) ||
            !spawner.TryGetComponent<RandomHumanoidSpawnerComponent>(out var human, _componentFactory) ||
            human.SettingsPrototypeId is null ||
            !_proto.TryIndex<RandomHumanoidSettingsPrototype>(human.SettingsPrototypeId, out var settings) ||
            settings.Components is null ||
            !settings.Components.TryGetComponent("GhostRole", out var ghostI))
            return;

        var ghost = (GhostRoleComponent)ghostI;

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

    private static int Scale(float scale, int amount)
    {
        return (int)MathF.Floor((scale + 1) * amount);
    }
}
