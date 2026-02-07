using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Comms;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EncryptionCipherComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string InputCode = "";

    [DataField, AutoNetworkedField]
    public int CipherSetting;

    [DataField, AutoNetworkedField]
    public string DecipheredWord = "";

    [DataField, AutoNetworkedField]
    public string StatusMessage = "Ready for input";

    [DataField, AutoNetworkedField]
    public int PunchcardCount = 10;

    [DataField, AutoNetworkedField]
    public bool ValidWord;
}
