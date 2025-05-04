using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;

/// <summary>
/// Allows the entity to build resin structures at a distance. Depends on XenoConstructionComponent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ResinWhispererComponent : Component
{
    /// <summary>
    /// Normal Construction Delay from XenoConstructionComponent
    /// </summary>
    [DataField]
    public TimeSpan? StandardConstructDelay = null;

    /// <summary>
    /// Normal Construction Max Distance from XenoConstructionComponent
    /// </summary>
    [DataField]
    public FixedPoint2? MaxConstructDistance = null;

    [DataField]
    public float MaxRemoteConstructDistance = 100f;

    /// <summary>
    /// Multiplier of the resin structure build delay
    /// </summary>
    [DataField]
    public float RemoteConstructDelayMultiplier = 2.5f;
}
