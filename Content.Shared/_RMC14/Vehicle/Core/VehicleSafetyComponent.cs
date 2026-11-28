using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(VehicleSafetySystem))]
public sealed partial class VehicleSafetyComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField]
    public SpriteSpecifier? Icon;

    [DataField]
    public SpriteSpecifier? Overlay = new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Interface/vehicle_hud.rsi"), "outline");
}
