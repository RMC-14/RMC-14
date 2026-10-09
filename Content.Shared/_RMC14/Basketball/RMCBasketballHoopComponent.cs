using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Basketball;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCBasketballSystem))]
public sealed partial class RMCBasketballHoopComponent : Component
{
    [DataField]
    public string CourtId = "basketball";

    [DataField]
    public RMCBasketballTeam Side;

    [DataField]
    public int ShotPoints = 2;

    [DataField]
    public float ShotChance = 0.5f;

    [DataField]
    public int DunkPoints = 2;

    [DataField]
    public string SensorFixtureId = "basketball";
}

[Serializable, NetSerializable]
public enum RMCBasketballTeam
{
    Left,
    Right,
}
