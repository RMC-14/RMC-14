using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Fruit.Events;

// Events for fruits that need them
[Serializable, NetSerializable]
public sealed partial class XenoFruitEffectRegenEvent : HandledEntityEventArgs;

[Serializable, NetSerializable]
public sealed partial class XenoFruitEffectPlasmaEvent : HandledEntityEventArgs;
