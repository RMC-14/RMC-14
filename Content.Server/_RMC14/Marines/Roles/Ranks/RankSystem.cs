using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Marines.Roles.Ranks;

public sealed class RankSystem : SharedRankSystem
{
    [Dependency] private readonly PlayTimeTrackingManager _tracking = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RankComponent, TransformSpeakerNameEvent>(OnSpeakerNameTransform);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnSpeakerNameTransform(Entity<RankComponent> ent, ref TransformSpeakerNameEvent args)
    {
        var name = GetSpeakerRankName(ent);
        if (name == null)
            return;

        args.VoiceName = name;
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId == null)
            return;

        if (!_prototypes.TryIndex<JobPrototype>(ev.JobId, out var jobPrototype))
            return;

        if (jobPrototype.Ranks == null)
            return;

        if (!_tracking.TryGetTrackerTimes(ev.Player, out var playTimes))
        {
            // Playtimes haven't loaded.
            Log.Error($"Playtimes weren't ready yet for {ev.Player} on roundstart!");
            playTimes ??= new Dictionary<string, TimeSpan>();
        }

        var profile = ev.Player != null
            ? _preferences.GetPreferences(ev.Player.UserId).SelectedCharacter as HumanoidCharacterProfile
            : HumanoidCharacterProfile.RandomWithSpecies();

        if (profile == null)
            return;

        profile.RankPreferences.TryGetValue(ev.JobId, out var preferredRankId);

        // First pass: try to honour the player's explicit rank preference.
        if (preferredRankId != null)
        {
            if (jobPrototype.Ranks.TryGetValue(preferredRankId.Value, out var preferredRequirements))
            {
                var requirementsMet = true;
                if (preferredRequirements != null)
                {
                    foreach (var req in preferredRequirements)
                    {
                        if (!req.Check(EntityManager, _prototypes, ev.Profile, playTimes, out _))
                        {
                            requirementsMet = false;
                            break;
                        }
                    }
                }

                if (requirementsMet && _prototypes.TryIndex(preferredRankId.Value, out RankPrototype? preferred))
                {
                    SetRank(ev.Mob, preferred);
                    return;
                }
            }
        }

        // Fallback / auto: iterate ranks in definition order & take the first one whose
        // playtime requirements pass. Ranks are defined highest-to-lowest in YAML so this
        // naturally gives the highest rank the player has earned.
        foreach (var (rankProtoId, jobRequirements) in jobPrototype.Ranks)
        {
            if (!_prototypes.TryIndex(rankProtoId, out RankPrototype? rankPrototype))
                continue;

            var failed = false;
            if (jobRequirements != null)
            {
                foreach (var req in jobRequirements)
                {
                    if (!req.Check(EntityManager, _prototypes, ev.Profile, playTimes, out _))
                    {
                        failed = true;
                        break;
                    }
                }
            }

            if (!failed)
            {
                SetRank(ev.Mob, rankPrototype);
                return;
            }
        }
    }
}
