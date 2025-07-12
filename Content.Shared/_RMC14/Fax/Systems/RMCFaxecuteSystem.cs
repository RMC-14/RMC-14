using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Fax.Components;
using Content.Shared._RMC14.Fax.Components;
using Content.Shared._RMC14.Fax;

namespace Content.Shared._RMC14.Fax.Systems;

/// <summary>
/// RMC-specific system for handling execution of a mob within fax when copy or send attempt is made.
/// This is RMC-specific functionality that extends the base fax system.
/// </summary>
public sealed class RMCFaxecuteSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void Faxecute(EntityUid uid, FaxMachineComponent component, RMCDamageOnFaxecuteEvent? args = null)
    {
        var sendEntity = component.PaperSlot.Item;
        if (sendEntity == null)
            return;

        if (!TryComp<RMCFaxecuteComponent>(uid, out var faxecute))
            return;

        var damageSpec = faxecute.Damage;
        _damageable.TryChangeDamage(sendEntity, damageSpec);
        _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-error", ("target", uid)), uid, PopupType.LargeCaution);
        return;
    }
}
