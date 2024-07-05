using Content.Shared.Examine;
using ConstructionInteractionVerbComponent = Content.Shared.Construction.Components.ConstructionInteractionVerbComponent;

namespace Content.Shared.Construction.Steps;

public sealed partial class InteractionGraphStep : ConstructionGraphStep
{
    [DataField]
    public string? Interaction;

    /// <summary>
    /// A localization string used for the guidebook.
    /// </summary>
    [DataField]
    public LocId GuideString = "construction-interact-entity-guide";

    public override void DoExamine(ExaminedEvent examinedEvent)
    {
        var target = examinedEvent.Examined;

        var entMan = IoCManager.Resolve<EntityManager>();
        if (entMan.TryGetComponent<ConstructionInteractionVerbComponent>(target, out var verbComp))
        {
            examinedEvent.PushMarkup(Loc.GetString("construction-interact-entity-verb",
                ("target", target),
                ("verbName", verbComp.VerbText)));
        }
    }

    public override ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = GuideString,
        };
    }
}
