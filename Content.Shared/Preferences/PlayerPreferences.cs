using System.Linq;
using Content.Shared._RMC14.Rules;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences
{
    /// <summary>
    ///     Contains all player characters and the index of the currently selected character.
    ///     Serialized both over the network and to disk.
    /// </summary>
    [Serializable]
    [NetSerializable]
    public sealed class PlayerPreferences
    {
        private Dictionary<int, ICharacterProfile> _characters;

        public PlayerPreferences(IEnumerable<KeyValuePair<int, ICharacterProfile>> characters, int selectedCharacterIndex, Color adminOOCColor, List<ProtoId<ConstructionPrototype>> constructionFavorites)
            : this(characters, selectedCharacterIndex, adminOOCColor, constructionFavorites, GetSelectedHumanoidJobPriorities(characters, selectedCharacterIndex))
        {
        }

        public PlayerPreferences(
            IEnumerable<KeyValuePair<int, ICharacterProfile>> characters,
            int selectedCharacterIndex,
            Color adminOOCColor,
            List<ProtoId<ConstructionPrototype>> constructionFavorites,
            Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
        {
            _characters = new Dictionary<int, ICharacterProfile>(characters);
            SelectedCharacterIndex = selectedCharacterIndex;
            AdminOOCColor = adminOOCColor;
            ConstructionFavorites = constructionFavorites;
            JobPriorities = SanitizeJobPriorities(jobPriorities);
        }

        // Transitional constructor for multi-slot ports while keeping selected-character compatibility.
        public PlayerPreferences(
            IEnumerable<KeyValuePair<int, ICharacterProfile>> characters,
            Color adminOOCColor,
            List<ProtoId<ConstructionPrototype>> constructionFavorites,
            Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
            : this(
                characters,
                characters.Any() ? characters.Min(kvp => kvp.Key) : 0,
                adminOOCColor,
                constructionFavorites,
                jobPriorities)
        {
        }

        /// <summary>
        ///     All player characters.
        /// </summary>
        public IReadOnlyDictionary<int, ICharacterProfile> Characters => _characters;

        public ICharacterProfile GetProfile(int index)
        {
            return _characters[index];
        }

        /// <summary>
        ///     Index of the currently selected character.
        /// </summary>
        public int SelectedCharacterIndex { get; }

        /// <summary>
        ///     The currently selected character.
        /// </summary>
        public ICharacterProfile SelectedCharacter => Characters[SelectedCharacterIndex];

        /// <summary>
        /// Player-level job priorities used by multi-slot job assignment.
        /// </summary>
        public Dictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities { get; set; } = [];

        public Color AdminOOCColor { get; set; }

        /// <summary>
        ///    List of favorite items in the construction menu.
        /// </summary>
        public List<ProtoId<ConstructionPrototype>> ConstructionFavorites { get; set; } = [];

        public int IndexOfCharacter(ICharacterProfile profile)
        {
            return _characters.FirstOrNull(p => p.Value == profile)?.Key ?? -1;
        }

        public bool TryIndexOfCharacter(ICharacterProfile profile, out int index)
        {
            return (index = IndexOfCharacter(profile)) != -1;
        }

        /// <summary>
        /// Get job priorities filtered by whether any enabled profile can actually take that job.
        /// </summary>
        public Dictionary<ProtoId<JobPrototype>, JobPriority> JobPrioritiesFiltered()
        {
            var allCharacterJobs = new HashSet<ProtoId<JobPrototype>>();
            foreach (var profile in Characters.Values)
            {
                if (profile is not HumanoidCharacterProfile { Enabled: true } humanoid)
                    continue;

                allCharacterJobs.UnionWith(humanoid.JobPreferences);
            }

            var filteredPlayerJobs = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
            foreach (var (job, priority) in JobPriorities)
            {
                if (!allCharacterJobs.Contains(job))
                    continue;

                filteredPlayerJobs.Add(job, priority);
            }

            return filteredPlayerJobs;
        }

        /// <summary>
        /// Given a job, return a random enabled character asking for this job.
        /// </summary>
        public HumanoidCharacterProfile? SelectProfileForJob(
            ProtoId<JobPrototype> job,
            EntProtoId<RMCPlanetMapPrototypeComponent>? currentMap = null,
            Predicate<HumanoidCharacterProfile>? filter = null)
        {
            List<HumanoidCharacterProfile> pool = [];
            foreach (var profile in Characters.Values)
            {
                if (profile is not HumanoidCharacterProfile { Enabled: true } humanoid)
                    continue;

                if (!humanoid.JobPreferences.Contains(job))
                    continue;

                if (filter != null && !filter(humanoid))
                    continue;

                pool.Add(humanoid);
            }

            var random = IoCManager.Resolve<IRobustRandom>();
            if (pool.Count == 0)
                return null;

            if (currentMap is { } map)
            {
                var mapPreferredPool = pool
                    .Where(profile => profile.PreferredMap == map)
                    .ToList();

                if (mapPreferredPool.Count > 0)
                    return random.Pick(mapPreferredPool);
            }

            return random.Pick(pool);
        }

        public Dictionary<int, HumanoidCharacterProfile> GetAllProfilesForJob(JobPrototype job)
        {
            var profiles = new Dictionary<int, HumanoidCharacterProfile>();

            foreach (var (slot, profile) in Characters)
            {
                if (profile is not HumanoidCharacterProfile humanoid)
                    continue;

                if (!humanoid.JobPreferences.Contains(job.ID))
                    continue;

                profiles[slot] = humanoid;
            }

            return profiles;
        }

        public Dictionary<int, HumanoidCharacterProfile> GetAllEnabledProfilesForJob(JobPrototype job)
        {
            var profiles = new Dictionary<int, HumanoidCharacterProfile>();

            foreach (var (slot, profile) in Characters)
            {
                if (profile is not HumanoidCharacterProfile { Enabled: true } humanoid)
                    continue;

                if (!humanoid.JobPreferences.Contains(job.ID))
                    continue;

                profiles[slot] = humanoid;
            }

            return profiles;
        }

        /// <summary>
        /// Given antags, return a random enabled character asking for at least one.
        /// </summary>
        public HumanoidCharacterProfile? SelectProfileForAntag(ICollection<ProtoId<AntagPrototype>> antags)
        {
            var pool = new HashSet<HumanoidCharacterProfile>();
            foreach (var profile in Characters.Values)
            {
                if (profile is not HumanoidCharacterProfile { Enabled: true } humanoid)
                    continue;

                foreach (var antag in antags)
                {
                    if (humanoid.AntagPreferences.Contains(antag))
                        pool.Add(humanoid);
                }
            }

            var random = IoCManager.Resolve<IRobustRandom>();
            return pool.Count == 0 ? null : random.Pick(pool);
        }

        public bool TryGetHumanoidInSlot(int slot, out HumanoidCharacterProfile? humanoid)
        {
            humanoid = null;
            if (!Characters.TryGetValue(slot, out var profile))
                return false;

            humanoid = profile as HumanoidCharacterProfile;
            return humanoid != null;
        }

        private static Dictionary<ProtoId<JobPrototype>, JobPriority> GetSelectedHumanoidJobPriorities(
            IEnumerable<KeyValuePair<int, ICharacterProfile>> characters,
            int selectedCharacterIndex)
        {
            var characterDict = new Dictionary<int, ICharacterProfile>(characters);

            if (!characterDict.TryGetValue(selectedCharacterIndex, out var profile))
                return [];

            if (profile is not HumanoidCharacterProfile humanoid)
                return [];

            return humanoid.JobPriorities
                .Where(kvp => kvp.Value != JobPriority.Never)
                .ToDictionary();
        }

        private static Dictionary<ProtoId<JobPrototype>, JobPriority> SanitizeJobPriorities(
            Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
        {
            var result = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
            var hasHigh = false;

            foreach (var (job, priority) in jobPriorities)
            {
                if (priority == JobPriority.Never)
                    continue;

                if (priority == JobPriority.High)
                {
                    if (hasHigh)
                        result[job] = JobPriority.Medium;
                    else
                    {
                        result[job] = JobPriority.High;
                        hasHigh = true;
                    }

                    continue;
                }

                result[job] = priority;
            }

            return result;
        }
    }
}
