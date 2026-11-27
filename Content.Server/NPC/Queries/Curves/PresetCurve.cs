using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries.Curves;

public sealed partial class PresetCurve : IUtilityCurve
{
    [DataField("preset", required: true)] public  ProtoId<UtilityCurvePresetPrototype> Preset = default!;
}
