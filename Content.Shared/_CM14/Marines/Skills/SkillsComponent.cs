using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Marines.Skills;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SkillsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Antagonist;

    [DataField, AutoNetworkedField]
    public int Construction;

    /// <summary>
    ///     Close quarters combat
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Cqc;

    [DataField, AutoNetworkedField]
    public int Domestics;

    [DataField, AutoNetworkedField]
    public int Endurance;

    [DataField, AutoNetworkedField]
    public int Engineer;

    [DataField, AutoNetworkedField]
    public int Execution;

    [DataField, AutoNetworkedField]
    public int Fireman;

    [DataField, AutoNetworkedField]
    public int Intel;

    [DataField, AutoNetworkedField]
    public int Jtac;

    [DataField, AutoNetworkedField]
    public int Leadership;

    [DataField, AutoNetworkedField]
    public int Medical;

    [DataField, AutoNetworkedField]
    public int MeleeWeapons;

    [DataField, AutoNetworkedField]
    public int Navigations;

    [DataField, AutoNetworkedField]
    public int Overwatch;

    [DataField, AutoNetworkedField]
    public int Pilot;

    [DataField, AutoNetworkedField]
    public int Police;

    // forklift certified
    [DataField, AutoNetworkedField]
    public int PowerLoader;

    [DataField, AutoNetworkedField]
    public int Research;

    [DataField, AutoNetworkedField]
    public int Smartgun;

    [DataField, AutoNetworkedField]
    public int SpecialistWeapons;

    // no longer a week away
    [DataField, AutoNetworkedField]
    public int Surgery;

    [DataField, AutoNetworkedField]
    public int Vehicles;
}
