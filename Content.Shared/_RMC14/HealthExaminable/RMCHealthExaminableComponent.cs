using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.HealthExaminable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCHealthExaminableSystem))]
public sealed partial class RMCHealthExaminableComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<DamageGroupPrototype>> Groups = new() { "Brute", "Burn", "Airloss" };
}
