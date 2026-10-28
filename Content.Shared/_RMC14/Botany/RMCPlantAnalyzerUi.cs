using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Botany;

[Serializable, NetSerializable]
public enum RMCPlantAnalyzerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public readonly record struct RMCPlantAnalyzerChemical(ProtoId<ReagentPrototype> ReagentId, string Name, Color Color, int Amount);
