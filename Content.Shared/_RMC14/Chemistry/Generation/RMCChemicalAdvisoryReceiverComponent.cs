using Content.Shared.Inventory;

namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent]
public sealed partial class RMCChemicalAdvisoryReceiverComponent : Component
{
    [DataField]
    public SlotFlags Slots = SlotFlags.EYES;
}
