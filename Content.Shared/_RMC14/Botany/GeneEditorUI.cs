using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Botany;

[Serializable, NetSerializable]
public enum GeneEditorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class GeneEditorApplyBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class GeneEditorEjectSeedBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class GeneEditorEjectDiscBuiMsg : BoundUserInterfaceMessage;
