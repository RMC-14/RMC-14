using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._RMC14.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Synth;

public sealed class SharedSynthGenerationSystem : EntitySystem
{

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SynthGenerationComponent, GenerationSelectActionEvent>(OnGenerationSelectAction);
        SubscribeLocalEvent<SynthGenerationComponent, GenerationSelectedActionEvent>(OnGenerationSelectedAction);
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

        ClearGeneration(ent);
        GenerationPopup(ent);
    }

    private void ClearGeneration(Entity<SynthGenerationComponent> ent)
    {
        if (ent.Comp.Generation is { } current && _prototype.TryIndex(current, out var proto))
        {
            var keep = _compFactory.GetComponentName(typeof(SynthGenerationComponent));
            foreach (var (name, _) in proto.Components)
            {
                if (name == keep)
                    continue;

                EntityManager.RemoveComponent(ent.Owner, _compFactory.GetRegistration(name).Type);
            }
        }

        ent.Comp.Generation = null;
        Dirty(ent);
        _actions.AddAction(ent.Owner, ref ent.Comp.SelectGenerationActionEntity, ent.Comp.GenerationAction);
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
        HashSet<EntProtoId<SynthGenerationComponent>> synthTypes = [];

        foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.HasComponent<SynthGenerationComponent>())
                synthTypes.Add(proto.ID);
        }

        foreach (var synth in synthTypes)
        {
            if (!_prototype.TryIndex(synth, out var proto))
                continue;
            options.Add(new DialogOption($"{proto.Name}", new GenerationSelectedActionEvent(synth)));
        }

        _dialog.OpenOptions(ent.Owner, "Select a Generation", options, "Available Generations");
    }

    private void OnGenerationSelectedAction(Entity<SynthGenerationComponent> ent, ref GenerationSelectedActionEvent args)
    {
        if (ent.Comp.Generation != null)
            return;

        if (!_prototype.TryIndex(args.Generation, out var proto))
        {
            Log.Warning("attempting to index Entity prototype failed");
            return;
        }

        EntityManager.AddComponents(ent, proto);

        if (TryComp<SynthGenerationComponent>(ent, out var gen))
            ApplyGenerationModifier((ent.Owner, gen));

        _actions.RemoveAction(ent.Owner, ent.Comp.SelectGenerationActionEntity);
    }
}
