using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Chemistry.Effects;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using ReactiveReagentEffectEntry = Content.Shared.Chemistry.Reagent.ReactiveReagentEffectEntry;

namespace Content.Shared._RMC14.Chemistry.Reagent;

public sealed class RMCReagentSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    public static readonly ProtoId<MetabolismGroupPrototype> GeneratedMetabolismGroup = "Poison";

    private static readonly ProtoId<ReactiveGroupPrototype> GeneratedReactiveGroup = "RMCGenerated";

    private FrozenDictionary<string, Reagent> _reagents = FrozenDictionary<string, Reagent>.Empty;
    private FrozenDictionary<string, RMCChemicalEffect[]> _properties = FrozenDictionary<string, RMCChemicalEffect[]>.Empty;
    private readonly Dictionary<string, RMCGeneratedReagentData> _generated = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<MobStateComponent, ReactionEntityEvent>(OnMobReaction);
        ReloadAllPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<ReagentPrototype>())
            return;

        var dict = new Dictionary<string, Reagent>(_reagents);

        if (ev.Removed != null && ev.Removed.TryGetValue(typeof(ReagentPrototype), out var removed))
        {
            foreach (var id in removed)
                dict.Remove(id);
        }

        if (ev.ByType.TryGetValue(typeof(ReagentPrototype), out var changeSet))
        {
            foreach (var (id, prototype) in changeSet.Modified)
            {
                if (prototype is not ReagentPrototype reagentProto)
                    continue;

                object? reagentObj = new Reagent();
                _serialization.CopyTo(reagentProto, ref reagentObj);
                if (reagentObj is Reagent reagent)
                    dict[id] = reagent;
            }
        }

        ApplyGenerated(dict);
        SetReagents(dict);
    }

    private void ReloadAllPrototypes()
    {
        var dict = new Dictionary<string, Reagent>();
        foreach (var reagentProto in _prototypes.EnumeratePrototypes<ReagentPrototype>())
        {
            object? reagentObj = new Reagent();
            _serialization.CopyTo(reagentProto, ref reagentObj);
            if (reagentObj is not Reagent reagent)
                continue;

            dict[reagentProto.ID] = reagent;
        }

        ApplyGenerated(dict);
        SetReagents(dict);
    }

    private void SetReagents(Dictionary<string, Reagent> dict)
    {
        _reagents = dict.ToFrozenDictionary();
        _properties = dict.ToFrozenDictionary(pair => pair.Key, pair => BuildProperties(pair.Value));
    }

    private static RMCChemicalEffect[] BuildProperties(Reagent reagent)
    {
        var properties = new List<RMCChemicalEffect>(reagent.FireProperties);
        if (reagent.Metabolisms == null)
            return properties.ToArray();

        foreach (var entry in reagent.Metabolisms.Values)
        {
            foreach (var effect in entry.Effects)
            {
                if (effect is RMCChemicalEffect property)
                    properties.Add(property);
            }
        }

        return properties.ToArray();
    }

    private void OnMobReaction(Entity<MobStateComponent> ent, ref ReactionEntityEvent args)
    {
        if (args.Reagent.FireProperties.Count == 0)
            return;

        var quantity = args.Source?.GetReagentQuantity(args.ReagentQuantity.Reagent) ?? args.ReagentQuantity.Quantity;
        var effectArgs = new EntityEffectReagentArgs(ent, EntityManager, null, args.Source, quantity, args.Reagent, args.Method, 1f);
        foreach (var property in args.Reagent.FireProperties)
        {
            if (property.ReactsOnTouch)
                property.Effect(effectArgs);
        }
    }

    private void ApplyGenerated(Dictionary<string, Reagent> dict)
    {
        foreach (var data in _generated.Values)
        {
            if (BuildGenerated(data) is { } reagent)
                dict[data.Id] = reagent;
        }
    }

    public void RegisterGenerated(RMCGeneratedReagentData data)
    {
        _generated[data.Id] = data;

        if (BuildGenerated(data) is not { } reagent)
            return;

        var dict = new Dictionary<string, Reagent>(_reagents) { [data.Id] = reagent };
        _reagents = dict.ToFrozenDictionary();

        var properties = new Dictionary<string, RMCChemicalEffect[]>(_properties) { [data.Id] = BuildProperties(reagent) };
        _properties = properties.ToFrozenDictionary();
    }

    public void ClearGenerated()
    {
        if (_generated.Count == 0)
            return;

        var dict = new Dictionary<string, Reagent>(_reagents);
        foreach (var id in _generated.Keys)
        {
            dict.Remove(id);
        }

        _generated.Clear();
        SetReagents(dict);
    }

    private Reagent? BuildGenerated(RMCGeneratedReagentData data)
    {
        var desc = string.IsNullOrEmpty(data.PhysicalDescription) ? data.Name : data.PhysicalDescription;
        var node = new MappingDataNode();
        node.Add("id", data.Id);
        node.Add("name", data.Name);
        node.Add("desc", desc);
        node.Add("physicalDesc", desc);
        if (!string.IsNullOrEmpty(data.Color))
            node.Add("color", data.Color);
        node.Add("group", "Generated");
        node.Add("fireEntity", "RMCTileFireGenerated");

        var tileReactions = new SequenceDataNode();
        if (data.Properties.Any(p => p.PropertyId == "Neutralizing"))
            tileReactions.Add(new MappingDataNode { Tag = "!type:RMCNeutralizeTileReaction" });

        if (data.Properties.Any(p => p.PropertyId == "Fueling"))
            tileReactions.Add(new MappingDataNode { Tag = "!type:SpillTileReaction" });

        if (tileReactions.Count > 0)
            node.Add("tileReactions", tileReactions);

        Reagent reagent;
        try
        {
            reagent = _serialization.Read<Reagent>(node, notNullableOverride: true);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to build generated reagent {data.Id}: {e}");
            return null;
        }

        reagent.ChemClass = data.ChemClass;
        reagent.CreditReward = data.CreditReward;
        reagent.Overdose = data.Overdose > 0 ? FixedPoint2.New(data.Overdose) : null;
        reagent.CriticalOverdose = data.CriticalOverdose > 0 ? FixedPoint2.New(data.CriticalOverdose) : null;
        reagent.Unknown = true;
        reagent.Flavor = PickGeneratedFlavor(data);

        var effects = new List<EntityEffect>();
        var reactionEffects = new List<EntityEffect>();

        var hyperLevel = 0f;
        var hypoLevel = 0f;

        foreach (var prop in data.Properties)
        {
            if (!_prototypes.TryIndex<ChemGeneratorPropertyPrototype>(prop.PropertyId, out var propProto))
                continue;

            var effect = _serialization.CreateCopy(propProto.Effect, notNullableOverride: true);
            if (effect is RMCChemicalEffect rmc)
                rmc.Potency = prop.Level;

            effects.Add(effect);
            if (effect is RMCChemicalEffect { ReactsOnTouch: true })
                reactionEffects.Add(effect);

            switch (prop.PropertyId)
            {
                case "Hypermetabolic":
                    hyperLevel = prop.Level;
                    break;
                case "Hypometabolic":
                    hypoLevel = prop.Level;
                    break;
            }
        }

        if (data.Properties.Any(p => p.PropertyId is "Defibrillating" or "Neurocryogenic" or "Thanatometabolizing"))
            reagent.WorksOnTheDead = true;

        if (effects.Count > 0)
        {
            var rate = 0.1f;
            if (hyperLevel > 0f)
                rate *= 1f + 0.25f * hyperLevel;
            if (hypoLevel > 0f)
                rate = MathF.Max(rate / (1f + 0.35f * hypoLevel), 0.005f);

            var entry = new ReagentEffectsEntry { Effects = effects.ToArray(), MetabolismRate = FixedPoint2.New(rate) };
            reagent.Metabolisms = new Dictionary<ProtoId<MetabolismGroupPrototype>, ReagentEffectsEntry>
            {
                [GeneratedMetabolismGroup] = entry,
            }.ToFrozenDictionary();
        }

        if (reactionEffects.Count > 0)
        {
            reagent.ReactiveEffects = new Dictionary<ProtoId<ReactiveGroupPrototype>, ReactiveReagentEffectEntry>
            {
                [GeneratedReactiveGroup] = new ReactiveReagentEffectEntry
                {
                    Methods = new HashSet<ReactionMethod> { ReactionMethod.Touch, ReactionMethod.Injection },
                    Effects = reactionEffects.ToArray(),
                },
            };
        }

        return reagent;
    }

    private ProtoId<FlavorPrototype>? PickGeneratedFlavor(RMCGeneratedReagentData data)
    {
        RecipeCandidateIngredient? dominant = null;
        foreach (var ingredient in data.Ingredients)
        {
            if (ingredient.Catalyst)
                continue;

            if (dominant == null || ingredient.Amount > dominant.Amount)
                dominant = ingredient;
        }

        if (dominant != null && TryIndex(dominant.Id, out var ingredientReagent))
            return ingredientReagent.Flavor;

        return null;
    }

    public Reagent Index(ProtoId<ReagentPrototype> id)
    {
        return _reagents[id];
    }

    public bool TryIndex(ProtoId<ReagentPrototype> id, [NotNullWhen(true)] out Reagent? reagent)
    {
        return _reagents.TryGetValue(id, out reagent);
    }

    public bool TryIndex(ReagentId id, [NotNullWhen(true)] out Reagent? reagent)
    {
        return _reagents.TryGetValue(id.Prototype, out reagent);
    }

    public bool IsGenerated(ProtoId<ReagentPrototype> id)
    {
        return _generated.ContainsKey(id);
    }

    public IReadOnlyList<RMCChemicalEffect> GetProperties(ProtoId<ReagentPrototype> id)
    {
        return _properties.TryGetValue(id, out var properties) ? properties : [];
    }

    public bool TryGetProperty<T>(ProtoId<ReagentPrototype> id, [NotNullWhen(true)] out T? property) where T : RMCChemicalEffect
    {
        foreach (var candidate in GetProperties(id))
        {
            if (candidate is not T found)
                continue;

            property = found;
            return true;
        }

        property = null;
        return false;
    }

    public bool HasProperty<T>(ProtoId<ReagentPrototype> id) where T : RMCChemicalEffect
    {
        return TryGetProperty<T>(id, out _);
    }
}
