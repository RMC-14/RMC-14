using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Comms;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DecryptionComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string CurrentChallengeCode = "ABCD1234";

    [DataField, AutoNetworkedField]
    public bool HasGracePeriod;

    [DataField, AutoNetworkedField]
    public TimeSpan GracePeriodEnd;

    [DataField, AutoNetworkedField]
    public string StatusMessage = "Ready for decryption";
}
