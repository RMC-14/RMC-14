namespace Content.Shared.Construction.Components;

[RegisterComponent]
public sealed partial class ConstructionInteractionVerbComponent : Component
{
    [DataField]
    public LocId VerbText = "construction-interaction-component-verb";

    [DataField]
    public LocId VerbFailedText = "construction-interaction-component-verb-failed";
}
