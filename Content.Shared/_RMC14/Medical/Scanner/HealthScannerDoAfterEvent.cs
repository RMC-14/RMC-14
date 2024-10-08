using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Scanner;

[Serializable, NetSerializable]
public sealed partial class HealthScannerDoAfterEvent : SimpleDoAfterEvent;
