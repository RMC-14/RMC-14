using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.Unrevivable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(RMCUnrevivableSystem))]
public sealed partial class RMCRevivableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan UnrevivableDelay = TimeSpan.FromMinutes(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan UnrevivableAt = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public LocId UnrevivableReasonMessage = "rmc-defibrillator-unrevivable";
}
