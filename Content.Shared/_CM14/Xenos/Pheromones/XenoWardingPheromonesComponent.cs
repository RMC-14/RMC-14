using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Xenos.Pheromones;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoPheromonesSystem))]
public sealed partial class XenoWardingPheromonesComponent : Component
{
    [DataField]
    public SpriteSpecifier Icon = new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_pheromones_hud.rsi"), "warding");

    [DataField]
    public FixedPoint2 Multiplier;
}
