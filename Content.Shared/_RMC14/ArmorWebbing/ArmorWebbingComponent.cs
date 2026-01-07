using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._RMC14.ArmorWebbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedArmorWebbingSystem))]
public sealed partial class ArmorWebbingComponent : Component
{
    [DataField, AutoNetworkedField]
    public Rsi? PlayerSprite;

    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
