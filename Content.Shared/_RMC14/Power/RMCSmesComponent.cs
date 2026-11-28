using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCSmesComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Charge;

    [DataField, AutoNetworkedField]
    public float MaxCharge = 200_000_000;

    [DataField, AutoNetworkedField]
    public float ChargePercentage;

    [DataField, AutoNetworkedField]
    public bool EmpDisabled;

    [DataField]
    public TimeSpan EmpDisableDuration = TimeSpan.FromSeconds(10);

    [ViewVariables]
    public TimeSpan EmpRestoreAt;

    [ViewVariables]
    public bool RestoreInput;

    [ViewVariables]
    public bool RestoreOutput;
}

[Serializable, NetSerializable]
public enum RMCSmesUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCSmesSetInputEnabledBuiMsg(bool enabled) : BoundUserInterfaceMessage
{
    public readonly bool Enabled = enabled;
}

[Serializable, NetSerializable]
public sealed class RMCSmesSetOutputEnabledBuiMsg(bool enabled) : BoundUserInterfaceMessage
{
    public readonly bool Enabled = enabled;
}

[Serializable, NetSerializable]
public sealed class RMCSmesSetInputLimitBuiMsg(float watts) : BoundUserInterfaceMessage
{
    public readonly float Watts = watts;
}

[Serializable, NetSerializable]
public sealed class RMCSmesSetOutputLimitBuiMsg(float watts) : BoundUserInterfaceMessage
{
    public readonly float Watts = watts;
}
