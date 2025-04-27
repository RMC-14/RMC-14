using Content.Server.Humanoid;
using Content.Server.Humanoid.Systems;
using Content.Shared._RMC14.Humanoid.Markings;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Humanoid.Markings;

public sealed class RMCRandomMarkingsSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCRandomMarkingsComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(RandomHumanoidAppearanceSystem), typeof(RandomHumanoidSystem) }
        );
    }

    private void OnMapInit(Entity<RMCRandomMarkingsComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid))
            return;

        if (!ent.Comp.Markings.TryGetValue(humanoid.Species, out var speciesCategory))
            return;

        foreach (var type in speciesCategory)
        {
            if (!_random.Prob(type.Value))
                continue;

            var possibleMarkings = _markings.MarkingsByCategoryAndSpeciesAndSex(type.Key, humanoid.Species, humanoid.Sex);

            if (possibleMarkings.Count == 0)
                continue;

            var pickedMarking = _random.Pick(possibleMarkings);

            if (!humanoid.MarkingSet.Markings.TryGetValue(type.Key, out var markings))
            {
                _humanoidAppearance.AddMarking(ent, pickedMarking.Key, humanoid.SkinColor, forced: true);
                continue;
            }

            if (markings.Count == 0)
            {
                _humanoidAppearance.AddMarking(ent, pickedMarking.Key, humanoid.SkinColor, forced: true);
                continue;
            }

            for (var idx = 0; idx < markings.Count; idx++) // Replace existing markings
            {
                _humanoidAppearance.SetMarkingId(ent, type.Key, idx, pickedMarking.Key);

                var colors = new List<Color>();
                var markingColors = markings[idx].MarkingColors.Count;

                for (int i = 0; i < markingColors - 1; i++)
                {
                    colors.Add(humanoid.SkinColor);
                }

                _humanoidAppearance.SetMarkingColor(ent, type.Key, idx, colors);
            }
        }
    }
}
