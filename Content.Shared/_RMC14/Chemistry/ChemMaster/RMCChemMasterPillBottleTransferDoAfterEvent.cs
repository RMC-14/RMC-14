using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.ChemMaster;

[Serializable, NetSerializable]
public sealed partial class RMCChemMasterPillBottleTransferDoAfterEvent : SimpleDoAfterEvent;
