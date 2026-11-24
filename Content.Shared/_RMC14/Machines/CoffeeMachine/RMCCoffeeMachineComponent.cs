using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Machines.CoffeeMachine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCCoffeeMachineComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Reagent = "RMCCoffee";

    [DataField, AutoNetworkedField]
    public FixedPoint2 DispenseAmount = 30;

    [DataField, AutoNetworkedField]
    public string SlotId = "coffee_machine_slot";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DispenseSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/coffee1.ogg");

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan NextDispenseTime = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public TimeSpan BrewTime = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public bool IsBrewing;

    [DataField, AutoNetworkedField]
    public TimeSpan BrewFinishTime;

    [DataField, AutoNetworkedField]
    public bool Spilled;

    [DataField, AutoNetworkedField]
    public EntityUid? LastUser;

    [DataField, AutoNetworkedField]
    public string MugOverlay = "coffee_cup_generic";
}

[Serializable, NetSerializable]
public enum RMCCoffeeMachineVisuals
{
    HasCup,
    IsBrewing,
}
