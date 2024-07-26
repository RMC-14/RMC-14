using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Inventory;
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
    public Skills SkillRequired = new() { SpecialistWeapons = 1 };

    [DataField, AutoNetworkedField]
    public bool Enabled = false;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionGhilliePreparePosition";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public TimeSpan CloakDelay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Components to add and remove.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}