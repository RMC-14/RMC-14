using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._RMC14.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Movement.Components;
using Content.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Synth;

public sealed class SharedSynthGenerationSystem : EntitySystem
{

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SynthGenerationComponent, GenerationSelectActionEvent>(OnGenerationSelectAction);
        SubscribeLocalEvent<SynthGenerationComponent, GenerationSelectedActionEvent>(OnGenerationSelectedAction);
        SubscribeLocalEvent<SynthGenerationComponent, GenerationConfirmedEvent>(OnGenerationConfirmed);
        SubscribeLocalEvent<SynthGenerationComponent, MapInitEvent>(OnGenerationMapInit);
        SubscribeLocalEvent<SynthGenerationComponent, PlayerAttachedEvent>(OnGenerationPlayerAttached);
        SubscribeLocalEvent<SynthGenerationComponent, PlayerSpawnCompleteEvent>(OnGenerationSpawnComplete);
    }

    public void SynthStartup(Entity<SynthComponent> ent)
    {
        EnsureComp(ent, out SynthGenerationComponent comp);

        if (comp.Generation != null)
        {
            ApplyGenerationModifier((ent.Owner, comp));

            return;
        }

        _actions.AddAction(ent, ref comp.SelectGenerationActionEntity, comp.GenerationAction);
        Dirty(ent.Owner, comp);
    }

    private void ApplyGenerationModifier(Entity<SynthGenerationComponent> ent)
    {
        if (ent.Comp.DamageModifier is { } mod &&
            TryComp<DamageableComponent>(ent, out var dmg))
        {
            _damageable.SetDamageModifierSetId(ent, mod, dmg);
        }
    }

    private void OnGenerationPlayerAttached(Entity<SynthGenerationComponent> ent, ref PlayerAttachedEvent args)
    {
        if (ent.Comp.Generation != null)
            return;

        GenerationPopup(ent);
    }

    private void OnGenerationSpawnComplete(Entity<SynthGenerationComponent> ent, ref PlayerSpawnCompleteEvent args)
    {
        if (!HasComp<RMCAdminSpawnedComponent>(ent))
            return;

        GenerationPopup(ent);
    }


    private void OnGenerationSelectAction(Entity<SynthGenerationComponent> ent, ref GenerationSelectActionEvent args)
    {
        GenerationPopup(ent);
    }

    private void OnGenerationMapInit(Entity<SynthGenerationComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Generation is not { } generation ||
            !_prototype.TryIndex(generation, out var proto))
        {
            return;
        }

        var repOverride = EnsureComp<RMCHumanoidRepresentationOverrideComponent>(ent);
        repOverride.Age = proto.Name;
        Dirty(ent.Owner, repOverride);
    }

    private void GenerationPopup(Entity<SynthGenerationComponent> ent)
    {
        if (_net.IsClient)
            return;

        var options = new List<DialogOption>();
        var synthTypes = new List<(EntityPrototype Proto, int Priority)>();

        if (ent.Comp.AvailableGenerations.Count > 0)
        {
            foreach (var id in ent.Comp.AvailableGenerations)
            {
                if (_prototype.TryIndex(id, out var proto) &&
                    proto.TryGetComponent(out SynthGenerationComponent? gen, _compFactory))
                    synthTypes.Add((proto, gen.Priority));
            }
        }
        else
        {
            foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.TryGetComponent(out SynthGenerationComponent? gen, _compFactory))
                    synthTypes.Add((proto, gen.Priority));
            }
        }

        synthTypes.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        foreach (var (proto, _) in synthTypes)
        {
            var desc = proto.TryGetComponent(out SynthGenerationComponent? genComp, _compFactory)
                ? genComp.Description
                : string.Empty;
            options.Add(new DialogOption(proto.Name, new GenerationSelectedActionEvent(proto.ID), description: desc));
        }

        _dialog.OpenOptions(ent.Owner, "Select a Generation", options, "Available Generations");
    }

    private void OnGenerationSelectedAction(Entity<SynthGenerationComponent> ent, ref GenerationSelectedActionEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Generation != null)
            return;

        if (!_prototype.TryIndex(args.Generation, out var proto))
            return;

        _dialog.OpenConfirmation(
            ent.Owner,
            "Confirm Generation",
            $"Please confirm {proto.Name} selection.",
            new GenerationConfirmedEvent(args.Generation));
    }

    private void OnGenerationConfirmed(Entity<SynthGenerationComponent> ent, ref GenerationConfirmedEvent args)
    {
        if (ent.Comp.Generation != null)
            return;

        if (!_prototype.TryIndex(args.Generation, out var proto))
        {
            Log.Warning("attempting to index Entity prototype failed");
            return;
        }

        var actionEntity = ent.Comp.SelectGenerationActionEntity;

        EntityManager.AddComponents(ent, proto);

        if (TryComp<SynthGenerationComponent>(ent, out var gen))
            ApplyGenerationModifier((ent.Owner, gen));

        _actions.RemoveAction(ent.Owner, actionEntity);
    }
}
