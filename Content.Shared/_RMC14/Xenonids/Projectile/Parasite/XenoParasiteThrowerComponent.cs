using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Projectile.Parasite;

/// <summary>
/// Allows a xeno to throw parasites using the "Throw Parasite" Action
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoParasiteThrowerComponent : Component
{
    public EntProtoId ParasitePrototype = "CMXenoParasite";

    [DataField, AutoNetworkedField]
    public int ReservedParasites = 0;

    [DataField]
    public float ParasiteThrowDistance = 4.0f;

    [DataField, AutoNetworkedField]
    public int MaxParasites = 16;

    [DataField, AutoNetworkedField]
    public int CurParasites = 0;

    [DataField]
    public TimeSpan ThrownParasiteStunDuration = TimeSpan.FromSeconds(7.5); //5 seconds in practice to account for less xeno stun time

    [DataField]
    public TimeSpan ThrownParasiteCooldown = TimeSpan.FromSeconds(2);

    //Should equal visible position length
    [DataField]
    public int NumPositions = 4;

    [DataField, AutoNetworkedField]
    public bool[] VisiblePositions = [false, false, false, false];
}

[Serializable, NetSerializable]
public enum ParasiteOverlayVisuals
{
    Resting,
    Downed,
    States
}

[Serializable, NetSerializable]
public enum ParasiteOverlayLayers : int
{
    RightArm = 0,
    Head = 1,
    LeftArm = 2,
    Back = 3
}
