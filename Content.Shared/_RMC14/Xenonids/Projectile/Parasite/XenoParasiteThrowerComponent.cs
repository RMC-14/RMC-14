using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Projectile.Parasite;

/// <summary>
/// Allows a xeno to throw parasites using the "Throw Parasite" Action
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoParasiteThrowerComponent : Component
{
    public EntProtoId ParasitePrototype = "CMXenoParasite";

    [AutoNetworkedField]
    public int ReservedParasites = 0;

    [DataField]
    public double ParasiteGhostRoleProbability = 0.25;

    [DataField]
    public double ParasiteThrowDistance = 4.0;

    [DataField, AutoNetworkedField]
    public int MaxParasites = 16;

    [DataField, AutoNetworkedField]
    public int CurParasites = 0;

    [DataField]
    public TimeSpan ThrownParasiteStunDuration = TimeSpan.FromSeconds(5);
}
