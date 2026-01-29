using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._RMC14.UniformAccessories;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UniformAccessoryComponent : Component
{
    [DataField, AutoNetworkedField]
    public Rsi? PlayerSprite;

    [DataField, AutoNetworkedField]
    public NetEntity? User;

    [DataField, AutoNetworkedField]
    public string Category;

    [DataField, AutoNetworkedField]
    public int Limit;

    [DataField, AutoNetworkedField]
    public bool Hidden = false;

    [DataField, AutoNetworkedField]
    public bool HiddenByJacketRolling = false;

    [DataField, AutoNetworkedField]
    public List<string>? LayerKeys;

    [DataField, AutoNetworkedField]
    public bool HasIconSprite = false;
}
