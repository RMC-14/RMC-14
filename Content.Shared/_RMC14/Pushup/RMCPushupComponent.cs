using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Pushup;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRMCPushupSystem))]
public sealed partial class RMCPushupComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1.2);

    [DataField]
    public float ProperOffsetPixels = 5f;

    [DataField]
    public float KneeOffsetPixels = 2.5f;

    [DataField]
    public double MinimumStaminaFraction = 0.1;

    [DataField]
    public double BaseStaminaCost = 2.5;

    [DataField]
    public double MinimumStaminaCost = 1;

    [DataField]
    public double NoEnduranceModifier = 5;

    [DataField]
    public double TrainedEnduranceModifier = -1;

    [DataField]
    public double MasterEnduranceModifier = -2;

    [DataField]
    public double ExpertEnduranceModifier = -3;

    [DataField]
    public double GearClassModifier = 0.5;

    [DataField]
    public double StarvingModifier = 2;

    [DataField]
    public double InjuredModifier = 2;

    [DataField]
    public float InjuredThreshold = 0.1f;

    [DataField]
    public double KneeModifier = -2;

    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public RMCPushupForm Form;

    [DataField, AutoNetworkedField]
    public bool Routine;

    public ushort? CurrentDoAfter;

    public int Count;
}

public enum RMCPushupForm : byte
{
    Proper,
    Knees,
}
