using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction;

public sealed partial class EggMorpherComponent : Component
{
    /// <summary>
    /// Currently stored parasites
    /// </summary>
    [DataField]
    public int CurParasites = 0;

    /// <summary>
    /// Max stored parasites
    /// </summary>
    [DataField]
    public int MaxParasites = 12;

    /// <summary>
    /// Max parasites that can be grown passively within the egg morpher
    /// </summary>
    [DataField]
    public int GrowMax = 6;

    [DataField]
    public int ReservedParasites = 0;

    /// <summary>
    /// How long it takes to spawn a single parasite
    /// </summary>
    [DataField]
    public TimeSpan StandardSpawnCooldown = TimeSpan.FromSeconds(120);

    /// <summary>
    /// How long it takes to spawn a single parasite while the queen is oving
    /// </summary>
    [DataField]
    public TimeSpan OviSpawnCooldown = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan? NextSpawnAt;
}
