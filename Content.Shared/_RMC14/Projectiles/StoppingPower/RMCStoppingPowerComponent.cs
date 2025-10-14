using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Projectiles.StoppingPower;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCStoppingPowerComponent : Component
{
    /// <summary>
    ///     The current stopping power.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentStoppingPower;

    /// <summary>
    ///     The maximum stopping power value possible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxStoppingPower = 5;

    /// <summary>
    ///     The minimum amount of stopping power required to apply any effects.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int StoppingThreshold = 2;

    /// <summary>
    ///     The minimum amount of stopping power required to apply screen shake to big xenos.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BigXenoScreenShakeThreshold = 3;

    /// <summary>
    ///     The minimum amount of stopping power required to apply a knockdown to big xenos.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BigXenoInterruptThreshold = 4;

    /// <summary>
    ///     The amount to divide the damage of the projectile by to calculate the stopping power.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double StoppingPowerDivider = 30;

    /// <summary>
    ///     The amount to multiply the stopping power by to determine the stun duration of normal sized xenos.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float XenoStunMultiplier = 0.3f;

    /// <summary>
    ///     How long to stun big xenos for if stopping power is high enough.
    ///     0.7 is the minimum needed to apply a stun any lower and no stun happens at all. In practice it's a micro stun.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BigXenoStunTime = TimeSpan.FromSeconds(0.7);

    /// <summary>
    ///     Where the shot originated from
    /// </summary>
    [DataField, AutoNetworkedField]
    public MapCoordinates? ShotFrom;

    /// <summary>
    ///     If updating stopping power requires aimed shot
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresAimedShot = true;

    [DataField, AutoNetworkedField]
    public int FocusedCounter = 0;

    /// <summary>
    ///     If stopping power requires focused to be above a certain thereshold to activate.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? FocusedCounterThreshold = 2;
}
