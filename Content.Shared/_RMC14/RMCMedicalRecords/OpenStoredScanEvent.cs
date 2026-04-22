using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.RMCMedicalRecords;

[Serializable, NetSerializable]
public sealed class OpenStoredScanEvent(NetEntity target) : EntityEventArgs
{
    public readonly NetEntity Target = target;
}
