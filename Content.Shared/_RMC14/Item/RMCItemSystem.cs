using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.Item;

namespace Content.Shared._RMC14.Item;

public sealed class RMCItemSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ItemComponent, CheckMeleeAttackerEvent>(OnReceivingMeleeAttackAttempt);
    }

    private void OnReceivingMeleeAttackAttempt(Entity<ItemComponent> item, ref CheckMeleeAttackerEvent args)
    {
        // Prevent random items on the ground from getting in the way of disarms/tackles, such as potted plants.
        // TODO RMC14 this is a sort of ham-fisted way of preventing garbage/props from getting in the way of things.
        if (args.Disarm)
            args.Defer = true;
    }
}
