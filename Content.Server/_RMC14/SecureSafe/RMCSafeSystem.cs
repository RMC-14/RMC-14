using Content.Shared._RMC14.SecureSafe;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.UserInterface;
using Content.Shared.Paper;
using Content.Shared.Roles.Jobs;
using Content.Shared.Hands.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server._RMC14.SecureSafe;

public sealed class RMCSafeSystem : SharedRMCSafeSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSafeComponent, MapInitEvent>(OnMapInit);

        // Cancel the ActivatableUI from opening when the safe is UNLOCKED
        // (normal storage interaction handles it when unlocked)
        SubscribeLocalEvent<RMCSafeComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);

        // Prevent normal lock toggling — only the safe cracking UI should unlock
        SubscribeLocalEvent<RMCSafeComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

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

        // Ensure locked on spawn
        if (TryComp<LockComponent>(ent, out var lockComp))
            _lock.Lock(ent, null, lockComp);

        // Give to existing players if any match
        if (!string.IsNullOrEmpty(ent.Comp.AutoPrintToJob))
        {
            var station = _station.GetOwningStation(ent);
            if (station != null)
            {
                foreach (var session in _playerManager.Sessions)
                {
                    if (session.AttachedEntity is not { Valid: true } mob)
                        continue;

                    if (_station.GetOwningStation(mob) != station)
                        continue;

                    if (!_jobs.MindHasJobWithId(_mind.GetMind(mob), ent.Comp.AutoPrintToJob))
                        continue;

                    GivePaperToPlayer(mob, ent.Comp);
                }
            }
        }
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (string.IsNullOrEmpty(ev.JobId))
            return;

        var station = ev.Station;
        var query = EntityQueryEnumerator<RMCSafeComponent>();
        while (query.MoveNext(out var uid, out var safe))
        {
            if (safe.AutoPrintToJob != ev.JobId)
                continue;

            if (_station.GetOwningStation(uid) != station)
                continue;

            GivePaperToPlayer(ev.Mob, safe);
        }
    }

    private void GivePaperToPlayer(EntityUid player, RMCSafeComponent safe)
    {
        if (safe.AutoPrintPaperPrototype == null)
            return;

        var paper = Spawn(safe.AutoPrintPaperPrototype, Transform(player).Coordinates);

        _metaData.SetEntityName(paper, safe.AutoPrintPaperName);
        _metaData.SetEntityDescription(paper, safe.AutoPrintPaperDesc);

        var text = string.Format(safe.AutoPrintFormat, safe.Code1, safe.Code2);
        _paper.SetContent(paper, text);

        _hands.PickupOrDrop(player, paper);
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
                _lock.Unlock(ent, args.Actor, lockComp);
                _ui.CloseUi(ent.Owner, RMCSafeUIKey.Key);
            }
        }
    }

    private void UpdateBUI(Entity<RMCSafeComponent> ent)
    {
        _ui.SetUiState(ent.Owner, RMCSafeUIKey.Key, new RMCSafeBuiState(ent.Comp.Dial1, ent.Comp.Dial2));
    }
}
