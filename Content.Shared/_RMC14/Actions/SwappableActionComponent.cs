using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SwappableActionSystem))]
public sealed partial class SwappableActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public string OriginalName = string.Empty;

    [DataField, AutoNetworkedField]
    public string OriginalDescription = string.Empty;

    [DataField, AutoNetworkedField]
    public SwappableActionEvent SwappedEvent;

    [DataField, AutoNetworkedField]
    public SwappableActionEvent OriginalEvent;
}

[Serializable, NetSerializable]
public enum SwappableActionEvent : byte
{
    None = 0,
    XenoExpandWeeds = 1,
    XenoPlantWeeds = 2,
}
