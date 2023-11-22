using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Fortify;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoFortifySystem))]
public sealed partial class XenoFortifyComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Fortified;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Armor = 30;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int FrontalArmor = 5;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ExplosionMultiplier = 0.4f;
}
