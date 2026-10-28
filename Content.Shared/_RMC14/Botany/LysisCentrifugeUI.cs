using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Botany;

[Serializable, NetSerializable]
public enum LysisCentrifugeUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class LysisCentrifugeExtractGeneBuiMsg(PlantGeneType geneType) : BoundUserInterfaceMessage
{
    public readonly PlantGeneType GeneType = geneType;
}

[Serializable, NetSerializable]
public sealed class LysisCentrifugeClearBufferBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class LysisCentrifugeEjectDiscBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class LysisCentrifugeProcessSeedBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class LysisCentrifugeEjectSeedBuiMsg : BoundUserInterfaceMessage;
