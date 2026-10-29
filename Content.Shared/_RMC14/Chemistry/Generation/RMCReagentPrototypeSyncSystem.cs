using System.Linq;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.GameTicking;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Generation;

public sealed class RMCReagentPrototypeSyncSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly RMCReagentSystem _reagent = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly Dictionary<string, RMCGeneratedReagentData> _generated = new();
    private readonly List<RMCGeneratedReagentData> _roundGenerated = new();
    private readonly List<RMCGeneratedReagentData> _batch = new();
    private int _batchDepth;

    public IReadOnlyDictionary<string, RMCGeneratedReagentData> Generated => _generated;

    public override void Initialize()
    {
        if (_net.IsClient)
        {
            SubscribeNetworkEvent<RMCGeneratedReagentSyncEvent>(OnSync);
            SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
            return;
        }

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_net.IsServer)
            _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public void Broadcast(RMCGeneratedReagentData data)
    {
        Register(data);
        _roundGenerated.Add(data);

        if (_batchDepth > 0)
        {
            _batch.Add(data);
            return;
        }

        Send(new List<RMCGeneratedReagentData> { data });
    }

    public void BeginBatch()
    {
        _batchDepth++;
    }

    public void EndBatch()
    {
        if (_batchDepth == 0 || --_batchDepth > 0 || _batch.Count == 0)
            return;

        var batch = new List<RMCGeneratedReagentData>(_batch);
        _batch.Clear();
        Send(batch);
    }

    public bool TryGet(string id, out RMCGeneratedReagentData data)
    {
        return _generated.TryGetValue(id, out data!);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _roundGenerated.Clear();
        _batch.Clear();

        if (_generated.Count == 0)
            return;

        var generated = new List<RMCGeneratedReagentData>(_generated.Values);
        _generated.Clear();
        _reagent.ClearGenerated();
        Unload(generated);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.InGame || _roundGenerated.Count == 0)
            return;

        var ev = new RMCGeneratedReagentSyncEvent(new List<RMCGeneratedReagentData>(_roundGenerated));
        RaiseNetworkEvent(ev, args.Session.Channel);
    }

    private void OnSync(RMCGeneratedReagentSyncEvent ev)
    {
        foreach (var data in ev.Data)
        {
            Register(data);
        }

        Inject(ev.Data);
    }

    private void Send(List<RMCGeneratedReagentData> batch)
    {
        Inject(batch);
        RaiseNetworkEvent(new RMCGeneratedReagentSyncEvent(batch));
    }

    private void Register(RMCGeneratedReagentData data)
    {
        _generated[data.Id] = data;
        _reagent.RegisterGenerated(data);
    }

    private void Inject(List<RMCGeneratedReagentData> batch)
    {
        var yaml = string.Join("\n", batch.Select(BuildYaml));
        try
        {
            var changed = new Dictionary<Type, HashSet<string>>();
            _prototypes.LoadString(yaml, overwrite: true, changed);
            _prototypes.ReloadPrototypes(changed);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to inject generated reagent prototypes for {string.Join(", ", batch.Select(d => d.Id))}: {e}");
        }
    }

    private void Unload(List<RMCGeneratedReagentData> generated)
    {
        var reagents = new HashSet<string>();
        var reactions = new HashSet<string>();
        var lines = new List<string>();
        foreach (var data in generated)
        {
            if (_prototypes.HasIndex<ReagentPrototype>(data.Id))
            {
                reagents.Add(data.Id);
                lines.Add("- type: reagent");
                lines.Add("  id: " + data.Id);
            }

            var reactionId = RecipeId(data.Id);
            if (_prototypes.HasIndex<ReactionPrototype>(reactionId))
            {
                reactions.Add(reactionId);
                lines.Add("- type: reaction");
                lines.Add("  id: " + reactionId);
            }
        }

        if (lines.Count == 0)
            return;

        var removed = new Dictionary<Type, HashSet<string>>();
        if (reagents.Count > 0)
            removed[typeof(ReagentPrototype)] = reagents;

        if (reactions.Count > 0)
            removed[typeof(ReactionPrototype)] = reactions;

        try
        {
            _prototypes.RemoveString(string.Join("\n", lines));
            _prototypes.ReloadPrototypes(new Dictionary<Type, HashSet<string>>(), removed);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to unload generated reagent prototypes: {e}");
        }
    }

    private static string RecipeId(string reagentId)
    {
        return reagentId + "Recipe";
    }

    private static string BuildYaml(RMCGeneratedReagentData data)
    {
        var desc = string.IsNullOrEmpty(data.PhysicalDescription) ? data.Name : data.PhysicalDescription;

        var lines = new List<string>
        {
            "- type: reagent",
            "  id: " + data.Id,
            "  name: " + YamlString(data.Name),
            "  group: Generated",
            "  desc: " + YamlString(desc),
            "  physicalDesc: " + YamlString(desc),
            "  creditReward: " + data.CreditReward,
            "  chemClass: " + data.ChemClass,
            "  unknown: true",
            "  fireEntity: RMCTileFireGenerated",
            "  overdose: " + data.Overdose,
            "  criticalOverdose: " + data.CriticalOverdose,
        };

        if (!string.IsNullOrEmpty(data.Color))
            lines.Add("  color: " + YamlString(data.Color));

        if (data.Properties.Any(p => p.PropertyId is "Defibrillating" or "Neurocryogenic"))
            lines.Add("  worksOnTheDead: true");

        if (data.Properties.Count > 0)
        {
            lines.Add("  metabolisms:");
            lines.Add("    Poison:");
            lines.Add("      metabolismRate: 0.1");
            lines.Add("      effects:");
            foreach (var property in data.Properties)
            {
                lines.Add("      - !type:" + property.PropertyId);
                lines.Add("        potency: " + property.Level);
            }
        }

        if (data.Ingredients.Count > 0)
        {
            var productAmount = data.Properties.Any(p => p.PropertyId == "Optimized")
                ? 3
                : data.Ingredients.Where(i => !i.Catalyst).Sum(i => Math.Max(i.Amount, 1));
            if (productAmount <= 0)
                productAmount = 1;

            lines.Add(string.Empty);
            lines.Add("- type: reaction");
            lines.Add("  id: " + RecipeId(data.Id));
            lines.Add("  reactants:");
            foreach (var ingredient in data.Ingredients)
            {
                lines.Add("    " + ingredient.Id + ":");
                lines.Add("      amount: " + Math.Max(ingredient.Amount, 1));
                if (ingredient.Catalyst)
                    lines.Add("      catalyst: true");
            }

            lines.Add("  products:");
            lines.Add("    " + data.Id + ": " + productAmount);

            lines.Add("  effects:");
            if (data.Indicator != ReactionIndicator.Calm)
            {
                lines.Add("  - !type:RMCReactionIndicatorEffect");
                lines.Add("    indicator: " + data.Indicator);
            }

            lines.Add("  - !type:RMCVolatileReactionEffect");
            lines.Add("    reagent: " + data.Id);
            lines.Add("    amount: " + productAmount);
        }

        return string.Join("\n", lines);
    }

    private static string YamlString(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}

[Serializable, NetSerializable]
public sealed class RMCGeneratedReagentSyncEvent : EntityEventArgs
{
    public List<RMCGeneratedReagentData> Data;

    public RMCGeneratedReagentSyncEvent(List<RMCGeneratedReagentData> data)
    {
        Data = data;
    }
}
