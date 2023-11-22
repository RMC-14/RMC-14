using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Shared._CM14.Xenos.Fortify;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoFortifySystem))]
public sealed partial class XenoFortifyComponent : Component
{
    public const string FixtureId = "cm-xeno-fortify";

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

    [DataField]
    public IPhysShape Shape = new PhysShapeCircle(0.49f);
}
