using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Areas;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AreaSystem))]
public sealed partial class RoofingEntityComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public float Range;

    [DataField, AutoNetworkedField]
    public bool CanCAS;

    [DataField, AutoNetworkedField]
    public bool CanMortarPlace;

    [DataField, AutoNetworkedField]
    public bool CanMortarFire;

    [DataField, AutoNetworkedField]
    public bool CanOrbitalBombard;

    [DataField, AutoNetworkedField]
    public bool CanMedevac;

    [DataField, AutoNetworkedField]
    public bool CanFulton;

    [DataField, AutoNetworkedField]
    public bool CanSupplyDrop;

    [DataField, AutoNetworkedField]
    public bool CanLase;

    [DataField, AutoNetworkedField]
    public bool CanParadrop;
}
