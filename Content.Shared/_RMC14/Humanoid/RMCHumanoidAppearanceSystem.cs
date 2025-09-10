using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Station;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Whitelist;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Humanoid;

public sealed class RMCHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedRMCStationSpawningSystem _rmcStationSpawning = default!;

    private EntityUid? _spawnMap;

    public bool HidePlayerIdentities { get; private set; }

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        Subs.CVar(_config, RMCCVars.HidePlayerIdentities, OnHidePlayerIdentitiesChanged, true);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _spawnMap = null;
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!TryComp(ev.Mob, out HumanoidAppearanceComponent? appearance))
            return;

        if (_spawnMap == null || TerminatingOrDeleted(_spawnMap))
            _spawnMap = _map.CreateMap();

        var coords = new EntityCoordinates(_spawnMap.Value, Vector2.Zero);
        var profile = HumanoidCharacterProfile.RandomWithSpecies(appearance.Species);
        var random = _rmcStationSpawning.SpawnPlayerMob(coords, null, profile, null);
        if (!TryComp(random, out HumanoidAppearanceComponent? fakeLook))
            return;

        var hidden = EnsureComp<HiddenAppearanceComponent>(ev.Mob);
        hidden.Appearance = new RMCHumanoidAppearance
        {
            ClientOldMarkings = new(fakeLook.ClientOldMarkings),
            MarkingSet = new(fakeLook.MarkingSet),
            BaseLayers = new(fakeLook.BaseLayers),
            PermanentlyHidden = new(fakeLook.PermanentlyHidden),
            Gender = fakeLook.Gender,
            Age = fakeLook.Age,
            CustomBaseLayers = new(fakeLook.CustomBaseLayers),
            Species = fakeLook.Species,
            SkinColor = fakeLook.SkinColor,
            HiddenLayers = new(fakeLook.HiddenLayers),
            Sex = fakeLook.Sex,
            EyeColor = fakeLook.EyeColor,
            CachedHairColor = fakeLook.CachedHairColor,
            CachedFacialHairColor = fakeLook.CachedFacialHairColor,
            HideLayersOnEquip = new(fakeLook.HideLayersOnEquip),
            UndergarmentTop = fakeLook.UndergarmentTop,
            UndergarmentBottom = fakeLook.UndergarmentBottom,
            MarkingsDisplacement = new(fakeLook.MarkingsDisplacement),
        };

        Dirty(ev.Mob, hidden);
        QueueDel(random);
    }

    private void OnHidePlayerIdentitiesChanged(bool value)
    {
        HidePlayerIdentities = value;
        if (HidePlayerIdentities)
            return;

        var query = EntityQueryEnumerator<HiddenAppearanceComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            RemCompDeferred<HiddenAppearanceComponent>(uid);
        }
    }

    public bool TryGetLocalHiddenAppearance(EntityUid ent, [NotNullWhen(true)] out IRMCHumanoidAppearance? appearance)
    {
        appearance = null;
        if (!TryComp(ent, out HiddenAppearanceComponent? hiddenComp) ||
            hiddenComp.Appearance == null)
        {
            return false;
        }

        appearance = hiddenComp.Appearance;
        return _player.LocalEntity is { } player &&
               _entityWhitelist.IsWhitelistPass(hiddenComp.Whitelist, player);
    }
}
