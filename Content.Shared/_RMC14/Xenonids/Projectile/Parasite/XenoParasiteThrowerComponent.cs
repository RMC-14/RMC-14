using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.Maths;

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
    public float ParasiteThrowDistance = RMCMathExtensions.CircleAreaFromSquareAbilityRange(6);

    [DataField, AutoNetworkedField]
    public int MaxParasites = 16;

    [DataField, AutoNetworkedField]
    public int CurParasites = 0;

    [DataField]
    public TimeSpan ThrownParasiteStunDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan DropParasiteStunDuration = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan ThrownParasiteCooldown = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan NextThrow;

    [DataField]
    public TimeSpan RetrieveParasiteCooldown = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan NextRetrieve;

    //Should equal visible position length
    [DataField]
    public int NumPositions = 4;

    [DataField, AutoNetworkedField]
    public bool[] VisiblePositions = [false, false, false, false];
}

[Serializable, NetSerializable]
public enum ParasiteOverlayVisuals
{
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
