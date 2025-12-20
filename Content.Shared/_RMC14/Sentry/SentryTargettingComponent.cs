using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sentry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSentryTargetingSystem))]
public sealed partial class SentryTargetingComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<string> FriendlyFactions = new();

    [DataField, AutoNetworkedField]
    public HashSet<string> DeployedFriendlyFactions = new();

    [DataField, AutoNetworkedField]
    public string OriginalFaction = "UNMC";

    [DataField, AutoNetworkedField]
    public HashSet<string> TargetedFactions = new();

    [DataField, AutoNetworkedField]
    public HashSet<string> HumanoidAdded = new();
}
