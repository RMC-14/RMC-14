using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
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
    public int ExplosionArmor = 60;

    [DataField, AutoNetworkedField]
    public string[] ImmuneToStatuses = { "KnockedDown" };

    [DataField]
    public IPhysShape Shape = new PhysShapeCircle(0.49f);

    [DataField, AutoNetworkedField]
    public RMCSizes FortifySize = RMCSizes.Immobile;

    [DataField, AutoNetworkedField]
    public RMCSizes? OriginalSize;

    [DataField, AutoNetworkedField]
    public bool ChangeExplosionWeakness = true;

    [DataField, AutoNetworkedField]
    public bool BaseWeakToExplosionStuns = true;

    [DataField, AutoNetworkedField]
    public bool CanMoveFortified = false;

    [DataField, AutoNetworkedField]
    public bool CanAttackHumanoidsFortified = false;

    [DataField, AutoNetworkedField]
    public bool CanHeadbuttFortified = false;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MoveSpeedModifier = FixedPoint2.New(0.45);

    [DataField, AutoNetworkedField]
    public DamageSpecifier DamageAddedFortified = new();

    [DataField, AutoNetworkedField]
    public SoundSpecifier FortifySound = new SoundPathSpecifier("/Audio/Effects/stonedoor_openclose.ogg");
}
