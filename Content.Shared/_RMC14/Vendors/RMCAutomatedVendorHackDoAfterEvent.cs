using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vendors;

[Serializable, NetSerializable]
public sealed partial class RMCAutomatedVendorHackDoAfterEvent : SimpleDoAfterEvent;
