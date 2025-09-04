using Content.Server.Humanoid;
using Content.Server.Humanoid.Systems;
using Content.Shared._RMC14.Humanoid.Markings;
using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Humanoid.Markings;

public sealed class RMCConditionalMarkingsSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCConditionalMarkingsComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(RandomHumanoidAppearanceSystem), typeof(RandomHumanoidSystem) }
        );
    }

    private void OnMapInit(Entity<RMCConditionalMarkingsComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid))
            return;

        var listToUse = humanoid.Gender switch
        {
            Gender.Female => ent.Comp.Markings[Sex.Female],
            Gender.Male => ent.Comp.Markings[Sex.Male],
            Gender.Epicene or Gender.Neuter or _ => ent.Comp.Markings[Sex.Male]
        };

        listToUse = humanoid.Sex switch
        {
            Sex.Female => ent.Comp.Markings[Sex.Female],
            Sex.Male => ent.Comp.Markings[Sex.Male],
            _ => listToUse // Sexless mobs use gender
        };

        var pickedMarking = _random.Pick(listToUse);

        if (!humanoid.MarkingSet.Markings.TryGetValue(ent.Comp.TargetCategory, out var markings))
        {
            _humanoidAppearance.AddMarking(ent, pickedMarking);
            return;
        }

        if (markings.Count == 0)
        {
            _humanoidAppearance.AddMarking(ent, pickedMarking);
            return;
        }

        for (var idx = 0; idx < markings.Count; idx++) // Replace existing markings
        {
            _humanoidAppearance.SetMarkingId(ent, ent.Comp.TargetCategory, idx, pickedMarking);
        }
    }
}
