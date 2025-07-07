using Content.Shared.Damage;
using Content.Shared.Doors.Systems;
using Content.Shared.Tools;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;

namespace Content.Shared._RMC14.Doors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCDoorComponent : Component
{
    /// <summary>
    /// What sound to play when a xeno is prying the door
    /// </summary>
    [DataField]
    public SoundSpecifier XenoPrySound = new SoundCollectionSpecifier("RMCXenoPry");
    [DataField]
    public SoundSpecifier XenoPodDoorPrySound = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg");

    [DataField, AutoNetworkedField]
    public EntityUid? SoundEntity;
}

[ByRefEvent]
public record struct RMCDoorPryEvent(EntityUid User)
{
    public readonly EntityUid User = User;

    public bool Cancelled;
}
