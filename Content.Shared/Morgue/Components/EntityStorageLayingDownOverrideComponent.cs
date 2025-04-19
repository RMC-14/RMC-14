using Robust.Shared.GameStates;

namespace Content.Shared.Morgue.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityStorageLayingDownOverrideComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;
}
