using System.Diagnostics.CodeAnalysis;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Shared._RMC14.Preferences;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Preferences;

public sealed class RMCCharacterSelectionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!_mind.TryGetMind(ev.Player, out var mindId, out var mind) || mind.OwnedEntity != ev.Mob)
            return;

        EnsureComp<RMCActiveCharacterComponent>(mindId).Profile = ev.Profile;
    }

    public RMCPlayerSettings GetPlayerSettings(NetUserId player)
    {
        return _preferences.TryGetCachedPreferences(player, out var prefs)
            ? prefs.PlayerSettings
            : RMCPlayerSettings.Default;
    }

    public PreferenceUnavailableMode GetPreferenceUnavailable(NetUserId player)
    {
        return GetPlayerSettings(player).PreferenceUnavailable;
    }

    public HumanoidCharacterProfile GetActiveProfile(ICommonSession player)
    {
        if (_mind.TryGetMind(player, out var mindId, out _) &&
            TryComp(mindId, out RMCActiveCharacterComponent? active) &&
            active.Profile != null)
        {
            return active.Profile;
        }

        return GetDefaultProfile(player.UserId);
    }

    public HumanoidCharacterProfile GetDefaultProfile(NetUserId player)
    {
        if (_preferences.TryGetCachedPreferences(player, out var prefs) &&
            prefs.TryGetHumanoidInSlot(prefs.SelectedCharacterIndex, out var humanoid))
        {
            return humanoid;
        }

        return HumanoidCharacterProfile.Random();
    }

    public Dictionary<ProtoId<JobPrototype>, JobPriority> GetPlayableJobPriorities(NetUserId player)
    {
        var playable = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
        if (!_preferences.TryGetCachedPreferences(player, out var prefs))
            return playable;

        foreach (var (job, priority) in prefs.JobPriorities)
        {
            if (PlayerPreferences.IsCharacterlessJob(job, _prototypes) ||
                HasCharacterForJob(player, prefs, job, null))
            {
                playable[job] = priority;
            }
        }

        return playable;
    }

    public bool TryGetPlayablePriority(NetUserId player, ProtoId<JobPrototype> job, out JobPriority priority)
    {
        priority = JobPriority.Never;
        if (!_preferences.TryGetCachedPreferences(player, out var prefs) ||
            !prefs.JobPriorities.TryGetValue(job, out var found))
        {
            return false;
        }

        if (!PlayerPreferences.IsCharacterlessJob(job, _prototypes) &&
            !HasCharacterForJob(player, prefs, job, null))
        {
            return false;
        }

        priority = found;
        return true;
    }

    public bool HasCharacterForJob(
        NetUserId player,
        ProtoId<JobPrototype> job,
        Predicate<HumanoidCharacterProfile>? filter = null)
    {
        return _preferences.TryGetCachedPreferences(player, out var prefs) &&
               HasCharacterForJob(player, prefs, job, filter);
    }

    public bool TryPickCharacter(
        NetUserId player,
        ProtoId<JobPrototype> job,
        [NotNullWhen(true)] out HumanoidCharacterProfile? profile,
        Predicate<HumanoidCharacterProfile>? filter = null)
    {
        profile = null;
        return _preferences.TryGetCachedPreferences(player, out var prefs) &&
               TryPickCharacter(player, prefs, job, filter, out profile);
    }

    public HumanoidCharacterProfile? ResolveCharacter(
        NetUserId player,
        ProtoId<JobPrototype> job,
        Predicate<HumanoidCharacterProfile>? filter = null)
    {
        if (!_preferences.TryGetCachedPreferences(player, out var prefs))
            return null;

        if (TryPickCharacter(player, prefs, job, filter, out var picked))
            return picked;

        if (prefs.TryGetHumanoidInSlot(prefs.SelectedCharacterIndex, out var selected) &&
            selected.Enabled &&
            IsEligible(player, selected, job, filter))
        {
            return selected;
        }

        var enabled = new List<HumanoidCharacterProfile>();
        foreach (var character in prefs.Characters.Values)
        {
            if (character is HumanoidCharacterProfile { Enabled: true } humanoid &&
                IsEligible(player, humanoid, job, filter))
            {
                enabled.Add(humanoid);
            }
        }

        if (enabled.Count > 0)
            return _random.Pick(enabled);

        return selected;
    }

    public bool TryGetRequestedCharacter(
        NetUserId player,
        int slot,
        ProtoId<JobPrototype> job,
        [NotNullWhen(true)] out HumanoidCharacterProfile? profile)
    {
        profile = null;
        if (!_preferences.TryGetCachedPreferences(player, out var prefs) ||
            !prefs.TryGetHumanoidInSlot(slot, out var requested) ||
            !IsEligible(player, requested, job, null))
        {
            return false;
        }

        profile = requested;
        return true;
    }

    public HumanoidCharacterProfile? ResolveAntagCharacter(NetUserId player, ICollection<ProtoId<AntagPrototype>> antags)
    {
        if (!_preferences.TryGetCachedPreferences(player, out var prefs))
            return null;

        var pool = new List<HumanoidCharacterProfile>();
        foreach (var character in prefs.Characters.Values)
        {
            if (character is not HumanoidCharacterProfile { Enabled: true } humanoid)
                continue;

            foreach (var antag in antags)
            {
                if (!humanoid.AntagPreferences.Contains(antag))
                    continue;

                pool.Add(humanoid);
                break;
            }
        }

        return pool.Count == 0 ? null : _random.Pick(pool);
    }

    private bool HasCharacterForJob(
        NetUserId player,
        PlayerPreferences prefs,
        ProtoId<JobPrototype> job,
        Predicate<HumanoidCharacterProfile>? filter)
    {
        foreach (var (_, profile) in prefs.GetCharactersForJob(job))
        {
            if (IsEligible(player, profile, job, filter))
                return true;
        }

        return false;
    }

    private bool TryPickCharacter(
        NetUserId player,
        PlayerPreferences prefs,
        ProtoId<JobPrototype> job,
        Predicate<HumanoidCharacterProfile>? filter,
        [NotNullWhen(true)] out HumanoidCharacterProfile? profile)
    {
        var pool = new List<HumanoidCharacterProfile>();
        foreach (var (_, character) in prefs.GetCharactersForJob(job))
        {
            if (IsEligible(player, character, job, filter))
                pool.Add(character);
        }

        profile = pool.Count == 0 ? null : _random.Pick(pool);
        return profile != null;
    }

    private bool IsEligible(
        NetUserId player,
        HumanoidCharacterProfile profile,
        ProtoId<JobPrototype> job,
        Predicate<HumanoidCharacterProfile>? filter)
    {
        if (filter != null && !filter(profile))
            return false;

        if (!_player.TryGetSessionById(player, out var session))
            return true;

        return _playTime.IsAllowed(session, job, profile);
    }
}
