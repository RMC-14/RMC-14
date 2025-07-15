using Robust.Shared.Serialization;

// ReSharper disable CheckNamespace
namespace Content.Shared.Doors.Components;
// ReSharper restore CheckNamespace

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
