using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.SightRestriction;

[Serializable, NetSerializable]
public sealed partial class SightRestrictionChangedEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed partial class SightRestrictionRemovedEvent : EntityEventArgs;
