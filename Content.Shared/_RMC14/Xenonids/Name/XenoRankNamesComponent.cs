using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Name;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoRankNamesComponent : Component
{
    [DataField]
    public Dictionary<int, LocId> RankNames = new()
    {
        {0, "rmc-xeno-young"},
        {2, "rmc-xeno-mature"},
        {3, "rmc-xeno-elder"},
        {4, "rmc-xeno-ancient"},
        {5, "rmc-xeno-prime"},
    };
}
