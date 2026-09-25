using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Requisitions.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BlackMarketValueComponent : Component
{
    [DataField]
    public int Value;

    [DataField]
    public int? DeadValue;

    [DataField]
    public bool UseStackCount = true;

    [DataField]
    public bool KillsMendozaWhenSoldAlive;
}
