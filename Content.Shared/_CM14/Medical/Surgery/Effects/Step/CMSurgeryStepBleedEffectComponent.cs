using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class CMSurgeryStepBleedEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Amount;
}
