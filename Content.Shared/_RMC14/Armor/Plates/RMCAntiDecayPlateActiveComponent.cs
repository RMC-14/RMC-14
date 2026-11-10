using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor.Plates;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCArmorPlateSystem))]
public sealed partial class RMCAntiDecayPlateActiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan BonusTime = TimeSpan.FromMinutes(4);

    [DataField, AutoNetworkedField]
    public EntityUid? Plate;

    [DataField, AutoNetworkedField]
    public bool Preserving;
}
