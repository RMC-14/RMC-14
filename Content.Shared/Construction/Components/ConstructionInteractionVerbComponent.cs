namespace Content.Shared.Construction.Components;

[RegisterComponent]
public sealed partial class ConstructionInteractionVerbComponent : Component
{
    [DataField]
    public string VerbText = "construction-interaction-component-verb";

    [DataField]
    public string VerbFailedText = "construction-interaction-component-verb-failed";
}
