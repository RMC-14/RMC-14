using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Comms;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DecoderComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string CurrentChallengeCode = "Awaiting current stream...";

    [DataField, AutoNetworkedField]
    public string CurrentChallengeWord = "";

    [DataField, AutoNetworkedField]
    public bool HasGracePeriod;

    [DataField, AutoNetworkedField]
    public TimeSpan GracePeriodEnd;

    [DataField, AutoNetworkedField]
    public string StatusMessage = "Ready for decode";

    [DataField, AutoNetworkedField]
    public int PunchcardCount = 10;

    [DataField, AutoNetworkedField]
    public TimeSpan ChallengeExpiry;
}
