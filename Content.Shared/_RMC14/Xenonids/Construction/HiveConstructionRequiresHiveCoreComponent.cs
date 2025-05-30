using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction;

/// <summary>
/// Indicates that the hive structure must be part of a hive that has a hivecore built in order to build.
/// </summary>
[RegisterComponent]
public sealed partial class HiveConstructionRequiresHiveCoreComponent : Component
{
}
