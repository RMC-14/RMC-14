using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.BulletBox;

public sealed class BulletBoxSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BulletBoxComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BulletBoxComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BulletBoxComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<BulletBoxComponent, BulletBoxTransferDoAfterEvent>(OnTransferDoAfter);
    }

    private void OnMapInit(Entity<BulletBoxComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnExamined(Entity<BulletBoxComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(BulletBoxComponent)))
        {
            args.PushText(Loc.GetString("rmc-bullet-box-amount", ("amount", ent.Comp.Amount)));
        }
    }

    private void OnInteractUsing(Entity<BulletBoxComponent> ent, ref InteractUsingEvent args)
    {
        var used = new Entity<RefillableByBulletBoxComponent?, BallisticAmmoProviderComponent?>(args.Used, null, null);
        if (!Resolve(used, ref used.Comp1, ref used.Comp2, false))
            return;

        args.Handled = true;
        if (!CanTransferPopup(ent, args.User, ref used))
            return;

        var ev = new BulletBoxTransferDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, ev, ent, ent, args.Used)
        {
            BreakOnMove = true,
            BreakOnDropItem = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnTransferDoAfter(Entity<BulletBoxComponent> ent, ref BulletBoxTransferDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } usedId)
            return;

        args.Handled = true;

        var user = args.User;
        var used = new Entity<RefillableByBulletBoxComponent?, BallisticAmmoProviderComponent?>(usedId, null, null);
        if (!CanTransferPopup(ent, user, ref used) || used.Comp2 == null)
            return;

        var transfer = used.Comp2.Capacity - used.Comp2.Count;
        if (transfer <= 0)
            return;

        transfer = Math.Min(transfer, ent.Comp.Amount);
        _gun.SetBallisticUnspawned((used, used.Comp2), used.Comp2.UnspawnedCount + transfer);
        ent.Comp.Amount -= transfer;
        Dirty(ent);

        _popup.PopupClient(Loc.GetString("rmc-bullet-box-transfer-done", ("amount", transfer), ("used", used)), user);
        UpdateAppearance(ent);
    }

    private bool CanTransferPopup(Entity<BulletBoxComponent> box, EntityUid user, ref Entity<RefillableByBulletBoxComponent?, BallisticAmmoProviderComponent?> used)
    {
        if (!Resolve(used, ref used.Comp1, ref used.Comp2, false))
            return false;

        if (used.Comp2.Count >= used.Comp2.Capacity)
        {
            _popup.PopupClient(Loc.GetString("rmc-bullet-box-none-left"), box, user, PopupType.MediumCaution);
            return false;
        }

        if (box.Comp.BulletType != used.Comp1.BulletType)
        {
            _popup.PopupClient(Loc.GetString("rmc-bullet-box-wrong-rounds"), box, user, PopupType.MediumCaution);
            return false;
        }

        if (box.Comp.Amount <= 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-bullet-box-none-left"), box, user, PopupType.MediumCaution);
            return false;
        }

        return true;
    }

    private void UpdateAppearance(Entity<BulletBoxComponent> ent)
    {
        var visual = ((double) ent.Comp.Amount / ent.Comp.Max) switch
        {
            >= 1 => BulletBoxVisuals.Full,
            >= 0.66 => BulletBoxVisuals.High,
            >= 0.33 => BulletBoxVisuals.Medium,
            > 0 => BulletBoxVisuals.Low,
            _ => BulletBoxVisuals.Empty,
        };

        _appearance.SetData(ent, BulletBoxLayers.Fill, visual);
    }
}
