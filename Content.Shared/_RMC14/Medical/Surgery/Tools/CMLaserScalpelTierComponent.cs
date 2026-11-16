using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Surgery.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class CMLaserScalpelTierComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Tier = 1;
}
