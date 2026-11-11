using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Hallucination.Events;

[RegisterComponent]
public sealed partial class RMCHallucinationBodyMessageComponent : Component
{
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> Lines = "RMCHallucinationBodyLines";
}
