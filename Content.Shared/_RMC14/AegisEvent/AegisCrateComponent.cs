using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.AegisCrate;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AegisCrateComponent : Component
{
    [DataField, AutoNetworkedField]
    public AegisCrateState State { get; set; } = AegisCrateState.Closed;

    [DataField("openSound")]
    public SoundSpecifier? OpenSound = new SoundPathSpecifier("/Audio/_RMC14/Structures/secure_box_opening/secure_box_opening.ogg");

    [DataField("closeSound")]
    public SoundSpecifier? CloseSound;

    [DataField, AutoNetworkedField]
    public EntProtoId OB = "RMCOrbitalCannonWarheadAegis";

    [DataField, AutoNetworkedField]
    public TimeSpan? OpenAt;

    [DataField, AutoNetworkedField]
    public bool Spawned = false;
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
