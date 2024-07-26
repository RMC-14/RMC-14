using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Armor.Ghillie;

public sealed partial class GhillieActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class GhillieSuitDoAfterEvent : SimpleDoAfterEvent;