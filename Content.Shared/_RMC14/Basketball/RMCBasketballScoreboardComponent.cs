using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Basketball;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(RMCBasketballSystem))]
public sealed partial class RMCBasketballScoreboardComponent : Component
{
    [DataField]
    public string CourtId = "basketball";

    [DataField]
    public int MaxScore = 99;

    [DataField, AutoNetworkedField]
    public int LeftScore;

    [DataField, AutoNetworkedField]
    public int RightScore;
}

[Serializable, NetSerializable]
public enum RMCBasketballScoreboardLayers
{
    Base,
    LeftTens,
    LeftOnes,
    RightTens,
    RightOnes,
}
