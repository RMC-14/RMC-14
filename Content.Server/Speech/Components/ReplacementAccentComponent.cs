using Content.Server.Speech.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech.Components;

/// <summary>
/// Replaces full sentences or words within sentences with new strings.
/// </summary>
[RegisterComponent]
public sealed partial class ReplacementAccentComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ReplacementAccentPrototype> Accent = default!;

}
