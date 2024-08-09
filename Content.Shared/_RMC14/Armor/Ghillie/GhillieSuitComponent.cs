using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor.Ghillie;

/// <summary>
/// Component for ghillie suit abilities
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedGhillieSuitSystem))]
public sealed partial class GhillieSuitComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = false;

    [DataField, AutoNetworkedField]
    public TimeSpan UseDelay = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new();

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionGhilliePreparePosition";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    /// <summary>
    /// Components to remove when the suit is disabled, add when enabled.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry AddComponentsOnEnable = new();

    /// <summary>
    /// Components to remove when the suit is enabled, add when disabled.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry AddComponentsOnDisable = new();
}