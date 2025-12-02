using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Damage;

public sealed class RMCDamagePopupSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamagePopupComponent, DamageDealtEvent>(OnDamagePopup);
    }

    private void OnDamagePopup(Entity<DamagePopupComponent> ent, ref DamageDealtEvent args)
    {
        if (!TryComp(ent, out DamageableComponent? damageable))
            return;

        ShowClientDamagePopup(ent, damageable.TotalDamage, ent.Comp.Type, args.Origin, args.DamageDelta);
    }

    private void ShowClientDamagePopup(EntityUid target, FixedPoint2 damageTotal, DamagePopupType type, EntityUid? origin, DamageSpecifier? damageDelta)
    {
        if (damageDelta == null || _net.IsServer)
            return;

        var delta = damageDelta.GetTotal();
        var msg = type switch
        {
            DamagePopupType.Delta => delta.ToString(),
            DamagePopupType.Total => damageTotal.ToString(),
            DamagePopupType.Combined => delta + " | " + damageTotal,
            DamagePopupType.Hit => "!",
            _ => "Invalid type",
        };
        _popupSystem.PopupPredicted(msg, target, origin);
    }
}
