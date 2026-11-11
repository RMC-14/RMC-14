using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Hallucination.Events;

[RegisterComponent]
public sealed partial class RMCHallucinationFakeAnnouncementComponent : Component
{
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> OverwatchLines = "RMCHallucinationOverwatchLines";
}
