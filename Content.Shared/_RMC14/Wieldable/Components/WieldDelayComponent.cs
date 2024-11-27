using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Wieldable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWieldableSystem))]
public sealed partial class WieldDelayComponent : Component
{
    /// <summary>
    /// The base delay which is then modified by attachments.
    /// Conversion from 13: SS13_WIELD_DELAY / 10
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BaseDelay = TimeSpan.FromSeconds(0.4);

    [DataField, AutoNetworkedField]
    public TimeSpan ModifiedDelay = TimeSpan.FromSeconds(0.4);

    [DataField, AutoNetworkedField]
    public bool PreventFiring; // TODO RMC14 this should support increased spread, decreased accuracy, and be applied to sniper/amr
}
