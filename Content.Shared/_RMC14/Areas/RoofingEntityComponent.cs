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
    public bool CanMortar;

    [DataField, AutoNetworkedField]
    public bool CanOrbitalBombard;
}
