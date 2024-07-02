namespace Content.Shared._RMC14.Storage;

[RegisterComponent]
public sealed partial class CMStorageVisualizerComponent : Component
{
    [DataField]
    public string? StorageClosed;

    [DataField]
    public string? StorageOpen;
}
