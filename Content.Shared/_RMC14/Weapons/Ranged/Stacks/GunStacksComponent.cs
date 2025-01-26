using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Weapons.Ranged.Stacks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GunStacksSystem))]
public sealed partial class GunStacksComponent : Component
{
    [DataField, AutoNetworkedField]
    public int IncreaseAP = 10;

    [DataField, AutoNetworkedField]
    public int MaxAP = 50;

    [DataField, AutoNetworkedField]
    public FixedPoint2 DamageIncrease = FixedPoint2.New(0.2);

    [DataField, AutoNetworkedField]
    public float SetFireRate = 1.4285f;

    // TODO RMC14
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi[] Crosshairs = new SpriteSpecifier.Rsi[]
    {
        new(new ResPath("_RMC14/Interface/MousePointer/XM88/xm88-0.rsi"), "all"),
        new(new ResPath("_RMC14/Interface/MousePointer/XM88/xm88-1.rsi"), "all"),
        new(new ResPath("_RMC14/Interface/MousePointer/XM88/xm88-2.rsi"), "all"),
        new(new ResPath("_RMC14/Interface/MousePointer/XM88/xm88-3.rsi"), "all"),
        new(new ResPath("_RMC14/Interface/MousePointer/XM88/xm88-4.rsi"), "all"),
    };

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi[] FiredCrosshairs = new SpriteSpecifier.Rsi[]
    {
        new(new ResPath("_RMC14/Interface/MousePointer/XM88/xm88-fired-0.rsi"), "all"),
        new(new ResPath("_RMC14/Interface/MousePointer/XM88/xm88-fired-1.rsi"), "all"),
        new(new ResPath("_RMC14/Interface/MousePointer/XM88/xm88-fired-2.rsi"), "all"),
        new(new ResPath("_RMC14/Interface/MousePointer/XM88/xm88-fired-3.rsi"), "all"),
        new(new ResPath("_RMC14/Interface/MousePointer/XM88/xm88-fired-4.rsi"), "all"),
    };
}
