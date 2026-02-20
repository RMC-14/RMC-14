using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Photocopier;
[Serializable, NetSerializable]
public enum PhotocopierUIKey: byte
{
    Key
}
[Serializable, NetSerializable]
public sealed class PhotocopierUiState : BoundUserInterfaceState
{
    public string PaperName = "";
    public bool IsPaperInserted = false;
    public bool CanCopy = false;

    public PhotocopierUiState(string paperName, bool isPaperInserted, bool canCopy)
    {
        PaperName = paperName;
        IsPaperInserted = isPaperInserted;
        CanCopy = canCopy;
    }
}
