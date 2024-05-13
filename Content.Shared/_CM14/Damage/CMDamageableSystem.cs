using Content.Shared._CM14.Armor;
using Content.Shared._CM14.Marines.Orders;
using Content.Shared._CM14.Xenos.Pheromones;
using Content.Shared.Armor;
using Content.Shared.Blocking;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Silicons.Borgs;

namespace Content.Shared._CM14.Damage;

public sealed class CMDamageableSystem : EntitySystem
{
    private EntityQuery<DamageableComponent> _damageableQuery;

    public override void Initialize()
    {
        _damageableQuery = GetEntityQuery<DamageableComponent>();

        SubscribeLocalEvent<MaxDamageComponent, BeforeDamageChangedEvent>(OnMaxBeforeDamageChanged);
        SubscribeLocalEvent<MaxDamageComponent, DamageModifyEvent>(OnMaxDamageModify,
            after:
            [
                typeof(SharedArmorSystem), typeof(BlockingSystem), typeof(InventorySystem), typeof(SharedBorgSystem),
                typeof(SharedMarineOrdersSystem), typeof(CMArmorSystem), typeof(SharedXenoPheromonesSystem)
            ]);
    }

    private void OnMaxBeforeDamageChanged(Entity<MaxDamageComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled ||
            !_damageableQuery.TryComp(ent, out var damageable))
        {
            return;
        }

        if (damageable.TotalDamage >= ent.Comp.Max && args.Damage.GetTotal() > FixedPoint2.Zero)
            args.Cancelled = true;
    }

    private void OnMaxDamageModify(Entity<MaxDamageComponent> ent, ref DamageModifyEvent args)
    {
        if (!_damageableQuery.TryComp(ent, out var damageable))
            return;

        var modifyTotal = args.Damage.GetTotal();
        if (modifyTotal <= FixedPoint2.Zero || damageable.TotalDamage + modifyTotal <= ent.Comp.Max)
            return;

        var remaining = ent.Comp.Max - damageable.TotalDamage;
        if (ent.Comp.Max <= FixedPoint2.Zero)
        {
            args.Damage *= 0;
            return;
        }

        if (modifyTotal <= remaining)
            return;

        args.Damage *= remaining.Float() / modifyTotal.Float();
    }
}
