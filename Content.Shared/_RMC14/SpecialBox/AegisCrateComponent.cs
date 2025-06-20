using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Content.Shared._RMC14.Storage;
using Content.Shared.Storage;
using System;
using Robust.Shared.Audio;

namespace Content.Shared._RMC14.AegisCrate;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AegisCrateComponent : Component
{
    [DataField, AutoNetworkedField]
    public AegisCrateState State { get; set; } = AegisCrateState.Closed;

    [NonSerialized]
    public EntityUid? StorageUid;

    [DataField("openSound")]
    public SoundSpecifier? OpenSound = new SoundPathSpecifier("/Audio/_RMC14/Structures/secure_box_opening/secure_box_opening.ogg");

    [DataField("closeSound")]
    public SoundSpecifier? CloseSound;
}

public enum AegisCrateState
{
    Closed,
    Opening,
    Open
}

public enum AegisCrateVisualLayers
{
    Base
}
