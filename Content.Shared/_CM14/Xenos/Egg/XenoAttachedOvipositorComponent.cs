using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Egg;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoAttachedOvipositorComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan? NextEgg;
}
