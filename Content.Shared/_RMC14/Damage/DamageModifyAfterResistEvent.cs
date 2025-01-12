using Content.Shared.Damage;
using Content.Shared.Inventory;

namespace Content.Shared._RMC14.Damage;

/// <summary>
/// Mostly the same as DamageModify, but always runs after the original, even if the original doesn't run at all due to ignoring resistances.
/// For things like shields that need to take some of the damage no matter what.
/// </summary>

public sealed class DamageModifyAfterResistEvent : EntityEventArgs, IInventoryRelayEvent
{
    // Whenever locational damage is a thing, this should just check only that bit of armour.
    public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

    public readonly DamageSpecifier OriginalDamage;
    public DamageSpecifier Damage;
    public EntityUid? Origin;
    public EntityUid? Tool;

    public DamageModifyAfterResistEvent(DamageSpecifier damage, EntityUid? origin = null, EntityUid? tool = null)
    {
        OriginalDamage = damage;
        Damage = damage;
        Origin = origin;
        Tool = tool;
    }
}
