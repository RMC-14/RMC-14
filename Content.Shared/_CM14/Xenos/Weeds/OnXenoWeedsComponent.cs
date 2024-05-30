using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class OnXenoWeedsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool OnXenoWeeds;

    // The current passive speed modifier this entity is getting from weeds
    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1f;
}
