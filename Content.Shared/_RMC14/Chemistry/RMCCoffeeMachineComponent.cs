using System;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCCoffeeMachineComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField, AutoNetworkedField]
    public FixedPoint2 DispenseAmount;

    [DataField, AutoNetworkedField]
    public string SlotId = "coffee_machine_slot";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DispenseSound;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan NextDispenseTime = TimeSpan.Zero;
}
