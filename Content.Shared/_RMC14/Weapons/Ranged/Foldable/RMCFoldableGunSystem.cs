using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared._RMC14.Weapons.Common;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Robust.Shared.Audio.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;

namespace Content.Shared._RMC14.Weapons.Ranged.Foldable;

public sealed class RMCFoldableGunSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCFoldableGunComponent, ExaminedEvent>(OnExamined, before: [typeof(SharedGunSystem)]);
        SubscribeLocalEvent<RMCFoldableGunComponent, GunShotEvent>(OnGunShoot);
        SubscribeLocalEvent<RMCFoldableGunComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<RMCFoldableGunComponent, UniqueActionEvent>(OnUniqueAction);
        SubscribeLocalEvent<RMCFoldableGunComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<RMCFoldableGunComponent, RMCFoldableGunDoAfterEvent>(OnFoldableGunDoAfter);
    }

    private void OnExamined(Entity<RMCFoldableGunComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.ExamineText), 1);
    }

    private void OnGunShoot(Entity<RMCFoldableGunComponent> ent, ref GunShotEvent args)
    {
        ent.Comp.Fired = true;
    }

    private void OnAttemptShoot(Entity<RMCFoldableGunComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Fired)
            args.Cancelled = true;
    }

    private void OnUniqueAction(Entity<RMCFoldableGunComponent> ent, ref UniqueActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = AttemptFold(ent, args.UserUid);
    }

    private void OnActivate(Entity<RMCFoldableGunComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !ent.Comp.OnActivate)
            return;

        var user = args.User;

        if (!_hands.IsHolding(user, ent.Owner))
            return;

        args.Handled = AttemptFold(ent, user);
    }

    private void OnFoldableGunDoAfter(Entity<RMCFoldableGunComponent> ent, ref RMCFoldableGunDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var user = args.User;

        if (args.Handled)
            return;

        var selfText = Loc.GetString(ent.Comp.FinishText, ("weapon", ent));
        var othersText = Loc.GetString(ent.Comp.FinishTextOthers, ("user", user), ("weapon", ent));

        if (_hands.GetActiveHand(user) is not { } handToUse)
            return;

        var newEntity = PredictedSpawnNextToOrDrop(ent.Comp.FoldedEntity, user);
        _hands.TryForcePickup(newEntity, user, handToUse, checkActionBlocker: false);

        _popup.PopupPredicted(selfText, othersText, user, user);
        _audio.PlayPredicted(ent.Comp.ToggleFoldSound, user, user);

        PredictedQueueDel(ent.Owner);

        args.Handled = true;
    }

    public bool AttemptFold(Entity<RMCFoldableGunComponent> ent, EntityUid user)
    {
        if (ent.Comp.Fired)
        {
            var popupText = Loc.GetString("rmc-gun-foldable-launcher-fold-already-fired-attempt", ("weapon", ent));
            _popup.PopupClient(popupText, user, user, PopupType.SmallCaution);
            return false;
        }

        var ev = new RMCFoldableGunDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.FoldDelay, ev, ent, ent)
        {
            BreakOnMove = true,
            BreakOnDamage = false,
            MovementThreshold = 0.5f,
            DuplicateCondition = DuplicateConditions.SameEvent,
            CancelDuplicate = true,
            NeedHand = true,
            BreakOnDropItem = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            var selfText = Loc.GetString(ent.Comp.FoldText, ("weapon", ent));
            var othersText = Loc.GetString(ent.Comp.FoldTextOthers, ("user", user), ("weapon", ent));

            _popup.PopupPredicted(selfText, othersText, user, user);
            return true;
        }

        return false;
    }
}

[Serializable, NetSerializable]
public sealed partial class RMCFoldableGunDoAfterEvent : SimpleDoAfterEvent;
