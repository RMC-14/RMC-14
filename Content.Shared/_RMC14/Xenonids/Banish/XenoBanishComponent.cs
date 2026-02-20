using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Banish;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoBanishComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CanDamageHive = false;

    [DataField, AutoNetworkedField]
    public TimeSpan BanishedAt;

    [DataField, AutoNetworkedField]
    public string Reason = string.Empty;

    [DataField, AutoNetworkedField]
    public EntityUid? OriginalHive;
}
