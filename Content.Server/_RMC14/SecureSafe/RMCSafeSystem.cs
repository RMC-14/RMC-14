using System.Numerics;
using Content.Shared._RMC14.SecureSafe;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.UserInterface;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Server.Station.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RMC14.SecureSafe;

public sealed class RMCSafeSystem : SharedRMCSafeSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSafeComponent, MapInitEvent>(OnMapInit);

        // Cancel the ActivatableUI from opening when the safe is UNLOCKED
        // (normal storage interaction handles it when unlocked)
        SubscribeLocalEvent<RMCSafeComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);

        // Prevent normal lock toggling — only the safe cracking UI should unlock
        SubscribeLocalEvent<RMCSafeComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);

        Subs.BuiEvents<RMCSafeComponent>(RMCSafeUIKey.Key, subs =>
        {
            subs.Event<RMCSafeChangeDialMessage>(OnBuiChangeDial);
            subs.Event<RMCSafeTryOpenMessage>(OnBuiTryOpen);
        });
    }

    private void OnMapInit(Entity<RMCSafeComponent> ent, ref MapInitEvent args)
    {
        var random = new Random();
        ent.Comp.Code1 = random.Next(0, 11) * 5; // 0, 5, ..., 50
        ent.Comp.Code2 = random.Next(0, 11) * 5;

        ent.Comp.Dial1 = 0;
        ent.Comp.Dial2 = 0;
        Dirty(ent);

        // Locked and CLOSED on spawn.
        // Closing it now prevents storagesystem from sucking in the paper later.
        _entityStorage.CloseStorage(ent);

        if (TryComp<LockComponent>(ent, out var lockComp))
            _lock.Lock(ent, null, lockComp);

        // Spawn combination paper if configured
        if (ent.Comp.AutoPrintPaperPrototype != null)
        {
            var coords = _transform.GetMoverCoordinates(ent).Offset(new Vector2(0, -1));
            var paper = Spawn(ent.Comp.AutoPrintPaperPrototype, coords);

            _metaData.SetEntityName(paper, ent.Comp.AutoPrintPaperName);
            _metaData.SetEntityDescription(paper, ent.Comp.AutoPrintPaperDesc);

            var text = string.Format(ent.Comp.AutoPrintFormat, ent.Comp.Code1, ent.Comp.Code2);
            _paper.SetContent(paper, text);

            // Ensure it's outside the safe
            _transform.PlaceNextTo(paper, ent.Owner);
        }
    }

    private void OnUIOpenAttempt(Entity<RMCSafeComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // If it's unlocked, cancel the safe cracking UI—let storage take over
        if (TryComp<LockComponent>(ent, out var lockComp) && !lockComp.Locked)
        {
            args.Cancel();
            return;
        }

        // If locked, let the UI open—update state when it does
        UpdateBUI(ent);
    }

    private void OnLockToggleAttempt(Entity<RMCSafeComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (!TryComp<LockComponent>(ent, out var lockComp))
            return;

        // Block all normal unlock attempts
        if (lockComp.Locked)
            args.Cancelled = true;
    }

    private void OnBuiChangeDial(Entity<RMCSafeComponent> ent, ref RMCSafeChangeDialMessage args)
    {
        if (args.DialNumber == 1)
        {
            ent.Comp.Dial1 += args.Amount;
            if (ent.Comp.Dial1 > 50) ent.Comp.Dial1 = 0;
            if (ent.Comp.Dial1 < 0) ent.Comp.Dial1 = 50;
        }
        else if (args.DialNumber == 2)
        {
            ent.Comp.Dial2 += args.Amount;
            if (ent.Comp.Dial2 > 50) ent.Comp.Dial2 = 0;
            if (ent.Comp.Dial2 < 0) ent.Comp.Dial2 = 50;
        }

        Dirty(ent);
        UpdateBUI(ent);
    }

    private void OnBuiTryOpen(Entity<RMCSafeComponent> ent, ref RMCSafeTryOpenMessage args)
    {
        if (ent.Comp.Dial1 == ent.Comp.Code1 && ent.Comp.Dial2 == ent.Comp.Code2)
        {
            if (TryComp<LockComponent>(ent, out var lockComp))
            {
                _popup.PopupEntity(Loc.GetString("rmc-safe-unlock-success"), ent, args.Actor);
                _audio.PlayPvs(ent.Comp.SoundSuccess, ent);
                _lock.Unlock(ent, args.Actor, lockComp);
                _ui.CloseUi(ent.Owner, RMCSafeUIKey.Key);
            }
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("rmc-safe-unlock-fail"), ent, args.Actor);
            _audio.PlayPvs(ent.Comp.SoundFail, ent);
        }
    }

    private void UpdateBUI(Entity<RMCSafeComponent> ent)
    {
        _ui.SetUiState(ent.Owner, RMCSafeUIKey.Key, new RMCSafeBuiState(ent.Comp.Dial1, ent.Comp.Dial2));
    }
}
