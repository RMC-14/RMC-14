using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

/// <summary>
/// Totally overrides fuel reagent properties. Allows for two firing modes.
/// </summary>
/// <remarks>
/// Place on the flamer, not the tank.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlamerSystem))]
public sealed partial class RMCFlamerReagentOverrideComponent : Component
{

    [DataField, AutoNetworkedField]
    public int? NormalIntensity;

    [DataField, AutoNetworkedField]
    public int? NormalDuration;

    [DataField, AutoNetworkedField]
    public int? NormalRange;

    [DataField, AutoNetworkedField]
    public int? NormalFireCost = 1;

    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype>? NormalFireReagent;


    [DataField, AutoNetworkedField]
    public int? IntenseIntensity;

    [DataField, AutoNetworkedField]
    public int? IntenseDuration;

    [DataField, AutoNetworkedField]
    public int? IntenseRange;

    [DataField, AutoNetworkedField]
    public int? IntenseFireCost;

    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype>? IntenseFireReagent;


    [DataField, AutoNetworkedField]
    public bool HasIntenseMode = false;

    [DataField, AutoNetworkedField]
    public bool Intense = false;


    [DataField, AutoNetworkedField]
    public string ActivateText = "rmc-flamer-intense-activate";

    [DataField, AutoNetworkedField]
    public string DeactivateText = "rmc-flamer-intense-deactivate";

    [DataField, AutoNetworkedField]
    public string ExamineText = "rmc-flamer-intense-action-examine";


    [DataField, AutoNetworkedField]
    public SoundPathSpecifier? ActivateSound = new("/Audio/_RMC14/Handling/weldingtool_on.ogg");

    [DataField, AutoNetworkedField]
    public SoundPathSpecifier? DeactivateSound = new("/Audio/_RMC14/Weapons/Handling/flamer_ignition.ogg");
}
