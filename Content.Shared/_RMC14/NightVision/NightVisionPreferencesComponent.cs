using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._RMC14.NightVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NightVisionPreferencesComponent : Component
{
    [DataField("color"), AutoNetworkedField]
    public NightVisionColor Color = NightVisionColor.Green;
}
