using Robust.Shared.GameStates;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._RMC14.Comms;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EncryptionEncoderComputerComponent : Component
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

    [DataField]
    public ItemSlot PunchcardSlot = new();
}

