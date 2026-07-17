using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.NamedItems;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Name;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// Character profile. Looks immutable, but uses non-immutable semantics internally for serialization/code sanity purposes.
    /// </summary>
    [DataDefinition]
    [Serializable, NetSerializable]
    public sealed partial class HumanoidCharacterProfile : ICharacterProfile
    {
        private static readonly HashSet<EntProtoId<SquadTeamComponent>> EmptySquadPreferences = [];
        private static readonly Regex RestrictedNameRegex = new(@"[^A-Za-z0-9 '\-]");
        private static readonly Regex ICNameCaseRegex = new(@"^(?<word>\w)|\b(?<word>\w)(?=\w*$)");

        /// <summary>
        /// Job preferences for initial spawn.
        /// </summary>
        [DataField]
        private Dictionary<ProtoId<JobPrototype>, JobPriority> _jobPriorities = new()
        {
            {
                SharedGameTicker.FallbackOverflowJob, JobPriority.High
            }
        };

        /// <summary>
        /// Antags we have opted in to.
        /// </summary>
        [DataField]
        private HashSet<ProtoId<AntagPrototype>> _antagPreferences = new();

        /// <summary>
        /// Enabled traits.
        /// </summary>
        [DataField]
        private HashSet<ProtoId<TraitPrototype>> _traitPreferences = new();

        /// <summary>
        /// When spawning in, decides what type of rank to give based on job. (Also dependent on playtime)
        /// </summary>
        [DataField]
        private Dictionary<ProtoId<JobPrototype>, ProtoId<RankPrototype>?> _rankPreferences = new();

        /// <summary>
        /// <see cref="_loadouts"/>
        /// </summary>
        public IReadOnlyDictionary<string, RoleLoadout> Loadouts => _loadouts;

        [DataField]
        private Dictionary<string, RoleLoadout> _loadouts = new();

        [DataField]
        public string Name { get; set; } = "John Doe";

        /// <summary>
        /// Detailed text that can appear for the character if <see cref="CCVars.FlavorText"/> is enabled.
        /// </summary>
        [DataField]
        public string FlavorText { get; set; } = string.Empty;

        /// <summary>
        /// Associated <see cref="SpeciesPrototype"/> for this profile.
        /// </summary>
        [DataField]
        public ProtoId<SpeciesPrototype> Species { get; set; } = SharedHumanoidAppearanceSystem.DefaultSpecies;

        [DataField]
        public int Age { get; set; } = 18;

        [DataField]
        public Sex Sex { get; private set; } = Sex.Male;

        [DataField]
        public Gender Gender { get; private set; } = Gender.Male;

        /// <summary>
        /// <see cref="Appearance"/>
        /// </summary>
        public ICharacterAppearance CharacterAppearance => Appearance;

        /// <summary>
        /// Stores markings, eye colors, etc for the profile.
        /// </summary>
        [DataField]
        public HumanoidCharacterAppearance Appearance { get; set; } = new();

        /// <summary>
        /// When spawning into a round what's the preferred spot to spawn.
        /// </summary>
        [DataField]
        public SpawnPriorityPreference SpawnPriority { get; private set; } = SpawnPriorityPreference.None;

        /// <summary>
        /// When selecting armor from a vendor, what armor is preferred.
        /// </summary>
        [DataField]
        public ArmorPreference ArmorPreference { get; private set; }

        /// <summary>
        /// <see cref="_rankPreferences"/>
        /// </summary>
        public IReadOnlyDictionary<ProtoId<JobPrototype>, ProtoId<RankPrototype>?> RankPreferences => _rankPreferences;

        /// <summary>
        /// When spawning into a squad role, what squads are preferred.
        /// A null value means "no explicit restriction", which the UI presents as all squads enabled.
        /// </summary>
        [DataField("squadPreferences")]
        private HashSet<EntProtoId<SquadTeamComponent>>? _squadPreferences;

        /// <summary>
        /// Backwardscompatibility for imported/exported profiles that still store a single squad preference, probably uncessary
        /// </summary>
        [DataField("squadPreference")]
        private EntProtoId<SquadTeamComponent>? LegacySquadPreference
        {
            get => SquadPreference;
            set
            {
                if (_squadPreferences != null || value == null)
                    return;

                _squadPreferences = [value.Value];
            }
        }

        /// <summary>
        /// When spawning into a squad role, what squads are preferred
        /// </summary>
        public IReadOnlySet<EntProtoId<SquadTeamComponent>> SquadPreferences => _squadPreferences ?? EmptySquadPreferences;

        /// <summary>
        /// Whether the user has explicitly customized squad selections
        /// </summary>
        public bool HasExplicitSquadPreferences => _squadPreferences != null;

        /// <summary>
        /// Compatibility accessor for older single squad
        /// Returns a squad only when exactly one explicit squad is selected
        /// </summary>
        public EntProtoId<SquadTeamComponent>? SquadPreference
        {
            get
            {
                if (_squadPreferences is not { Count: 1 })
                    return default;

                foreach (var squadPreference in _squadPreferences)
                {
                    return squadPreference;
                }

                return default;
            }
        }

        /// <summary>
        /// If true, only the selected preferred squads may be used for squad jobs
        /// Otherwise the selection is a soft preference and other squads may be chosen as fallback
        /// </summary>
        [DataField]
        public bool OnlyUsePreferredSquads { get; private set; }

        /// <summary>
        /// <see cref="_jobPriorities"/>
        /// </summary>
        public IReadOnlyDictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities => _jobPriorities;

        /// <summary>
        /// Jobs this profile is willing to take.
        /// </summary>
        public IReadOnlySet<ProtoId<JobPrototype>> JobPreferences => _jobPriorities.Keys.ToHashSet();

        /// <summary>
        /// <see cref="_antagPreferences"/>
        /// </summary>
        public IReadOnlySet<ProtoId<AntagPrototype>> AntagPreferences => _antagPreferences;

        /// <summary>
        /// <see cref="_traitPreferences"/>
        /// </summary>
        public IReadOnlySet<ProtoId<TraitPrototype>> TraitPreferences => _traitPreferences;

        /// <summary>
        /// If we're unable to get one of our preferred jobs do we spawn as a fallback job or do we stay in lobby.
        /// </summary>
        [DataField]
        public PreferenceUnavailableMode PreferenceUnavailable { get; private set; } =
            PreferenceUnavailableMode.SpawnAsOverflow;

        [DataField]
        public SharedRMCNamedItems NamedItems { get; private set; } = new();

        [DataField]
        public bool PlaytimePerks { get; private set; } = true;

        [DataField]
        public string XenoPrefix { get; private set; } = string.Empty;

        [DataField]
        public string XenoPostfix { get; private set; } = string.Empty;

        /// <summary>
        /// Whether this character slot is active for multi-slot assignment.
        /// </summary>
        [DataField]
        public bool Enabled { get; private set; } = true;

        /// <summary>
        /// Optional preferred distress-signal planet map for this character.
        /// If set and currently active, this character is preferred for matching jobs.
        /// </summary>
        [DataField]
        public EntProtoId<RMCPlanetMapPrototypeComponent>? PreferredMap { get; private set; }

        public HumanoidCharacterProfile(
            string name,
            string flavortext,
            string species,
            int age,
            Sex sex,
            Gender gender,
            HumanoidCharacterAppearance appearance,
            SpawnPriorityPreference spawnPriority,
            ArmorPreference armorPreference,
            Dictionary<ProtoId<JobPrototype>, ProtoId<RankPrototype>?> rankPreference,
            HashSet<EntProtoId<SquadTeamComponent>>? squadPreferences,
            bool onlyUsePreferredSquads,
            Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities,
            PreferenceUnavailableMode preferenceUnavailable,
            HashSet<ProtoId<AntagPrototype>> antagPreferences,
            HashSet<ProtoId<TraitPrototype>> traitPreferences,
            Dictionary<string, RoleLoadout> loadouts,
            SharedRMCNamedItems namedItems,
            bool playtimePerks,
            string xenoPrefix,
            string xenoPostfix,
            EntProtoId<RMCPlanetMapPrototypeComponent>? preferredMap = null,
            bool enabled = true)
        {
            Name = name;
            FlavorText = flavortext;
            Species = species;
            Age = age;
            Sex = sex;
            Gender = gender;
            Appearance = appearance;
            SpawnPriority = spawnPriority;
            ArmorPreference = armorPreference;
            _rankPreferences = rankPreference;
            _squadPreferences = squadPreferences;
            OnlyUsePreferredSquads = onlyUsePreferredSquads;
            _jobPriorities = jobPriorities;
            PreferenceUnavailable = preferenceUnavailable;
            _antagPreferences = antagPreferences;
            _traitPreferences = traitPreferences;
            _loadouts = loadouts;
            _jobPriorities = SanitizeJobPriorities(_jobPriorities);

            NamedItems = namedItems;
            PlaytimePerks = playtimePerks;
            XenoPrefix = xenoPrefix;
            XenoPostfix = xenoPostfix;
            PreferredMap = preferredMap;
            Enabled = enabled;
        }

        /// <summary>Copy constructor</summary>
        public HumanoidCharacterProfile(HumanoidCharacterProfile other)
            : this(other.Name,
                other.FlavorText,
                other.Species,
                other.Age,
                other.Sex,
                other.Gender,
                other.Appearance.Clone(),
                other.SpawnPriority,
                other.ArmorPreference,
                new Dictionary<ProtoId<JobPrototype>, ProtoId<RankPrototype>?>(other.RankPreferences),
                other._squadPreferences != null ? new HashSet<EntProtoId<SquadTeamComponent>>(other._squadPreferences) : null,
                other.OnlyUsePreferredSquads,
                new Dictionary<ProtoId<JobPrototype>, JobPriority>(other.JobPriorities),
                other.PreferenceUnavailable,
                new HashSet<ProtoId<AntagPrototype>>(other.AntagPreferences),
                new HashSet<ProtoId<TraitPrototype>>(other.TraitPreferences),
                new Dictionary<string, RoleLoadout>(other.Loadouts),
                other.NamedItems,
                other.PlaytimePerks,
                other.XenoPrefix,
                other.XenoPostfix,
                other.PreferredMap,
                other.Enabled)
        {
        }

        /// <summary>
        ///     Get the default humanoid character profile, using internal constant values.
        ///     Defaults to <see cref="SharedHumanoidAppearanceSystem.DefaultSpecies"/> for the species.
        /// </summary>
        /// <returns></returns>
        public HumanoidCharacterProfile()
        {
        }

        /// <summary>
        ///     Return a default character profile, based on species.
        /// </summary>
        /// <param name="species">The species to use in this default profile. The default species is <see cref="SharedHumanoidAppearanceSystem.DefaultSpecies"/>.</param>
        /// <returns>Humanoid character profile with default settings.</returns>
        public static HumanoidCharacterProfile DefaultWithSpecies(string? species = null)
        {
            species ??= SharedHumanoidAppearanceSystem.DefaultSpecies;

            return new()
            {
                Species = species,
            };
        }

        // TODO: This should eventually not be a visual change only.
        public static HumanoidCharacterProfile Random(HashSet<string>? ignoredSpecies = null)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var random = IoCManager.Resolve<IRobustRandom>();

            var species = random.Pick(prototypeManager
                .EnumeratePrototypes<SpeciesPrototype>()
                .Where(x => ignoredSpecies == null ? x.RoundStart : x.RoundStart && !ignoredSpecies.Contains(x.ID))
                .ToArray()
            ).ID;

            return RandomWithSpecies(species);
        }

        public static HumanoidCharacterProfile RandomWithSpecies(string? species = null)
        {
            species ??= SharedHumanoidAppearanceSystem.DefaultSpecies;

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var random = IoCManager.Resolve<IRobustRandom>();

            var sex = Sex.Unsexed;
            var age = 18;
            if (prototypeManager.TryIndex<SpeciesPrototype>(species, out var speciesPrototype))
            {
                sex = random.Pick(speciesPrototype.Sexes);
                age = random.Next(speciesPrototype.MinAge, speciesPrototype.OldAge); // people don't look and keep making 119 year old characters with zero rp, cap it at middle aged
            }

            var gender = Gender.Epicene;

            switch (sex)
            {
                case Sex.Male:
                    gender = Gender.Male;
                    break;
                case Sex.Female:
                    gender = Gender.Female;
                    break;
            }

            var name = GetName(species, gender);

            return new HumanoidCharacterProfile()
            {
                Name = name,
                Sex = sex,
                Age = age,
                Gender = gender,
                Species = species,
                Appearance = HumanoidCharacterAppearance.Random(species, sex),
            };
        }

        public HumanoidCharacterProfile WithName(string name)
        {
            return new(this) { Name = name };
        }

        public HumanoidCharacterProfile WithFlavorText(string flavorText)
        {
            return new(this) { FlavorText = flavorText };
        }

        public HumanoidCharacterProfile WithAge(int age)
        {
            return new(this) { Age = age };
        }

        public HumanoidCharacterProfile WithSex(Sex sex)
        {
            return new(this) { Sex = sex };
        }

        public HumanoidCharacterProfile WithGender(Gender gender)
        {
            return new(this) { Gender = gender };
        }

        public HumanoidCharacterProfile WithSpecies(string species)
        {
            return new(this) { Species = species };
        }


        public HumanoidCharacterProfile WithCharacterAppearance(HumanoidCharacterAppearance appearance)
        {
            return new(this) { Appearance = appearance };
        }

        public HumanoidCharacterProfile WithSpawnPriorityPreference(SpawnPriorityPreference spawnPriority)
        {
            return new(this) { SpawnPriority = spawnPriority };
        }

        public HumanoidCharacterProfile WithArmorPreference(ArmorPreference armorPreference)
        {
            return new(this) { ArmorPreference = armorPreference };
        }

        public HumanoidCharacterProfile WithRankPreference(ProtoId<JobPrototype> jobId, ProtoId<RankPrototype>? rankId)
        {
            var dictionary = new Dictionary<ProtoId<JobPrototype>, ProtoId<RankPrototype>?>(_rankPreferences);

            if (rankId == null)
                dictionary.Remove(jobId);
            else
                dictionary[jobId] = rankId;

            return new(this) { _rankPreferences = dictionary };
        }

        public HumanoidCharacterProfile WithSquadPreference(EntProtoId<SquadTeamComponent>? squadPreference)
        {
            return new(this)
            {
                _squadPreferences = squadPreference == null ? null : [squadPreference.Value],
            };
        }

        public HumanoidCharacterProfile WithSquadPreferences(HashSet<EntProtoId<SquadTeamComponent>>? squadPreferences)
        {
            return new(this)
            {
                _squadPreferences = squadPreferences != null
                    ? new HashSet<EntProtoId<SquadTeamComponent>>(squadPreferences)
                    : null,
            };
        }

        public HumanoidCharacterProfile WithOnlyUsePreferredSquads(bool onlyUsePreferredSquads)
        {
            return new(this) { OnlyUsePreferredSquads = onlyUsePreferredSquads };
        }

        public HumanoidCharacterProfile WithPlaytimePerks(bool playtimePerks)
        {
            return new(this) { PlaytimePerks = playtimePerks };
        }

        public HumanoidCharacterProfile WithXenoPrefix(string prefix)
        {
            return new(this) { XenoPrefix = prefix };
        }

        public HumanoidCharacterProfile WithXenoPostfix(string postfix)
        {
            return new(this) { XenoPostfix = postfix };
        }

        public HumanoidCharacterProfile WithEnabled(bool enabled)
        {
            return new(this) { Enabled = enabled };
        }

        public HumanoidCharacterProfile WithPreferredMap(EntProtoId<RMCPlanetMapPrototypeComponent>? preferredMap)
        {
            return new(this) { PreferredMap = preferredMap };
        }

        public HumanoidCharacterProfile WithJobPriorities(IEnumerable<KeyValuePair<ProtoId<JobPrototype>, JobPriority>> jobPriorities)
        {
            var dictionary = SanitizeJobPriorities(new Dictionary<ProtoId<JobPrototype>, JobPriority>(jobPriorities));

            return new(this)
            {
                _jobPriorities = dictionary
            };
        }

        private static Dictionary<ProtoId<JobPrototype>, JobPriority> SanitizeJobPriorities(
            Dictionary<ProtoId<JobPrototype>, JobPriority> priorities)
        {
            var sanitized = new Dictionary<ProtoId<JobPrototype>, JobPriority>(priorities.Count);
            var hasHighPriority = false;

            foreach (var (job, priority) in priorities)
            {
                if (priority == JobPriority.Never)
                    continue;

                if (priority == JobPriority.High)
                {
                    if (hasHighPriority)
                        sanitized[job] = JobPriority.Medium;
                    else
                    {
                        sanitized[job] = JobPriority.High;
                        hasHighPriority = true;
                    }

                    continue;
                }

                sanitized[job] = priority;
            }

            return sanitized;
        }

        public HumanoidCharacterProfile WithJobPriority(ProtoId<JobPrototype> jobId, JobPriority priority)
        {
            var dictionary = new Dictionary<ProtoId<JobPrototype>, JobPriority>(_jobPriorities);
            if (priority == JobPriority.Never)
            {
                dictionary.Remove(jobId);
            }
            else if (priority == JobPriority.High)
            {
                // There can only ever be one high priority job.
                foreach (var (job, value) in dictionary)
                {
                    if (value == JobPriority.High)
                        dictionary[job] = JobPriority.Medium;
                }

                dictionary[jobId] = priority;
            }
            else
            {
                dictionary[jobId] = priority;
            }

            return new(this)
            {
                _jobPriorities = dictionary,
            };
        }

        public HumanoidCharacterProfile WithPreferenceUnavailable(PreferenceUnavailableMode mode)
        {
            return new(this) { PreferenceUnavailable = mode };
        }

        public HumanoidCharacterProfile WithAntagPreferences(IEnumerable<ProtoId<AntagPrototype>> antagPreferences)
        {
            return new(this)
            {
                _antagPreferences = new (antagPreferences),
            };
        }

        public HumanoidCharacterProfile WithAntagPreference(ProtoId<AntagPrototype> antagId, bool pref)
        {
            var list = new HashSet<ProtoId<AntagPrototype>>(_antagPreferences);
            if (pref)
            {
                list.Add(antagId);
            }
            else
            {
                list.Remove(antagId);
            }

            return new(this)
            {
                _antagPreferences = list,
            };
        }

        public HumanoidCharacterProfile WithTraitPreference(ProtoId<TraitPrototype> traitId, IPrototypeManager protoManager)
        {
            // null category is assumed to be default.
            if (!protoManager.TryIndex(traitId, out var traitProto))
                return new(this);

            var category = traitProto.Category;

            // Category not found so dump it.
            TraitCategoryPrototype? traitCategory = null;

            if (category != null && !protoManager.TryIndex(category, out traitCategory))
                return new(this);

            var list = new HashSet<ProtoId<TraitPrototype>>(_traitPreferences) { traitId };

            if (traitCategory == null || traitCategory.MaxTraitPoints < 0)
            {
                return new(this)
                {
                    _traitPreferences = list,
                };
            }

            var count = 0;
            foreach (var trait in list)
            {
                // If trait not found or another category don't count its points.
                if (!protoManager.TryIndex<TraitPrototype>(trait, out var otherProto) ||
                    otherProto.Category != traitCategory)
                {
                    continue;
                }

                count += otherProto.Cost;
            }

            if (count > traitCategory.MaxTraitPoints && traitProto.Cost != 0)
            {
                return new(this);
            }

            return new(this)
            {
                _traitPreferences = list,
            };
        }

        public HumanoidCharacterProfile WithoutTraitPreference(ProtoId<TraitPrototype> traitId, IPrototypeManager protoManager)
        {
            var list = new HashSet<ProtoId<TraitPrototype>>(_traitPreferences);
            list.Remove(traitId);

            return new(this)
            {
                _traitPreferences = list,
            };
        }

        public string Summary =>
            Loc.GetString(
                "humanoid-character-profile-summary",
                ("name", Name),
                ("gender", Gender.ToString().ToLowerInvariant()),
                ("age", Age)
            );

        public bool MemberwiseEquals(ICharacterProfile maybeOther)
        {
            if (maybeOther is not HumanoidCharacterProfile other) return false;
            if (Name != other.Name) return false;
            if (Age != other.Age) return false;
            if (Sex != other.Sex) return false;
            if (Gender != other.Gender) return false;
            if (Species != other.Species) return false;
            if (PreferenceUnavailable != other.PreferenceUnavailable) return false;
            if (SpawnPriority != other.SpawnPriority) return false;
            if (OnlyUsePreferredSquads != other.OnlyUsePreferredSquads) return false;
            if ((_squadPreferences == null) != (other._squadPreferences == null)) return false;
            if (_squadPreferences != null && !_squadPreferences.SetEquals(other._squadPreferences!)) return false;
            if (!_jobPriorities.SequenceEqual(other._jobPriorities)) return false;
            if (!_antagPreferences.SequenceEqual(other._antagPreferences)) return false;
            if (!_traitPreferences.SequenceEqual(other._traitPreferences)) return false;
            if (!Loadouts.SequenceEqual(other.Loadouts)) return false;
            if (FlavorText != other.FlavorText) return false;
            if (NamedItems != other.NamedItems) return false;
            if (ArmorPreference != other.ArmorPreference) return false;
            if (!_rankPreferences.SequenceEqual(other._rankPreferences)) return false;
            if (PlaytimePerks != other.PlaytimePerks) return false;
            if (XenoPrefix != other.XenoPrefix) return false;
            if (XenoPostfix != other.XenoPostfix) return false;
            if (PreferredMap != other.PreferredMap) return false;
            if (Enabled != other.Enabled) return false;
            return Appearance.MemberwiseEquals(other.Appearance);
        }

        public void EnsureValid(ICommonSession session, IDependencyCollection collection)
        {
            var configManager = collection.Resolve<IConfigurationManager>();
            var prototypeManager = collection.Resolve<IPrototypeManager>();
            var compFactory = collection.Resolve<IComponentFactory>();

            if (!prototypeManager.TryIndex(Species, out var speciesPrototype) || speciesPrototype.RoundStart == false)
            {
                Species = SharedHumanoidAppearanceSystem.DefaultSpecies;
                speciesPrototype = prototypeManager.Index(Species);
            }

            var sex = Sex switch
            {
                Sex.Male => Sex.Male,
                Sex.Female => Sex.Female,
                Sex.Unsexed => Sex.Unsexed,
                _ => Sex.Male // Invalid enum values.
            };

            // ensure the species can be that sex and their age fits the founds
            if (!speciesPrototype.Sexes.Contains(sex))
                sex = speciesPrototype.Sexes[0];

            var age = Math.Clamp(Age, speciesPrototype.MinAge, speciesPrototype.MaxAge);

            var gender = Gender switch
            {
                Gender.Epicene => Gender.Epicene,
                Gender.Female => Gender.Female,
                Gender.Male => Gender.Male,
                Gender.Neuter => Gender.Neuter,
                _ => Gender.Epicene // Invalid enum values.
            };

            string name;
            var maxNameLength = configManager.GetCVar(CCVars.MaxNameLength);
            if (string.IsNullOrEmpty(Name))
            {
                name = GetName(Species, gender);
            }
            else if (Name.Length > maxNameLength)
            {
                name = Name[..maxNameLength];
            }
            else
            {
                name = Name;
            }

            name = name.Trim();

            if (configManager.GetCVar(CCVars.RestrictedNames))
            {
                name = RestrictedNameRegex.Replace(name, string.Empty);
            }

            if (configManager.GetCVar(CCVars.ICNameCase))
            {
                // This regex replaces the first character of the first and last words of the name with their uppercase version
                name = ICNameCaseRegex.Replace(name, m => m.Groups["word"].Value.ToUpper());
            }

            if (string.IsNullOrEmpty(name))
            {
                name = GetName(Species, gender);
            }

            string flavortext;
            var maxFlavorTextLength = configManager.GetCVar(CCVars.MaxFlavorTextLength);
            if (FlavorText.Length > maxFlavorTextLength)
            {
                flavortext = FormattedMessage.RemoveMarkupOrThrow(FlavorText)[..maxFlavorTextLength];
            }
            else
            {
                flavortext = FormattedMessage.RemoveMarkupOrThrow(FlavorText);
            }

            var appearance = HumanoidCharacterAppearance.EnsureValid(Appearance, Species, Sex);

            var prefsUnavailableMode = PreferenceUnavailable switch
            {
                PreferenceUnavailableMode.StayInLobby => PreferenceUnavailableMode.StayInLobby,
                PreferenceUnavailableMode.SpawnAsOverflow => PreferenceUnavailableMode.SpawnAsOverflow,
                _ => PreferenceUnavailableMode.StayInLobby // Invalid enum values.
            };

            var spawnPriority = SpawnPriority switch
            {
                SpawnPriorityPreference.None => SpawnPriorityPreference.None,
                SpawnPriorityPreference.Arrivals => SpawnPriorityPreference.Arrivals,
                SpawnPriorityPreference.Cryosleep => SpawnPriorityPreference.Cryosleep,
                _ => SpawnPriorityPreference.None // Invalid enum values.
            };

            var priorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>(JobPriorities
                .Where(p => prototypeManager.TryIndex<JobPrototype>(p.Key, out var job) && job.SetPreference && p.Value switch
                {
                    JobPriority.Never => false, // Drop never since that's assumed default.
                    JobPriority.Low => true,
                    JobPriority.Medium => true,
                    JobPriority.High => true,
                    _ => false
                }));

            var hasHighPrio = false;
            foreach (var (key, value) in priorities)
            {
                if (value != JobPriority.High)
                    continue;

                if (hasHighPrio)
                    priorities[key] = JobPriority.Medium;
                hasHighPrio = true;
            }

            var antags = AntagPreferences
                .Where(id => prototypeManager.TryIndex(id, out var antag) && antag.SetPreference)
                .ToList();

            var traits = TraitPreferences
                         .Where(prototypeManager.HasIndex)
                         .ToList();

            Name = name;
            FlavorText = flavortext;
            Age = age;
            Sex = sex;
            Gender = gender;
            Appearance = appearance;
            SpawnPriority = spawnPriority;

            var armorPreference = ArmorPreference switch
            {
                ArmorPreference.Random => ArmorPreference.Random,
                ArmorPreference.Padded => ArmorPreference.Padded,
                ArmorPreference.Padless => ArmorPreference.Padless,
                ArmorPreference.Ridged => ArmorPreference.Ridged,
                ArmorPreference.Carrier => ArmorPreference.Carrier,
                ArmorPreference.Skull => ArmorPreference.Skull,
                ArmorPreference.Smooth => ArmorPreference.Smooth,
                _ => ArmorPreference.Random // Invalid enum values.
            };

            ArmorPreference = armorPreference;

            var ranks = new Dictionary<ProtoId<JobPrototype>, ProtoId<RankPrototype>?>(RankPreferences
                .Where(p => prototypeManager.TryIndex<JobPrototype>(p.Key, out var job) && job.SetRankPreference && p.Value != null));

            _rankPreferences.Clear();

            foreach (var (job, rank) in ranks)
            {
                _rankPreferences.Add(job, rank);
            }

            if (_squadPreferences != null)
            {
                var validSquads = new HashSet<EntProtoId<SquadTeamComponent>>();
                foreach (var squadPreference in _squadPreferences)
                {
                    if (!prototypeManager.TryIndex(squadPreference, out var squad) ||
                        !squad.TryGetComponent(out SquadTeamComponent? team, compFactory) ||
                        !team.RoundStart)
                    {
                        continue;
                    }

                    validSquads.Add(squadPreference);
                }

                _squadPreferences = validSquads;
            }

            if (PreferredMap is { } preferredMap)
            {
                if (!prototypeManager.TryIndex(preferredMap, out var preferredMapProto) ||
                    !preferredMapProto.TryGetComponent<RMCPlanetMapPrototypeComponent>(out _, compFactory))
                {
                    PreferredMap = null;
                }
            }

            _jobPriorities.Clear();

            foreach (var (job, priority) in priorities)
            {
                _jobPriorities.Add(job, priority);
            }

            PreferenceUnavailable = prefsUnavailableMode;

            _antagPreferences.Clear();
            _antagPreferences.UnionWith(antags);

            _traitPreferences.Clear();
            _traitPreferences.UnionWith(GetValidTraits(traits, prototypeManager));

            // Checks prototypes exist for all loadouts and dump / set to default if not.
            var toRemove = new ValueList<string>();

            foreach (var (roleName, loadouts) in _loadouts)
            {
                if (!prototypeManager.HasIndex<RoleLoadoutPrototype>(roleName))
                {
                    toRemove.Add(roleName);
                    continue;
                }

                loadouts.EnsureValid(this, session, collection);
            }

            foreach (var value in toRemove)
            {
                _loadouts.Remove(value);
            }

            string? ValidateNamedItem(string? itemName)
            {
                return itemName?.Length > 20 ? itemName[..20] : itemName;
            }

            NamedItems = new SharedRMCNamedItems
            {
                PrimaryGunName = ValidateNamedItem(NamedItems.PrimaryGunName),
                SidearmName = ValidateNamedItem(NamedItems.SidearmName),
                HelmetName = ValidateNamedItem(NamedItems.HelmetName),
                ArmorName = ValidateNamedItem(NamedItems.ArmorName),
                SentryName = ValidateNamedItem(NamedItems.SentryName),
            };

            string ValidateXenoName(string xenoName, bool numberEndingAllowed)
            {
                xenoName = xenoName.ToUpperInvariant();
                for (var i = 0; i < xenoName.Length; i++)
                {
                    var c = xenoName[i];
                    if (i > 0 && numberEndingAllowed && c >= '0' && c <= '9')
                        continue;

                    if (c < 'A' || c > 'Z')
                        return string.Empty;
                }

                return xenoName;
            }

            XenoPrefix = XenoPrefix.Trim();
            XenoPostfix = XenoPostfix.Trim();

            var xenoName = collection.Resolve<IEntityManager>().System<SharedXenoNameSystem>();
            var prefixMax = xenoName.GetMaxXenoPrefixLength(session);
            var postfixMax = xenoName.GetMaxXenoPostfixLength(session);
            if (XenoPrefix.Length > prefixMax)
                XenoPrefix = XenoPrefix[..prefixMax];

            XenoPrefix = ValidateXenoName(XenoPrefix, false);

            if (XenoPrefix.Length > 2)
            {
                XenoPostfix = string.Empty;
            }
            else
            {
                if (XenoPostfix.Length > postfixMax)
                    XenoPostfix = XenoPostfix[..postfixMax];

                XenoPostfix = ValidateXenoName(XenoPostfix, true);
            }
        }

        /// <summary>
        /// Takes in an IEnumerable of traits and returns a List of the valid traits.
        /// </summary>
        public List<ProtoId<TraitPrototype>> GetValidTraits(IEnumerable<ProtoId<TraitPrototype>> traits, IPrototypeManager protoManager)
        {
            // Track points count for each group.
            var groups = new Dictionary<string, int>();
            var result = new List<ProtoId<TraitPrototype>>();

            foreach (var trait in traits)
            {
                if (!protoManager.TryIndex(trait, out var traitProto))
                    continue;

                // Always valid.
                if (traitProto.Category == null)
                {
                    result.Add(trait);
                    continue;
                }

                // No category so dump it.
                if (!protoManager.TryIndex(traitProto.Category, out var category))
                    continue;

                var existing = groups.GetOrNew(category.ID);
                existing += traitProto.Cost;

                // Too expensive.
                if (existing > category.MaxTraitPoints)
                    continue;

                groups[category.ID] = existing;
                result.Add(trait);
            }

            return result;
        }

        public ICharacterProfile Validated(ICommonSession session, IDependencyCollection collection)
        {
            var profile = new HumanoidCharacterProfile(this);
            profile.EnsureValid(session, collection);
            return profile;
        }

        // sorry this is kind of weird and duplicated,
        /// working inside these non entity systems is a bit wack
        public static string GetName(string species, Gender gender)
        {
            var namingSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<NamingSystem>();
            return namingSystem.GetName(species, gender);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is HumanoidCharacterProfile other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_jobPriorities);
            hashCode.Add(_antagPreferences);
            hashCode.Add(_traitPreferences);
            hashCode.Add(_loadouts);
            hashCode.Add(Name);
            hashCode.Add(FlavorText);
            hashCode.Add(Species);
            hashCode.Add(Age);
            hashCode.Add((int)Sex);
            hashCode.Add((int)Gender);
            hashCode.Add(Appearance);
            hashCode.Add((int)SpawnPriority);
            hashCode.Add((int)ArmorPreference);
            hashCode.Add(_rankPreferences);
            hashCode.Add(OnlyUsePreferredSquads);
            hashCode.Add(_squadPreferences);
            hashCode.Add((int)PreferenceUnavailable);
            hashCode.Add(NamedItems);
            hashCode.Add(PlaytimePerks);
            hashCode.Add(XenoPrefix);
            hashCode.Add(XenoPostfix);
            hashCode.Add(PreferredMap);
            hashCode.Add(Enabled);
            return hashCode.ToHashCode();
        }

        public void SetLoadout(RoleLoadout loadout)
        {
            _loadouts[loadout.Role.Id] = loadout;
        }

        public HumanoidCharacterProfile WithLoadout(RoleLoadout loadout)
        {
            // Deep copies so we don't modify the DB profile.
            var copied = new Dictionary<string, RoleLoadout>();

            foreach (var proto in _loadouts)
            {
                if (proto.Key == loadout.Role)
                    continue;

                copied[proto.Key] = proto.Value.Clone();
            }

            copied[loadout.Role] = loadout.Clone();
            var profile = Clone();
            profile._loadouts = copied;
            return profile;
        }

        public RoleLoadout GetLoadoutOrDefault(string id, ICommonSession? session, ProtoId<SpeciesPrototype>? species, IEntityManager entManager, IPrototypeManager protoManager)
        {
            if (!_loadouts.TryGetValue(id, out var loadout))
            {
                loadout = new RoleLoadout(id);
                loadout.SetDefault(this, session, protoManager, force: true);
            }

            loadout.SetDefault(this, session, protoManager);
            return loadout;
        }

        public HumanoidCharacterProfile WithNamedItems(SharedRMCNamedItems named)
        {
            var profile = Clone();
            profile.NamedItems = named;
            return profile;
        }

        public HumanoidCharacterProfile Clone()
        {
            return new HumanoidCharacterProfile(this);
        }
    }
}
