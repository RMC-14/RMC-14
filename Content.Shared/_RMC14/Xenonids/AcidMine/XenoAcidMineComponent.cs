using Content.Shared._RMC14.Xenonids.DeployTraps;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.AcidMine;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAcidMineSystem), typeof(XenoDeployTrapsSystem))]
public sealed partial class XenoAcidMineComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier DamageToMobs;

    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier DamageToStructures;

    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier DamageToStructuresEmpowered;

    [DataField, AutoNetworkedField]
    public bool Empowered = false;

    [DataField, AutoNetworkedField]
    public int Range = 10;

    [DataField]
    public DoAfterId? AcidMineDoAfter;

    [DataField]
    public FixedPoint2 PlasmaCost = 40;

    //1 for a 3x3 area.
    [DataField, AutoNetworkedField]
    public int AcidMineRadius = 1;

    // Length of do-after
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public TimeSpan DeployTrapsCooldownReduction = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public EntProtoId TelegraphEffect = "RMCEffectXenoTelegraphRed";
}
