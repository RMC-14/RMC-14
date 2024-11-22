using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;

/// <summary>
/// Allows the entity to build resin structures at a distance
/// </summary>
[RegisterComponent]
public sealed partial class ResinWhisperComponent : Component
{
    [DataField]
    public TimeSpan? ConstructDelay = null;

    [DataField]
    public float MaxRemoteDistance = 30f;

    /// <summary>
    /// Multiplier of the resin structure build delay
    /// </summary>
    [DataField]
    public float RemoteConstructDelayMultiplier = 2.5f;
}
