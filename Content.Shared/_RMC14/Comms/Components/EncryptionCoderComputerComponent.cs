using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Comms;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EncryptionCoderComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string LastSubmittedCode = "";

    [DataField, AutoNetworkedField]
    public int KnownLetters;

    [DataField, AutoNetworkedField]
    public string ClarityDescription = "unknown";

    [DataField, AutoNetworkedField]
    public string CurrentWord = "";

    [DataField, AutoNetworkedField]
    public int CurrentOffset;

    [DataField, AutoNetworkedField]
    public int PunchcardCount = 10;
}
