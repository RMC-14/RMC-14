using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server._RMC14.Marines.Roles.Ranks;

public sealed class RankSystem : SharedRankSystem
{
    [Dependency] private readonly PlayTimeTrackingManager _tracking = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
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

        bool skipPreferenceEvaluation = false;

        var profile = ev.Player != null ?
            _preferences.GetPreferences(ev.Player.UserId).SelectedCharacter as HumanoidCharacterProfile
            : HumanoidCharacterProfile.RandomWithSpecies();

        if (profile == null)
            return;

        var rankPreferences = profile.RankPreferences != null ?
            profile.RankPreferences : HumanoidCharacterProfile.RandomWithSpecies().RankPreferences;

        if (rankPreferences == null)
            return;

        if (!rankPreferences.TryGetValue(ev.JobId, out int rankPreference))
            skipPreferenceEvaluation = true;

        // We offset i here as our rankPreference has 1 more element than there are ranks. This is because 0 is reserved for Auto and we do not want to check against Auto.
        for (int i = 1; i <= jobPrototype.Ranks.Count; i++)
        {
            var rank = jobPrototype.Ranks.ElementAt(i - 1); // We have to counter the offset here to make sure we grab the right equivilant rank without the offset for Auto.
            var failed = false;
            var jobRequirements = rank.Value;

            if (_prototypes.TryIndex<RankPrototype>(rank.Key, out var rankPrototype) && rankPrototype != null)
            {
                // We can't really havea an enum here to explain the values because there can in theory be infinite values.
                // 0 is reserved for auto which skips the whole rank preference system entirely.
                // (i != jobPrototype.Ranks.Count) makes sure that we at the very least select the default rank
                // even if the client manages to somehow set their rank preference to something higher than what they have unlocked on the server.
                if (!skipPreferenceEvaluation && rankPreference != 0 && i != jobPrototype.Ranks.Count)
                {
                    if (rankPreference != i) failed = true;
                }

                if (jobRequirements != null)
                {
                    foreach (var req in jobRequirements)
                    {
                        if (!req.Check(_entityManager, _prototypes, ev.Profile, playTimes, out _))
                            failed = true;
                    }
                }

                if (!failed)
                {
                    SetRank(ev.Mob, rankPrototype);
                    break;
                }
            }
        }
    }
}
