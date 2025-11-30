using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Shared._RMC14.TrainingDummy;

public sealed partial class RMCTrainingDummySystem : EntitySystem
{
    [Dependency] private readonly GrammarSystem _grammarSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCTrainingDummyComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<RMCTrainingDummyComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.RemoveComponents != null)
            EntityManager.RemoveComponents(ent.Owner, ent.Comp.RemoveComponents);

        // Can't set gender via component as it gets overwritten on spawn.
        if (TryComp<GrammarComponent>(ent.Owner, out var grammar))
        {
            _grammarSystem.SetGender((ent.Owner, grammar), Gender.Neuter);
            _grammarSystem.SetProperNoun((ent.Owner, grammar), false);
        }
    }
}
