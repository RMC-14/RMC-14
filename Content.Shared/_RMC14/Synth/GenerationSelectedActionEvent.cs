using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Synth;

[Serializable, NetSerializable]
public record GenerationSelectedActionEvent(EntProtoId<SynthGenerationComponent> Generation);
