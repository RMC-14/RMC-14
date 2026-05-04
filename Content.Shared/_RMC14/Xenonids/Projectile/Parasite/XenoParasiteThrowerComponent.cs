using Content.Shared._RMC14.Medical.Wounds;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Projectile.Parasite;

/// <summary>
/// Allows a xeno to throw parasites using the "Throw Parasite" Action
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoParasiteThrowerComponent : Component, IComponentDebug
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

    public string GetDebugString()
    {
        return $"""
            ParasitePrototype: {ParasitePrototype.Id}
            ReservedParasites: {ReservedParasites}
            ParasiteThrowDistance: {ParasiteThrowDistance}
            MaxParasites: {MaxParasites}
            CurParasites: {CurParasites}
            ThrownParasiteStunDuration: {ThrownParasiteStunDuration.TotalSeconds}
            ThrownParasiteCooldown: {ThrownParasiteStunDuration.TotalSeconds}
            NumPositions: {NumPositions}
            VisiblePositions: [{string.Join(", ", VisiblePositions)}]
            """;
    }
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
