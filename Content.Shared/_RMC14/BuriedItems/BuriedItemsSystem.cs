using Content.Shared._RMC14.Entrenching;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.BuriedItems;

/// <summary>
/// Shared system for handling buried items interactions.
/// Manages digging up buried items with entrenching tools and controlling examine/storage access.
/// </summary>
public sealed class BuriedItemsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BuriedItemsComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<BuriedItemsComponent, ExamineAttemptEvent>(OnExamineAttempt);
        SubscribeLocalEvent<BuriedItemsComponent, StorageInteractAttemptEvent>(OnStorageOpenAttempt);
    }

    private void OnExamineAttempt(Entity<BuriedItemsComponent> buried, ref ExamineAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // Only entities with SeeBuriedItemsComponent (admin ghosts) can examine buried items
        if (HasComp<SeeBuriedItemsComponent>(args.Examiner))
            return;

        args.Cancel();
    }

    private void OnInteractUsing(Entity<BuriedItemsComponent> buried, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<EntrenchingToolComponent>(args.Used))
            return;

        args.Handled = true;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, buried.Comp.DigDelay, new BuriedItemsDigDoAfterEvent(), buried, target: buried, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        _audio.PlayPredicted(buried.Comp.DigSound, buried, args.User);
        _popup.PopupPredicted(
            Loc.GetString("rmc-buried-items-start-digging-self"),
            Loc.GetString("rmc-buried-items-start-digging-others", ("user", args.User)),
            args.User,
            args.User);
    }

    private void OnStorageOpenAttempt(Entity<BuriedItemsComponent> buried, ref StorageInteractAttemptEvent args)
    {
        // Allow ghosts to open and inspect contents
        if (HasComp<SeeBuriedItemsComponent>(args.User))
            return;

        // Block everyone else from opening storage UI
        args.Cancelled = true;
    }
}
