using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;

/// <summary>
/// Raised during validation to allow dyanamic changes to the XenoConstructionComponet as needed
/// </summary>
[ByRefEvent]
public record struct XenoSecreteStructureAdjustFields(EntityCoordinates TargetCoordinates);
