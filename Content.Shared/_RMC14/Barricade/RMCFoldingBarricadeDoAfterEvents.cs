using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Barricade;

[Serializable, NetSerializable]
public sealed partial class RMCFoldingBarricadeDeployDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RMCFoldingBarricadeCollapseDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RMCFoldingBarricadeRepairDoAfterEvent : SimpleDoAfterEvent;
