using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Banish;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoBanishSystem))]
public sealed partial class XenoBanishComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Banished;

    [DataField, AutoNetworkedField]
    public TimeSpan BanishedAt;

    [DataField, AutoNetworkedField]
    public string? BanishReason;

    [DataField, AutoNetworkedField]
    public string? BanishedBy;

    [DataField, AutoNetworkedField]
    public TimeSpan ReadmitAvailableAt;
}