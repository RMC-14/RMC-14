using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.ElectronicSystem;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
public sealed partial class DropshipTargetingSystemComponent : Component
{
    [DataField, AutoNetworkedField]
    public int SpreadModifier = -2;

    [DataField, AutoNetworkedField]
    public int BulletSpreadModifier = -3;

    [DataField, AutoNetworkedField]
    public TimeSpan TravelingTimeModifier = TimeSpan.FromSeconds(-2);
}
