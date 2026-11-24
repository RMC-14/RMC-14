using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerVisualComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? CatalyzeVisual;
}
