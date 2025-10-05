using Content.Shared.Doors.Systems;
using Content.Shared.Tools;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

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

    [DataField]
    public SoundSpecifier XenoPrySound = new SoundCollectionSpecifier("RMCXenoPry");

    [DataField]
    public SoundSpecifier XenoPodDoorPrySound = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg");

    [DataField, AutoNetworkedField]
    public EntityUid? SoundEntity;
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

[ByRefEvent]
public record struct RMCDoorPryEvent(EntityUid User)
{
    public readonly EntityUid User = User;

    public bool Cancelled;
}
