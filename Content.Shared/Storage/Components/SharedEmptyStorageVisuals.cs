using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Components
{
    [Serializable, NetSerializable]
    public enum SharedEmptyStorageVisuals : byte
    {
        StorageState,
    }

    [Serializable, NetSerializable]
    public enum SharedStorageState : byte
    {
        Empty,
        NotEmpty,
    }
}
