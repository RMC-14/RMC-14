using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

/// <summary>
/// Fire group component which prevents things like shotgun juggling.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedFireGroupSystem))]
public sealed partial class RMCFireGroupComponent : Component
{
    /// <summary>
    /// The fire group of the item
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Group = string.Empty;

    /// <summary>
    /// How long the other gun(s) are delayed for
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The UseDelay ID
    /// </summary>
    [DataField, AutoNetworkedField]
    public string UseDelayID = "RMCFireGroupDelay";
}