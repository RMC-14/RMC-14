using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.AlertLevel;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCAlertLevelSystem))]
public sealed partial class RMCAlertLevelDisplayComponent : Component;

[Serializable, NetSerializable]
public enum RMCAlertLevelDisplayVisualLayers : byte
{
    MinuteOnes,
    MinuteTens,
    Separator,
    HourOnes,
    HourTens
}
