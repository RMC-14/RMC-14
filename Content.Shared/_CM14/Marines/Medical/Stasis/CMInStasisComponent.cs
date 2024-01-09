using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Marines.Medical.Stasis;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMStasisBagSystem))]
public sealed partial class CMInStasisComponent : Component
{
    [DataField, AutoNetworkedField]
    public float IncubationMultiplier = 0.333f;
}
