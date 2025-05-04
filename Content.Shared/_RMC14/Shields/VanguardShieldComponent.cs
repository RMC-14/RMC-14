using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Shields;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VanguardShieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 RegenAmount = FixedPoint2.New(800);

    [DataField, AutoNetworkedField]
    public int ExplosionResistance = 75;

    [DataField]
    public bool WasHit = false;

    [DataField]
    public TimeSpan NextDecay;

    [DataField]
    public TimeSpan DecayEvery = TimeSpan.FromSeconds(0.4);

    [DataField, AutoNetworkedField]
    public float DecayMult = 0.7f;

    [DataField, AutoNetworkedField]
    public float DecaySub = 50;

    [DataField]
    public TimeSpan LastTimeHit;

    [DataField]
    public TimeSpan RechargeTime = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan LastRecharge;

    [DataField]
    public TimeSpan BuffExtraTime = TimeSpan.FromSeconds(0.7);

    [DataField]
    public float DecayThreshold = 5;
}
