using System.Runtime.CompilerServices;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[ByRefEvent]
public readonly record struct XenoChangingCasteEvent(EntityUid Xeno, EntityPrototype NewProto, ComponentRegistry NewComponents, HashSet<string> AdditionalExclusions);
