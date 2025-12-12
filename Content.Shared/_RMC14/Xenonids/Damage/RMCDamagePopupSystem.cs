using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Xenonids.Damage;

public sealed class RMCDamagePopupSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamagePopupComponent, ProjectileDamageDealtEvent>(OnDamagePopup);
    }

    private void OnDamagePopup(Entity<DamagePopupComponent> ent, ref ProjectileDamageDealtEvent args)
    {
        if (!TryComp(ent, out DamageableComponent? damageable))
            return;

        ShowClientDamagePopup(ent, damageable.TotalDamage, ent.Comp.Type, args.Origin, args.DamageDelta);
    }

    private void ShowClientDamagePopup(EntityUid target, FixedPoint2 damageTotal, DamagePopupType type, EntityUid? origin, DamageSpecifier? damageDelta)
    {
        if (damageDelta == null)
            return;

        var delta = damageDelta.GetTotal();
        var total = damageTotal + damageDelta.GetTotal(); // The total does not include the damage dealt yet on the client side.
        var msg = type switch
        {
            DamagePopupType.Delta => delta.ToString(),
            DamagePopupType.Total => total.ToString(),
            DamagePopupType.Combined => delta + " | " + total,
            DamagePopupType.Hit => "!",
            _ => "Invalid type",
        };
        _popupSystem.PopupClient(msg, target, origin);
    }
}
