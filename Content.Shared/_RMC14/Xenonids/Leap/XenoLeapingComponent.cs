﻿using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Leap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoLeapSystem))]
public sealed partial class XenoLeapingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityCoordinates Origin;

    [DataField, AutoNetworkedField]
    public TimeSpan ParalyzeTime;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? LeapSound;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LeapEndTime;

    [DataField, AutoNetworkedField]
    public TimeSpan MoveDelayTime;

    [DataField, AutoNetworkedField]
    public bool KnockedDown;

    [DataField, AutoNetworkedField]
    public bool PlayedSound;

    [DataField, AutoNetworkedField]
    public bool KnockdownRequiresInvisibility;
}
