using Content.Shared._CM14.Comtech.Barbed;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._CM14.Barbed;

public sealed class BarbedSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BarbedComponent, AttackedEvent>(OnAttacked);
    }
    private void OnAttacked(EntityUid uid, BarbedComponent component, AttackedEvent args)
    {
        _damageableSystem.TryChangeDamage(args.User, component.ThornsDamage);
        return;
    }
}
