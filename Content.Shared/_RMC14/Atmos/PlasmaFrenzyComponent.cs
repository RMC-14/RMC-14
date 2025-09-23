using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlasmaFrenzyComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> RoarEmote = "XenoRoar";
}
