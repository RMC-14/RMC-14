using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[ByRefEvent]
public readonly record struct XenoChangingPrototypeEvent(EntityUid Xeno, EntityPrototype NewProto, ComponentRegistry NewComponents, HashSet<string> AdditionalExclusions);
