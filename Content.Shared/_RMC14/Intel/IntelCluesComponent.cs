using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelCluesComponent : Component
{
    [DataField, AutoNetworkedField]
    public string InitialArea = string.Empty;

    [DataField, AutoNetworkedField]
    public int Clues; // TODO RMC14 implement

    [DataField, AutoNetworkedField]
    public LocId Clue = "rmc-intel-clue-paper-scrap";

    [DataField, AutoNetworkedField]
    public LocId? Category;
}
