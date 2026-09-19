using Content.Shared._RMC14.Marines.Orders;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Sentry.Flag;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PlantedFlagComponent : Component
{
    /// <summary>
    ///     The current mode of the flag.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FlagMode Mode;

    /// <summary>
    ///     The ID used for dropship target selection.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? Id;

    /// <summary>
    ///     The range in which the buff is applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 4.515f; // The result from RMCMathExtensions.CircleAreaFromSquareAbilityRange(3.5)

    /// <summary>
    ///     The strength of the applied buffs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Strength = 4;

    /// <summary>
    ///     Whether the flag should apply the <see cref="FocusOrderComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ApplyFocus = true;

    /// <summary>
    ///     Whether the flag should apply the <see cref="HoldOrderComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ApplyHold = true;

    /// <summary>
    ///     Whether the flag should apply the <see cref="MoveOrderComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ApplyMove;

    /// <summary>
    ///     The round time at which the flag will apply a buff.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextOrder;

    /// <summary>
    ///     The duration of the buff.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1.5);

    /// <summary>
    ///     How long it takes before the flag applies a buff again.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1.5);

    /// <summary>
    ///     The entities the flag is able to upgrade into by using a <see cref="SentryUpgradeItemComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId[]? Upgrades = ["RMCPlantedFlagExtendedRange", "RMCPlantedFlagWarbanner"];

    /// <summary>
    ///     Tge bane if the fixture after deploying
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? DeployFixture = "deploy";
}

[Serializable, NetSerializable]
public enum FlagMode
{
    Item,
    Off,
    On,
}

[Serializable, NetSerializable]
public enum FlagLayers
{
    Layer,
}
