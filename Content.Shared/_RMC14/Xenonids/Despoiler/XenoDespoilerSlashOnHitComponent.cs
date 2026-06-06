using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerSlashOnHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public int EnhanceStacksThreshold = 2;
}
