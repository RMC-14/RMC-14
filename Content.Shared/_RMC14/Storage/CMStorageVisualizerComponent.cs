namespace Content.Shared._CM14.Storage;

[RegisterComponent]
public sealed partial class CMStorageVisualizerComponent : Component
{
    [DataField(required: true)]
    public string StorageOpen;

    [DataField(required: true)]
    public string StorageClosed;
}
