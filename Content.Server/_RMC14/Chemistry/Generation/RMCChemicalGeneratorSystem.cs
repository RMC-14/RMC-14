using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Content.Shared._RMC14.Chemistry.Effects;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Intel;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Paper;
using Robust.Server.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Reagent = Content.Shared._RMC14.Chemistry.Reagent.Reagent;

namespace Content.Server._RMC14.Chemistry.Generation;
// INSANITY BELOW
public sealed record ContractStats(
    string Name,
    string Color,
    string PhysicalDesc,
    List<(ChemGeneratorPropertyPrototype Property, int Level)> Properties,
    int Overdose,
    int CriticalOverdose,
    string PropertyHintId,
    string IngredientHintId,
    int CreditReward);

public sealed class RMCChemicalGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IntelSystem _intel = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RMCReagentPrototypeSyncSystem _protoSync = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCChemistryResearchSystem _research = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;

    private static readonly EntProtoId ReportProto = "RMCChemResearchReport";

    private int _generatedCount;
    private readonly Dictionary<string, int> _generatedTiers = new();
    private readonly Dictionary<string, string> _originalIds = new();
    private readonly Dictionary<string, ReactionIndicator> _reactionIndicators = new();

    private readonly Dictionary<string, ProtoId<ReagentPrototype>> _simulationResults = new();

    // legendary/ciphering recipes are rolled fresh once per round
    private static readonly string[] LegendaryPropertyIds = ["Hypergenetic", "Boosting", "Regulating", "Optimized"];
    private static readonly ProtoId<ChemGeneratorPropertyPrototype> CipheringId = "Ciphering";
    private static readonly ProtoId<ChemGeneratorPropertyPrototype> EncryptedId = "Encrypted";
    private readonly Dictionary<string, List<string>> _legendaryCombines = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        RollLegendaryCombines();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        RollLegendaryCombines();

        foreach (var properties in _generatedProperties.Values)
        {
            properties.Clear();
        }

        _generatedTiers.Clear();
        _originalIds.Clear();
        _reactionIndicators.Clear();
        _simulationResults.Clear();
        _recipeReactantsCache.Clear();
        _generatedNameCache = null;
        _classPoolCache = null;
    }

    // LEGENDARY_COMBINE_PROPERTIES = 3 random properties per legendary, drawn from the same
    // pool PickProperties uses. Ciphering is a separate case sharing the mechanism: 2 random
    // properties plus a hardcoded 3rd slot of encrypted
    private void RollLegendaryCombines()
    {
        _legendaryCombines.Clear();

        var pool = _prototype.EnumeratePrototypes<ChemGeneratorPropertyPrototype>()
            .Where(p => !p.Flags.HasFlag(ChemPropertyType.Disabled) && p.Rarity != ChemPropertyRarity.Rare)
            .ToList();

        var categories = new[] { ChemPropertyCategory.Neutral, ChemPropertyCategory.Positive, ChemPropertyCategory.Negative }
            .Select(category => pool.Where(p => p.Category == category).Select(p => p.ID).ToList())
            .Where(ids => ids.Count > 0)
            .ToList();

        if (categories.Count == 0)
            return;

        foreach (var legendary in LegendaryPropertyIds)
        {
            _legendaryCombines[legendary] = RollCombinePieces(categories, 3);
        }

        var cipherPieces = RollCombinePieces(categories, 2);
        cipherPieces.Add(EncryptedId.Id);
        _legendaryCombines[CipheringId.Id] = cipherPieces;
    }

    private List<string> RollCombinePieces(List<List<string>> categories, int count)
    {
        var pieces = new List<string>();
        for (var i = 0; i < count; i++)
        {
            pieces.Add(_random.Pick(_random.Pick(categories)));
        }

        return pieces;
    }

    public bool TryGetLegendaryRecipe(string propertyId, [NotNullWhen(true)] out List<string>? pieces) =>
        _legendaryCombines.TryGetValue(propertyId, out pieces);

    // tracked for the life of the round so low tier chems dont keep
    // rolling the same property, until every property in that category has appeared at least once
    private readonly Dictionary<ChemPropertyCategory, HashSet<string>> _generatedProperties = new()
    {
        [ChemPropertyCategory.Positive] = new(),
        [ChemPropertyCategory.Negative] = new(),
        [ChemPropertyCategory.Neutral] = new(),
    };

    public bool IsGenerated(ProtoId<ReagentPrototype> id) => _generatedTiers.ContainsKey(id.Id);

    public bool TryGetGeneratedTier(ProtoId<ReagentPrototype> id, out int tier) => _generatedTiers.TryGetValue(id.Id, out tier);

    public bool TryGetOriginalId(string id, [NotNullWhen(true)] out string? originalId) => _originalIds.TryGetValue(id, out originalId);

    public IEnumerable<string> GetVariantsOf(string baseReagentId) => _originalIds.Where(kv => kv.Value == baseReagentId).Select(kv => kv.Key);

    private static readonly string[] Prefix =
    [
        "Alph", "At", "Aw", "Az", "Bar", "Bet", "Bren", "Bor", "Cak", "Cal", "Cath", "Cet", "Con", "Dal", "De", "Dean",
        "Delt", "Dox", "Em", "Ep", "Ethr", "Far", "Feth", "Fohs", "Het", "Hydr", "Hyper", "Ian", "In", "Iot", "Jam",
        "Kan", "Kap", "Kil", "Kl", "Kol", "Lar", "Lin", "Loz", "Meg", "Meth", "Mn", "Mnem", "Mit", "Mor", "Mut", "Nath",
        "Neth", "Nov", "Nux", "Pas", "Pax", "Per", "Ph", "Phr", "Quaz", "Rath", "Ret", "Rol", "Sal", "Sig", "Stim",
        "Super", "Tau", "Te", "Th", "Thet", "Throx", "Ul", "Up", "Ven", "Vic", "Xen", "Xeon", "Xit", "Xir", "Zar",
        "Zet", "Zol",
    ];

    private static readonly string[] WordRoot =
    [
        "a", "adia", "agha", "ama", "amma", "anthra", "ara", "ata", "ade", "alde", "ale", "ange", "arpe", "anthe",
        "aze", "azide", "adefi", "ami", "ari", "assi", "athi", "avi", "azi", "ado", "alope", "arpo", "alto", "aquo",
        "agu", "alnu", "aphnu", "axu", "adhy", "any", "aphy", "awy", "e", "ea", "ega", "enna", "epina", "era", "erma",
        "etha", "eva", "exa", "edre", "etre", "epe", "ephe", "exe", "efi", "emni", "esli", "eno", "elto", "eto", "evo",
        "elnu", "epu", "ethu", "equ", "edry", "ely", "ety", "evy", "i", "ia", "ie", "iqua", "ipha", "ista", "ira",
        "itra", "iphe", "imne", "ine", "isse", "ihi", "ini", "itri", "izi", "ico", "iglo", "iho", "ino", "isto",
        "immu", "inu", "ixu", "izu", "idy", "immy", "ithy", "ity", "isty", "o", "oga", "olna", "omna", "ora", "oxa",
        "oghe", "one", "ore", "oste", "ove", "oi", "ogi", "oni", "orphi", "owni", "oxi", "ocho", "ommo", "osto",
        "odru", "oru", "owu", "oxu", "odry", "omny", "oxy", "u", "ua", "uga", "unga", "uppa", "ura", "ugre", "une",
        "use", "uve", "udri", "uli", "unni", "utri", "uthri", "uo", "uclo", "uno", "uto", "uu", "uphu", "umnu", "uru",
        "udry", "uly", "upy", "urvy", "y", "ya", "ydra", "yla", "ytha", "yna", "yxa", "ye", "ylfe", "ymne", "ylre",
        "yse", "ytre", "yani", "yphi", "ypi", "yvi", "yo", "ylo", "ypho", "yro", "ydro", "ynu", "ysto", "ytro", "yxu",
        "yru", "ydy",
    ];

    private static readonly string[] Suffix =
    [
        "cin", "cene", "cin", "cine", "cone", "din", "dine", "dol", "done", "drate", "drine", "fen", "fene", "fic",
        "gar", "gen", "l", "lem", "lin", "line", "lis", "mel", "mide", "mite", "mol", "n", "ne", "nt", "phrine", "r",
        "rene", "rine", "ry", "ride", "rium", "rus", "sene", "sine", "sone", "sp", "stol", "tane", "tant", "tene",
        "tine", "thol", "tic", "um", "vene", "vone", "x", "xene", "xin", "xine", "zene", "zine", "zone",
    ];

    private void BroadcastGenerated(
        string id,
        string name,
        string color,
        string physicalDesc,
        List<(ChemGeneratorPropertyPrototype Property, int Level)> properties,
        int overdose,
        int criticalOverdose,
        List<(string Id, int Amount, bool Catalyst)> ingredients,
        ChemClass chemClass,
        ReactionIndicator indicator,
        int creditReward = 2)
    {
        var data = new RMCGeneratedReagentData(
            id,
            name,
            color,
            physicalDesc,
            properties.Select(p => new ChemReportProperty { PropertyId = p.Property.ID, Level = p.Level }).ToList(),
            overdose,
            criticalOverdose,
            ingredients.Select(i => new RecipeCandidateIngredient { Id = i.Id, Amount = i.Amount, Catalyst = i.Catalyst }).ToList(),
            chemClass,
            indicator,
            creditReward);

        _protoSync.Broadcast(data);

        if (chemClass >= ChemClass.Special)
            _intel.AddPendingChemical(id, name);
    }

    private static bool IsWhiteHot(List<(ChemGeneratorPropertyPrototype Property, int Level)> properties)
    {
        var stats = new RMCFireStatsBuilder();
        foreach (var (property, level) in properties)
        {
            if (property.Effect is RMCChemicalEffect effect)
                effect.ModifyFireStats(ref stats, level);
        }

        return stats.HasFireProperty && stats.Intensity >= 50f;
    }

    public ProtoId<ReagentPrototype> GenerateReagent(int tier, ChemClass chemClass = ChemClass.Special)
    {
        tier = Math.Clamp(tier, 1, 3);

        var name = GenerateName();
        var id = $"RMCGenerated{++_generatedCount}{name.Replace(" ", "")}";
        var color = $"#{_random.Next(0, 256):X2}{_random.Next(0, 256):X2}{_random.Next(0, 256):X2}";

        var properties = PickProperties(tier);
        if (IsWhiteHot(properties))
            color = "#FFFFFF";

        var (overdose, criticalOverdose) = RollOverdose(tier);

        var ingredients = PickRecipeIngredients(tier, id);
        var physicalDesc = _random.Pick(PhysicalDescriptions);
        var indicator = RollReactionIndicator();

        BroadcastGenerated(id, name, color, physicalDesc, properties, overdose, criticalOverdose, ingredients, chemClass, indicator);
        if (chemClass == ChemClass.Special)
            AppendToClassPoolCache(chemClass, id);

        CacheRecipeReactants(id, ingredients);
        _generatedTiers[id] = tier;
        _reactionIndicators[id] = indicator;

        return id;
    }

    public ProtoId<ReagentPrototype> GenerateReagentWithProperties(
        List<(ChemGeneratorPropertyPrototype Property, int Level)> properties,
        int tier = 1,
        int? overdose = null,
        int? criticalOverdose = null)
    {
        tier = Math.Clamp(tier, 1, 3);

        var name = GenerateName();
        var id = $"RMCGenerated{++_generatedCount}{name.Replace(" ", "")}";
        var color = $"#{_random.Next(0, 256):X2}{_random.Next(0, 256):X2}{_random.Next(0, 256):X2}";
        if (IsWhiteHot(properties))
            color = "#FFFFFF";

        var (rolledOverdose, rolledCriticalOverdose) = RollOverdose(tier);
        var finalOverdose = overdose ?? rolledOverdose;
        var finalCriticalOverdose = criticalOverdose ?? rolledCriticalOverdose;

        var ingredients = PickRecipeIngredients(tier, id);
        var physicalDesc = _random.Pick(PhysicalDescriptions);
        var indicator = RollReactionIndicator();

        BroadcastGenerated(id, name, color, physicalDesc, properties, finalOverdose, finalCriticalOverdose, ingredients, ChemClass.Special, indicator);
        AppendToClassPoolCache(ChemClass.Special, id);
        CacheRecipeReactants(id, ingredients);
        _generatedTiers[id] = tier;
        _reactionIndicators[id] = indicator;

        return id;
    }

    public ContractStats RollStats(int tier)
    {
        tier = Math.Clamp(tier, 1, 3);

        var name = GenerateName();
        var color = $"#{_random.Next(0, 256):X2}{_random.Next(0, 256):X2}{_random.Next(0, 256):X2}";
        var properties = PickProperties(tier);
        if (IsWhiteHot(properties))
            color = "#FFFFFF";

        var (overdose, criticalOverdose) = RollOverdose(tier);
        var physicalDesc = _random.Pick(PhysicalDescriptions);

        var propertyHintId = _random.Pick(properties).Property.ID;

        var pools = BuildClassPools();
        var ingredientPool = RollContractHintPool(pools, tier);
        if (ingredientPool.Count == 0)
            ingredientPool = pools.Where(p => p.Key != ChemClass.None).SelectMany(p => p.Value).Distinct().ToList();
        var ingredientHintId = ingredientPool.Count > 0 ? _random.Pick(ingredientPool) : string.Empty;

        // cm: credit_reward = 3/5/7 for easy/medium/hard contracts (reroll_chemicals())
        var creditReward = tier switch
        {
            1 => 3,
            2 => 5,
            _ => 7,
        };

        return new ContractStats(name, color, physicalDesc, properties, overdose, criticalOverdose, propertyHintId, ingredientHintId, creditReward);
    }

    public (ProtoId<ReagentPrototype> Id, List<(string Id, int Amount, bool Catalyst)> Ingredients) FinalizeContract(ContractStats stats, int tier)
    {
        tier = Math.Clamp(tier, 1, 3);
        var id = $"RMCGenerated{++_generatedCount}{stats.Name.Replace(" ", "")}";

        var forcedFirst = string.IsNullOrEmpty(stats.IngredientHintId) ? null : stats.IngredientHintId;
        var ingredients = PickRecipeIngredients(tier, id, forcedFirst);
        var indicator = RollReactionIndicator();

        BroadcastGenerated(id, stats.Name, stats.Color, stats.PhysicalDesc, stats.Properties, stats.Overdose, stats.CriticalOverdose, ingredients, ChemClass.Special, indicator, stats.CreditReward);
        AppendToClassPoolCache(ChemClass.Special, id);
        CacheRecipeReactants(id, ingredients);
        _generatedTiers[id] = tier;
        _reactionIndicators[id] = indicator;

        return (id, ingredients);
    }

    private ReactionIndicator RollReactionIndicator()
    {
        // Weighted like cms: Bubbling 1, Fire 2, Endothermic 1, Glowing 1, Smoking 2, Calm 1.
        return _random.Next(0, 8) switch
        {
            0 => ReactionIndicator.Bubbling,
            1 or 2 => ReactionIndicator.Fire,
            3 => ReactionIndicator.Endothermic,
            4 => ReactionIndicator.Glowing,
            5 or 6 => ReactionIndicator.Smoking,
            _ => ReactionIndicator.Calm,
        };
    }

    private HashSet<string>? _generatedNameCache;

    private string GenerateName()
    {
        _generatedNameCache ??= _prototype.EnumeratePrototypes<ReagentPrototype>().Select(r => r.LocalizedName).ToHashSet();

        string name;
        do
        {
            name = _random.Pick(Prefix) + _random.Pick(WordRoot) + _random.Pick(Suffix);
        } while (_generatedNameCache.Contains(name));

        _generatedNameCache.Add(name);
        return name;
    }

    private (int Overdose, int CriticalOverdose) RollOverdose(int tier)
    {
        var overdoseMultiplier = tier switch
        {
            1 => _random.Next(1, 3),
            2 => _random.Next(4, 7),
            _ => _random.Next(6, 10),
        };

        var overdose = 5 + 5 * overdoseMultiplier;
        var criticalOverdose = overdose + 5;

        var attempts = _random.Next(1, 4);
        for (var i = 0; i < attempts; i++)
        {
            if (_random.Prob(0.20f + 0.02f * tier))
                criticalOverdose += 5;
        }

        return (overdose, criticalOverdose);
    }

    private List<(ChemGeneratorPropertyPrototype Property, int Level)> PickProperties(int tier)
    {
        var pool = _prototype.EnumeratePrototypes<ChemGeneratorPropertyPrototype>()
            .Where(p => !p.Flags.HasFlag(ChemPropertyType.Disabled))
            .ToList();

        var rarePool = pool.Where(p => p.Rarity == ChemPropertyRarity.Rare).ToList();
        var positive = pool.Where(p => p.Category == ChemPropertyCategory.Positive && p.Rarity != ChemPropertyRarity.Rare).ToList();
        var negative = pool.Where(p => p.Category == ChemPropertyCategory.Negative && p.Rarity != ChemPropertyRarity.Rare).ToList();
        var neutral = pool.Where(p => p.Category == ChemPropertyCategory.Neutral && p.Rarity != ChemPropertyRarity.Rare).ToList();

        var propertiesBuff = _random.Next(3, 5);
        if (tier == 2)
            propertiesBuff -= 2;
        var count = tier + propertiesBuff;

        var properties = new List<(ChemGeneratorPropertyPrototype, int)>();
        var genValue = 0;
        var forceNegative = false;

        for (var i = 1; i <= count; i++)
        {
            if (i == 1)
            {
                List<ChemGeneratorPropertyPrototype>? forcePool = null;
                var trackFirst = true;
                if (tier > 2 && rarePool.Count > 0)
                {
                    forcePool = rarePool;
                    trackFirst = false;
                }
                else if (tier > 1 && rarePool.Count > 0 && _random.Prob(0.20f))
                {
                    forcePool = rarePool;
                    forceNegative = true;
                }

                genValue = AddRandomProperty(properties, pool, forcePool, negative, neutral, positive, tier, 0, false, trackFirst);
                continue;
            }

            if (genValue == tier * 2 + 2)
                break;

            var valueOffset = tier - genValue - 1;
            genValue += AddRandomProperty(properties, pool, null, negative, neutral, positive, tier, valueOffset, forceNegative, tier < 3);
        }

        for (var attempt = 0; properties.Count < tier + 1 && attempt < 50; attempt++)
        {
            AddRandomProperty(properties, pool, null, negative, neutral, positive, tier, 0, false);
        }

        return properties;
    }

    private int AddRandomProperty(
        List<(ChemGeneratorPropertyPrototype Property, int Level)> properties,
        List<ChemGeneratorPropertyPrototype> pool,
        List<ChemGeneratorPropertyPrototype>? forcePool,
        List<ChemGeneratorPropertyPrototype> negative,
        List<ChemGeneratorPropertyPrototype> neutral,
        List<ChemGeneratorPropertyPrototype> positive,
        int tier,
        int valueOffset,
        bool forceNegative,
        bool trackAdded = false)
    {
        List<ChemGeneratorPropertyPrototype> from;
        if (forcePool is { Count: > 0 })
        {
            from = forcePool;
        }
        else if (forceNegative && negative.Count > 0)
        {
            from = negative;
        }
        else if (valueOffset > 0 && positive.Count > 0)
        {
            from = positive;
        }
        else if (valueOffset < 0)
        {
            from = _random.Next(0, 100) < tier * 10 && negative.Count > 0 ? negative : neutral;
        }
        else
        {
            var (negativeChance, neutralChance) = tier switch
            {
                1 => (0.40f, 0.10f),
                2 => (0.35f, 0.10f),
                _ => (0.15f, 0.10f),
            };
            var roll = _random.NextFloat();
            from = roll <= negativeChance ? negative : roll <= negativeChance + neutralChance ? neutral : positive;
        }

        if (from.Count == 0)
            from = pool;
        if (from.Count == 0)
            return 0;

        var property = _random.Pick(from);

        if (trackAdded)
        {
            var category = property.Category switch
            {
                ChemPropertyCategory.Negative => negative,
                ChemPropertyCategory.Neutral => neutral,
                _ => positive,
            };

            var checks = 0;
            while (!CheckGeneratedProperty(property, category) && checks < 4 && category.Count > 0)
            {
                checks++;
                property = _random.Pick(category);
            }
        }

        var level = Math.Clamp(RollLevel(), 1, Math.Min(tier + 3, property.MaxLevel));

        var newValue = property.Category switch
        {
            ChemPropertyCategory.Negative => -level,
            ChemPropertyCategory.Neutral => -(int) Math.Ceiling(level / 2.0),
            _ => level,
        };

        InsertProperty(properties, property, level);
        return newValue;
    }

    // rejects a repeat within its category until every property in
    // that category has been generated at least once this round
    private bool CheckGeneratedProperty(ChemGeneratorPropertyPrototype property, List<ChemGeneratorPropertyPrototype> categoryPool)
    {
        var generated = _generatedProperties[property.Category];
        if (generated.Contains(property.ID) && generated.Count < categoryPool.Count)
            return false;

        generated.Add(property.ID);
        return true;
    }

    private int RollLevel()
    {
        var roll = _random.Next(0, 101);
        return roll switch
        {
            <= 20 => 1,
            <= 40 => 2,
            <= 60 => 3,
            <= 75 => 4,
            <= 85 => 5,
            <= 90 => 6,
            <= 95 => 7,
            _ => 8,
        };
    }

    // Jank asf but idk what else to do
    private Dictionary<ChemClass, List<string>>? _classPoolCache;

    private Dictionary<ChemClass, List<string>> GetClassPools()
    {
        if (_classPoolCache != null)
            return _classPoolCache;

        var pools = new Dictionary<ChemClass, List<string>>();
        foreach (ChemClass c in Enum.GetValues<ChemClass>())
            pools[c] = new List<string>();

        foreach (var r in _prototype.EnumeratePrototypes<ReagentPrototype>())
        {
            if (r.Abstract || r.Unknown || r.NoGeneration)
                continue;

            pools[r.ChemClass].Add(r.ID);
        }

        _classPoolCache = pools;
        return pools;
    }

    private void AppendToClassPoolCache(ChemClass chemClass, string id)
    {
        _classPoolCache?[chemClass].Add(id);
    }

    private Dictionary<ChemClass, List<string>> BuildClassPools(params string[] exclude)
    {
        var cached = GetClassPools();
        if (exclude.Length == 0)
            return cached;

        var excludeSet = exclude.ToHashSet();
        var pools = new Dictionary<ChemClass, List<string>>();
        foreach (var (chemClass, ids) in cached)
            pools[chemClass] = ids.Where(id => !excludeSet.Contains(id)).ToList();

        return pools;
    }

    private List<string> RollIngredientPool(Dictionary<ChemClass, List<string>> pools, int tier)
    {
        var roll = _random.Next(0, 101);
        return tier switch
        {
            1 => roll <= 60 ? pools[ChemClass.Basic] : roll <= 80 ? pools[ChemClass.Common] : pools[ChemClass.Uncommon],
            2 => roll <= 50 ? pools[ChemClass.Common] : roll <= 75 ? pools[ChemClass.Uncommon] : pools[ChemClass.Rare],
            _ => roll <= 80 ? pools[_random.Prob(0.5f) ? ChemClass.Basic : ChemClass.Common] : pools[ChemClass.Hydro],
        };
    }

    // cm vial/random: uniform pick from the Special class (cm "C5").
    // 4% to get special RMCXenogeneticCatalyst
    private const float VialSpecialChance = 0.04f;
    private static readonly ProtoId<ReagentPrototype> XenogeneticCatalyst = "RMCXenogeneticCatalyst";

    public string RollRandomKnownReagent()
    {
        if (_random.Prob(VialSpecialChance))
            return XenogeneticCatalyst.Id;

        var special = GetClassPools()[ChemClass.Special]
            .Where(id => id != XenogeneticCatalyst.Id && !IsGenerated(id))
            .ToList();

        return special.Count > 0 ? _random.Pick(special) : string.Empty;
    }

    private List<string> RollContractHintPool(Dictionary<ChemClass, List<string>> pools, int tier)
    {
        var roll = _random.Next(0, 101);
        return tier switch
        {
            1 => roll <= 60 ? pools[ChemClass.Basic] : pools[ChemClass.Common],
            2 => roll <= 40 ? pools[ChemClass.Common] : pools[ChemClass.Uncommon],
            _ => pools[ChemClass.Hydro],
        };
    }

    private bool TryPickIngredient(Dictionary<ChemClass, List<string>> pools, List<string> allPool, int tier, HashSet<string> used, out string pick)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var pool = RollIngredientPool(pools, tier);
            if (pool.Count == 0)
                pool = allPool;
            if (pool.Count == 0)
                break;

            var candidate = _random.Pick(pool);
            if (!used.Add(candidate))
                continue;

            pick = candidate;
            return true;
        }

        pick = string.Empty;
        return false;
    }

    private List<(string Id, int Amount, bool Catalyst)> PickRecipeIngredients(int tier, string generatedId, string? forcedFirst = null)
    {
        List<(string Id, int Amount, bool Catalyst)> recipe = new();
        for (var attempt = 0; attempt < 10; attempt++)
        {
            recipe = RollRecipeIngredientsOnce(tier, generatedId, forcedFirst);
            if (recipe.Count == 0)
                continue;

            if (!IsAllMedical(recipe) && !DuplicatesExistingReaction(recipe))
                return recipe;
        }

        return recipe;
    }

    private bool IsAllMedical(List<(string Id, int Amount, bool Catalyst)> recipe)
    {
        var any = false;
        foreach (var (id, _, catalyst) in recipe)
        {
            if (catalyst)
                continue;

            any = true;
            if (!_rmcReagent.TryIndex(id, out var reagent) || !reagent.Medical)
                return false;
        }

        return any;
    }

    private bool DuplicatesExistingReaction(List<(string Id, int Amount, bool Catalyst)> recipe)
    {
        var reagents = recipe.Where(i => !i.Catalyst).Select(i => i.Id).ToHashSet();
        if (reagents.Count == 0)
            return false;

        foreach (var reaction in _prototype.EnumeratePrototypes<ReactionPrototype>())
        {
            if (reagents.All(r => reaction.Reactants.ContainsKey(r)))
                return true;
        }

        foreach (var (_, cached) in _recipeReactantsCache)
        {
            var existing = cached.Where(i => !i.Catalyst).Select(i => i.Id).ToHashSet();
            if (reagents.All(r => existing.Contains(r)))
                return true;
        }

        return false;
    }

    private List<(string Id, int Amount, bool Catalyst)> RollRecipeIngredientsOnce(int tier, string generatedId, string? forcedFirst = null)
    {
        var pools = BuildClassPools(generatedId);
        var allPool = pools.Where(p => p.Key != ChemClass.None).SelectMany(p => p.Value).Distinct().ToList();
        var used = new HashSet<string>();

        var desired = tier == 1 ? 3 : _random.Next(3, 5);

        var result = new List<(string, int, bool)>();
        for (var i = 0; i < desired; i++)
        {
            string pick;
            if (i == 0 && forcedFirst != null)
            {
                pick = forcedFirst;
                used.Add(pick);
            }
            else if (!TryPickIngredient(pools, allPool, tier, used, out pick))
            {
                break;
            }

            var amount = 1;
            if (i == 0)
            {
                amount = _random.Next(0, 101) switch
                {
                    <= 60 => 1,
                    <= 75 => 2,
                    <= 85 => 3,
                    <= 92 => 4,
                    <= 97 => 5,
                    _ => 6,
                };
            }

            result.Add((pick, amount, false));
        }

        // prob(20) && gen_tier >= 2 adds one extra catalyst component, always modifier 5
        if (tier >= 2 && _random.Prob(0.20f) && TryPickIngredient(pools, allPool, tier, used, out var catalystPick))
            result.Add((catalystPick, 5, true));

        return result;
    }

    private static readonly string[] PhysicalDescriptions =
    [
        "nondescript", "oily", "cloudy", "syrupy", "crystalline", "metallic", "pungent", "viscous", "gaseous",
        "shiny", "powdery",
    ];


    public List<(ChemGeneratorPropertyPrototype Property, int Level)> GetProperties(Reagent reagent)
    {
        var result = new List<(ChemGeneratorPropertyPrototype, int)>();
        var effects = _rmcReagent.GetProperties(reagent.ID);
        if (effects.Count == 0)
            return result;

        var pool = _prototype.EnumeratePrototypes<ChemGeneratorPropertyPrototype>().ToList();
        foreach (var effect in effects)
        {
            var property = pool.FirstOrDefault(p => p.Effect.GetType() == effect.GetType());
            if (property == null)
                continue;

            result.Add((property, (int) effect.Potency));
        }

        return result;
    }

    public EntityUid PrintReport(EntityCoordinates coords, ProtoId<ReagentPrototype> reagentId, Reagent reagent, int sampleNumber = 0, string? category = null, bool simulatorReport = false)
    {
        var report = Spawn(ReportProto, coords);
        WriteReport(report, reagentId, reagent, sampleNumber, simulatorReport);

        if (category != null)
        {
            var title = sampleNumber > 0 ? $"{sampleNumber} - {Name(report)}" : Name(report);
            _research.SaveDocument(category, report, title);
        }

        return report;
    }

    public void RefreshReport(EntityUid report)
    {
        if (!TryComp(report, out RMCChemResearchReportComponent? comp) ||
            comp.ReagentId is not { } reagentId ||
            !_rmcReagent.TryIndex(reagentId, out var reagent))
        {
            return;
        }

        WriteReport(report, reagentId, reagent, comp.SampleNumber, comp.SimulatorReport);
    }

    private void WriteReport(EntityUid report, ProtoId<ReagentPrototype> reagentId, Reagent reagent, int sampleNumber, bool simulatorReport)
    {
        var isGenerated = IsGenerated(reagentId);
        var ultra = isGenerated && reagent.ChemClass >= ChemClass.Special || reagent.ChemClass == ChemClass.Hydro;

        var hasTier = TryGetGeneratedTier(reagentId, out var tier);
        var tierClassified = ultra && hasTier && !_research.HasSufficientClearance(tier);

        var xClassified = !isGenerated && reagent.ChemClass == ChemClass.Special && !_research.HasXAccess();

        var classified = tierClassified || xClassified;

        var allProperties = GetProperties(reagent);
        var properties = classified ? new List<(ChemGeneratorPropertyPrototype Property, int Level)>() : allProperties;

        var comp = EnsureComp<RMCChemResearchReportComponent>(report);
        comp.ReagentId = reagentId;
        comp.Properties = properties.Select(p => new ChemReportProperty { PropertyId = p.Property.ID, Level = p.Level }).ToList();
        comp.Overdose = (int) (reagent.Overdose ?? FixedPoint2.Zero);
        comp.CriticalOverdose = (int) (reagent.CriticalOverdose ?? FixedPoint2.Zero);
        comp.IsGenerated = isGenerated;
        comp.SimulatorReport = simulatorReport;
        comp.SampleNumber = sampleNumber;

        if (reagent.ChemClass >= ChemClass.Special && !_research.IsIdentified(reagentId))
            comp.SavedEmissionSpectrum = true;

        var sb = new StringBuilder();
        if (simulatorReport)
        {
            var simulatedName = $"{reagent.LocalizedName}{GetPropertyCodes(allProperties)}";
            _metaData.SetEntityName(report, $"Simulation result for {simulatedName}");
            RMCChemPaperFormat.AppendHeader(sb, "Official Company Document", "Simulated Synthesis Report");
            sb.AppendLine();
            sb.AppendLine($"[bold][head=3]Result for {simulatedName}[/head][/bold]");
        }
        else
        {
            var codes = ultra && !classified ? GetPropertyCodes(properties) : string.Empty;
            _metaData.SetEntityName(report, $"Analysis of {reagent.LocalizedName}{codes}");
            RMCChemPaperFormat.AppendHeader(sb, "Official Weston-Yamada Document", "Automated A-XRF Report");
            sb.AppendLine();
            sb.AppendLine($"[bold][head=3]Analysis of {reagent.LocalizedName}[/head][/bold]");
            if (sampleNumber > 0)
                sb.AppendLine($"[bold]Results for sample:[/bold] #{sampleNumber}");
        }

        sb.AppendLine();
        sb.AppendLine($"[bold]ID:[/bold] [italic]{reagent.LocalizedName}[/italic]");
        sb.AppendLine();
        sb.AppendLine("[bold]Database Details:[/bold]");

        if (tierClassified)
        {
            sb.AppendLine($"CLASSIFIED: [italic]Clearance level {tier} required to read the database entry.[/italic]");
        }
        else if (xClassified)
        {
            sb.AppendLine("CLASSIFIED: [italic]Clearance level [bold]X[/bold] required to read the database entry.[/italic]");
        }
        else
        {
            if (ultra)
                sb.AppendLine($"[italic]The following information relating to {reagent.LocalizedName} is restricted with a level {(hasTier ? tier : 0)} clearance classification.[/italic]");

            sb.AppendLine();
            sb.AppendLine("Chemical has following reaction indicators:");
            var indicator = _reactionIndicators.TryGetValue(reagentId.Id, out var rollAndIndicator) ? rollAndIndicator : ReactionIndicator.Calm;
            sb.AppendLine(GetIndicatorText(indicator));

            sb.AppendLine();
            if (isGenerated)
            {
                var entries = properties.Select(p => $"[bold]{p.Property.ID} Level {p.Level}[/bold] - {p.Property.Description}");
                sb.AppendLine($"[font size={RMCChemPaperFormat.PropertiesFontSize}]{string.Join("\n\n", entries)}[/font]");
            }
            else
            {
                sb.AppendLine($"[font size={RMCChemPaperFormat.PropertiesFontSize}]{reagent.LocalizedDescription}[/font]");
            }

            sb.AppendLine();
            sb.AppendLine($"Overdoses at: {comp.Overdose} units");
            sb.AppendLine($"Critically Overdoses at: {comp.CriticalOverdose} units");
            sb.AppendLine(ultra
                ? $"Standard duration multiplier of {GetDurationMultiplier(reagent)}x"
                : $"Standard duration multiplier: {GetDurationMultiplier(reagent)}x");
        }

        if (comp.SavedEmissionSpectrum)
        {
            sb.AppendLine();
            sb.AppendLine($"[italic]Saved emission spectrum of {reagent.LocalizedName} to the database.[/italic]");
        }

        sb.AppendLine();
        var compositionKnown = AppendComposition(sb, reagentId, reagent);

        comp.Completed = !classified && (compositionKnown || (!isGenerated && reagent.ChemClass == ChemClass.Special));
        Dirty(report, comp);

        sb.AppendLine();
        RMCChemPaperFormat.AppendFooter(sb, simulatorReport
            ? "This report was automatically printed by the Synthesis Simulator."
            : "This report was automatically printed by the A-XRF Scanner.");

        _paper.SetContent(report, RMCChemPaperFormat.Wrap(sb));
    }

    private static string GetPropertyCodes(List<(ChemGeneratorPropertyPrototype Property, int Level)> properties)
    {
        return string.Concat(properties.Select(p => $" {(string.IsNullOrEmpty(p.Property.Code) ? p.Property.ID : p.Property.Code)}{p.Level}"));
    }

    private static string GetIndicatorText(ReactionIndicator indicator)
    {
        return indicator switch
        {
            ReactionIndicator.Bubbling => "[bold]Aggressive foaming[/bold]\n- The reaction causes bubbling and foam to build up rapidly and shoot out of the beaker. Biological Suit gives complete protection.",
            ReactionIndicator.Glowing => "[bold]Luminescence[/bold]\n- The reaction produces light, power of the light is dictated by the amount mixed.",
            ReactionIndicator.Fire => "[bold]Exothermic[/bold]\n- The reaction produces heat and will cause a small scale combustion. This will not compromise the contents of the beaker.",
            ReactionIndicator.Smoking => "[bold]Fuming[/bold]\n- The reaction produces heavy fumes from contents of the beaker. Work under a fume hood, wear a gas mask, or simply put an airtight seal over the beaker.",
            ReactionIndicator.Endothermic => "[bold]Endothermic[/bold]\n- The reaction is endothermic. This slows down the mixing process significantly and stops all other reactions from happening.",
            _ => "[bold]Inert[/bold]\n- The reaction has no indicators.",
        };
    }

    private static string GetDurationMultiplier(ReagentPrototype reagent)
    {
        const float standardRate = 0.1f;

        if (reagent.Metabolisms is not { } metabolisms ||
            !metabolisms.TryGetValue(RMCReagentSystem.GeneratedMetabolismGroup, out var entry) ||
            entry.MetabolismRate <= FixedPoint2.Zero)
        {
            return "1";
        }

        var multiplier = standardRate / (float) entry.MetabolismRate;
        return multiplier.ToString("0.##");
    }

    private bool AppendComposition(StringBuilder sb, ProtoId<ReagentPrototype> reagentId, ReagentPrototype reagent)
    {
        sb.AppendLine("[bold]Composition Details:[/bold]");

        var reaction = _prototype.EnumeratePrototypes<ReactionPrototype>().FirstOrDefault(r => r.Products.ContainsKey(reagentId.Id));
        if (reaction != null)
        {
            var known = AppendReactants(sb, reaction.Reactants.Where(kv => !kv.Value.Catalyst), isCatalyst: false);

            var catalysts = reaction.Reactants.Where(kv => kv.Value.Catalyst).ToList();
            if (catalysts.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Reaction would require the following catalysts:");
                known &= AppendReactants(sb, catalysts, isCatalyst: true);
            }

            return known;
        }

        if (reagent.ChemClass == ChemClass.Basic)
        {
            sb.AppendLine($"- {reagent.LocalizedName}");
            return true;
        }

        sb.AppendLine("[italic]ERROR: Unable to analyze emission spectrum of sample.[/italic]");
        return false;
    }

    private bool AppendReactants(StringBuilder sb, IEnumerable<KeyValuePair<string, ReactantPrototype>> reactants, bool isCatalyst)
    {
        var known = true;
        foreach (var (ingredientId, reactant) in reactants)
        {
            if (TryGetChemClass(ingredientId, out var ingredientClass) &&
                ingredientClass >= ChemClass.Special &&
                (isCatalyst || ingredientClass != ChemClass.Hydro) &&
                !_research.IsIdentified(ingredientId))
            {
                sb.AppendLine("[italic]- Unknown emission spectrum[/italic]");
                known = false;
                continue;
            }

            var name = _rmcReagent.TryIndex(ingredientId, out var ingredientReagent) ? ingredientReagent.LocalizedName : ingredientId;
            sb.AppendLine($"- {reactant.Amount} {name}");
        }

        return known;
    }

    public EntityUid PrintErrorReport(EntityCoordinates coords, string reason, int sampleNumber = 0)
    {
        var report = Spawn(ReportProto, coords);
        var comp = EnsureComp<RMCChemResearchReportComponent>(report);
        comp.Completed = false;
        Dirty(report, comp);

        _metaData.SetEntityName(report, "research report (ERROR)");

        var sb = new StringBuilder();
        RMCChemPaperFormat.AppendHeader(sb, "Official Weston-Yamada Document", "Reagent Analysis Print");
        sb.AppendLine();
        sb.AppendLine("[bold][head=3]Analysis ERROR[/head][/bold]");
        sb.AppendLine();
        sb.AppendLine(sampleNumber > 0
            ? $"Result: Analysis failed for sample #{sampleNumber}."
            : "Result: Analysis failed.");
        sb.AppendLine();
        sb.AppendLine("Reason for error:");
        sb.AppendLine($"[italic]{reason}[/italic]");
        sb.AppendLine();
        RMCChemPaperFormat.AppendFooter(sb, "This report was automatically printed by the A-XRF Scanner.");

        _paper.SetContent(report, RMCChemPaperFormat.Wrap(sb));

        return report;
    }

    public void InsertProperty(List<(ChemGeneratorPropertyPrototype Property, int Level)> properties, ChemGeneratorPropertyPrototype property, int level)
    {
        var pool = _prototype.EnumeratePrototypes<ChemGeneratorPropertyPrototype>().ToDictionary(p => p.ID);
        var propertyId = property.ID;
        string? initialPropertyId = null;

        foreach (var (existing, existingLevel) in properties.ToArray())
        {
            ChemGeneratorPropertyPrototype? match = null;

            if (existing.ID == propertyId)
            {
                match = existing;
            }
            else
            {
                foreach (var (comboId, pieces) in ChemPropertyRelations.Combines.Concat(_legendaryCombines))
                {
                    if (!pieces.Contains(propertyId) || !pieces.Contains(existing.ID))
                        continue;

                    var satisfied = pieces.Count(p => p == propertyId || properties.Any(x => x.Property.ID == p));
                    if (satisfied < pieces.Count)
                        continue;

                    initialPropertyId = propertyId;
                    propertyId = comboId;
                    level = Math.Max(Math.Max(level - existingLevel, existingLevel - level), 1);

                    foreach (var (r, rLevel) in properties.ToArray())
                    {
                        if (!pieces.Contains(r.ID) || r.Flags.HasFlag(ChemPropertyType.Catalyst))
                            continue;

                        var newLevel = rLevel - level;
                        var idx = properties.FindIndex(p => p.Property.ID == r.ID);
                        if (newLevel <= 0)
                            properties.RemoveAt(idx);
                        else
                            properties[idx] = (r, newLevel);
                    }

                    break;
                }

                if (match == null)
                {
                    foreach (var (a, b) in ChemPropertyRelations.Conflicts)
                    {
                        if ((propertyId == a && existing.ID == b) || (propertyId == b && existing.ID == a))
                        {
                            match = existing;
                            break;
                        }
                    }
                }
            }

            if (match == null)
                continue;

            var matchIdx = properties.FindIndex(p => p.Property.ID == match.ID);
            if (matchIdx == -1)
                continue;

            var matchLevel = properties[matchIdx].Level;
            if (matchLevel > level)
            {
                properties[matchIdx] = (match, matchLevel - level);
                return;
            }

            if (matchLevel < level)
            {
                level -= matchLevel;
                properties.RemoveAt(matchIdx);
            }
            else
            {
                properties.RemoveAt(matchIdx);
                return;
            }

            break;
        }

        if (!pool.TryGetValue(propertyId, out var finalProperty))
            return;

        level = Math.Min(level, finalProperty.MaxLevel);
        properties.Add((finalProperty, level));

        if (initialPropertyId != null &&
            initialPropertyId != propertyId &&
            pool.TryGetValue(initialPropertyId, out var initialProperty) &&
            initialProperty.Flags.HasFlag(ChemPropertyType.Catalyst))
        {
            properties.Add((initialProperty, level));
        }
    }

    private readonly Dictionary<string, List<(string Id, int Amount, bool Catalyst)>> _recipeReactantsCache = new();

    private void CacheRecipeReactants(string reagentId, List<(string Id, int Amount, bool Catalyst)> ingredients)
    {
        if (ingredients.Count > 0)
            _recipeReactantsCache[reagentId] = ingredients;
    }

    public bool HasExistingRecipe(string reagentId) => TryGetRecipeReactants(reagentId, out _);

    public bool TryGetRecipeReactants(string reagentId, out List<(string Id, int Amount, bool Catalyst)> reactants)
    {
        if (_recipeReactantsCache.TryGetValue(reagentId, out var cached))
        {
            reactants = cached;
            return true;
        }

        var existing = _prototype.EnumeratePrototypes<ReactionPrototype>()
            .FirstOrDefault(r => r.Products.ContainsKey(reagentId));

        if (existing == null || existing.Reactants.Count == 0)
        {
            reactants = new();
            return false;
        }

        reactants = existing.Reactants.Select(kv => (Id: kv.Key, Amount: (int) kv.Value.Amount, kv.Value.Catalyst)).ToList();
        return true;
    }

    public bool IsDuplicateRecipe(List<(string Id, int Amount, bool Catalyst)> a, List<(string Id, int Amount, bool Catalyst)> b)
    {
        if (a.Count != b.Count)
            return false;

        var aKeys = a.Select(x => $"{x.Id}:{x.Amount}:{x.Catalyst}").OrderBy(x => x, StringComparer.Ordinal);
        var bKeys = b.Select(x => $"{x.Id}:{x.Amount}:{x.Catalyst}").OrderBy(x => x, StringComparer.Ordinal);
        return aKeys.SequenceEqual(bKeys);
    }

    public string GetRootId(string reagentId)
    {
        return _originalIds.TryGetValue(reagentId, out var originalId) ? originalId : reagentId;
    }

    public string ComputeSimulationSignature(string rootId, List<(ChemGeneratorPropertyPrototype Property, int Level)> properties)
    {
        var sorted = properties
            .OrderBy(p => p.Property.ID, StringComparer.Ordinal)
            .Select(p => $"{p.Property.ID}:{p.Level}");
        return $"{rootId}#" + string.Join("|", sorted);
    }

    public bool TryGetSimulationResult(string signature, out ProtoId<ReagentPrototype> reagentId) =>
        _simulationResults.TryGetValue(signature, out reagentId);

    public void RegisterSimulationResult(string signature, ProtoId<ReagentPrototype> reagentId) =>
        _simulationResults[signature] = reagentId;

    public bool TryRollRecipeCandidate(
        string baseReagentId,
        int tier,
        out List<(string Id, int Amount, bool Catalyst)> ingredients,
        out string? newIngredient)
    {
        newIngredient = null;
        if (!TryGetRecipeReactants(baseReagentId, out var existing))
        {
            ingredients = PickRecipeIngredients(tier, baseReagentId);
            return ingredients.Count > 0;
        }

        ingredients = new List<(string Id, int Amount, bool Catalyst)>(existing);

        var pools = BuildClassPools([baseReagentId, ..existing.Select(x => x.Id)]);
        var allPool = pools.Where(p => p.Key != ChemClass.None).SelectMany(p => p.Value).Distinct().ToList();
        if (!TryPickIngredient(pools, allPool, Math.Max(tier - 1, 1), new HashSet<string>(), out var picked) ||
            !TryGetChemClass(picked, out var pickedClass) ||
            pickedClass >= ChemClass.Special)
        {
            return false;
        }

        var required = ingredients.Where(i => !i.Catalyst).ToList();
        if (required.Count > 2)
            ingredients.Remove(_random.Pick(required));

        var amount = _random.Next(0, 65) switch
        {
            < 30 => 1,
            < 45 => 2,
            < 60 => 3,
            _ => 4,
        };
        ingredients.Add((picked, amount, false));

        if (IsAllMedical(ingredients) || DuplicatesExistingReaction(ingredients))
            return false;

        newIngredient = picked;
        return true;
    }

    public bool TryGetChemClass(string reagentId, out ChemClass chemClass)
    {
        if (_rmcReagent.TryIndex(reagentId, out var reagent))
        {
            chemClass = reagent.ChemClass;
            return true;
        }

        chemClass = ChemClass.None;
        return false;
    }

    public ProtoId<ReagentPrototype> GenerateVariant(
        Reagent baseReagent,
        List<(ChemGeneratorPropertyPrototype Property, int Level)> newProperties,
        int tier,
        List<(string Id, int Amount, bool Catalyst)> ingredients,
        int newOverdose)
    {
        var suffix = string.Concat(newProperties.Select(p => $" {(string.IsNullOrEmpty(p.Property.Code) ? p.Property.ID : p.Property.Code)}{p.Level}"));
        var hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(suffix)))[..2];

        var rootId = GetRootId(baseReagent.ID);
        var rootName = _rmcReagent.TryIndex(rootId, out var rootReagent) ? rootReagent.LocalizedName : baseReagent.LocalizedName;
        var name = $"{rootName} {hash}";
        var id = $"{rootId}Variant{++_generatedCount}{hash}";
        var color = baseReagent.SubstanceColor.ToHexNoAlpha();

        var criticalOverdose = Math.Max(Math.Min(newOverdose * 2, newOverdose + 30), 10);
        var physicalDesc = _random.Pick(PhysicalDescriptions);
        var indicator = RollReactionIndicator();

        BroadcastGenerated(id, name, color, physicalDesc, newProperties, newOverdose, criticalOverdose, ingredients, ChemClass.Rare, indicator);
        AppendToClassPoolCache(ChemClass.Rare, id);
        CacheRecipeReactants(id, ingredients);
        _generatedTiers[id] = tier;
        _originalIds[id] = rootId;
        _reactionIndicators[id] = indicator;

        return id;
    }
}
