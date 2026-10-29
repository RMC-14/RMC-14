using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedRMCResearchComputerSystem))]
public sealed partial class RMCResearchComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextPrintAt;
}
