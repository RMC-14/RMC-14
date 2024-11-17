using Content.Shared._RMC14.Stun;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Shared._RMC14.Xenonids.Fortify;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoFortifySystem))]
public sealed partial class XenoFortifyComponent : Component
{
    public const string FixtureId = "cm-xeno-fortify";

    [DataField, AutoNetworkedField]
    public bool Fortified;

    [DataField, AutoNetworkedField]
    public int Armor = 30;

    [DataField, AutoNetworkedField]
    public int FrontalArmor = 5;

    [DataField, AutoNetworkedField]
    public float ExplosionMultiplier = 0.4f;

    [DataField, AutoNetworkedField]
    public string[] ImmuneToStatuses = { "KnockedDown" };

    [DataField]
    public IPhysShape Shape = new PhysShapeCircle(0.49f);

    [DataField, AutoNetworkedField]
    public RMCSizes FortifySize = RMCSizes.Immobile;

    [DataField, AutoNetworkedField]
    public RMCSizes? OriginalSize;
}
