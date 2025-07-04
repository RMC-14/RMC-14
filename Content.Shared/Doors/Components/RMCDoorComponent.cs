using Robust.Shared.Serialization;

namespace Content.Shared.Doors.Components;

/// <summary>
///     This is an extension of the upstream DoorComponent.
/// </summary>
public sealed partial class DoorComponent
{
    [DataField, AutoNetworkedField]
    public DoorLocation Location;
}

[Serializable, NetSerializable]
public enum DoorLocation : byte
{
    None,
    Aft,
    Bow,
    Cockpit,
    Port,
    Starboard,
}
