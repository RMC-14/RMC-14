using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Targeting.Focused;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCBeingFocusedComponent : Component
{
    /// <summary>
    ///     The entities focusing the entity with this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> FocusedBy = new();

    [DataField]
    public ResPath RsiPath = new("/Textures/_RMC14/Interface/xeno_hud.rsi");
    [DataField]
    public string FocusedState = "hudeye";
}
