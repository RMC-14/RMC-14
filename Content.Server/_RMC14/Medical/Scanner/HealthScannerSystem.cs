using Content.Server.Body.Components;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Temperature.Components;
using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.Scanner;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Timing;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.Scanner;

public sealed class HealthScannerSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly CMHandsSystem _rmcHands = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HealthScannerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HealthScannerComponent, DoAfterAttemptEvent<HealthScannerDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<HealthScannerComponent, HealthScannerDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<HealthScannerComponent> scanner, ref AfterInteractEvent args)
    {
        if (!args.CanReach ||
            args.Target is not { } target ||
            !CanUseHealthScannerPopup(scanner, args.User, ref target))
        {
            return;
        }

        var delay = _skills.GetDelay(args.User, scanner);
        var ev = new HealthScannerDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, delay, ev, scanner, target, scanner)
        {
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };

        if (delay > TimeSpan.Zero)
        {
            var name = Loc.GetString("zzzz-the", ("ent", target));
            _popup.PopupEntity($"You start fumbling around with {name}...", target, args.User);
        }

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoAfterAttempt(Entity<HealthScannerComponent> ent, ref DoAfterAttemptEvent<HealthScannerDoAfterEvent> args)
    {
        var doAfter = args.DoAfter.Args;
        if (doAfter.Target is not { } target)
            return;

        if (!CanUseHealthScannerPopup(ent, doAfter.User, ref target))
        {
            args.Cancel();
            return;
        }

        var userCoords = Transform(doAfter.User).Coordinates;
        if (!_transform.InRange(userCoords, args.DoAfter.UserPosition, doAfter.MovementThreshold))
            args.Cancel();
    }

    private void OnDoAfter(Entity<HealthScannerComponent> scanner, ref HealthScannerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (TryComp(scanner, out UseDelayComponent? useDelay))
            _useDelay.TryResetDelay((scanner, useDelay));

        scanner.Comp.Target = target;
        Dirty(scanner);

        _audio.PlayPvs(scanner.Comp.Sound, scanner);
        _ui.OpenUi(scanner.Owner, HealthScannerUIKey.Key, args.User);

        UpdateUI(scanner);
    }

    /// <param name="scanner">The Health Scanner</param>
    /// <param name="user"> The entity using the Health Scanner</param>
    /// <param name="target">The entity being scanned by the Health Scanner. May be changed</param>
    /// <returns></returns>
    private bool CanUseHealthScannerPopup(Entity<HealthScannerComponent> scanner, EntityUid user, ref EntityUid target)
    {
        if (HasComp<HealthScannableContainerComponent>(target) && TryComp(target, out EntityStorageComponent? entityStorage))
        {
            foreach (var entity in entityStorage.Contents.ContainedEntities)
            {
                if (HasComp<DamageableComponent>(entity) &&
                HasComp<MobStateComponent>(entity) &&
                HasComp<MobThresholdsComponent>(entity))
                {
                    target = entity;
                    break;
                }
            }
        }

        if (!HasComp<DamageableComponent>(target) ||
            !HasComp<MobStateComponent>(target) ||
            !HasComp<MobThresholdsComponent>(target))
        {
            _popup.PopupEntity("You can't analyze that!", target, user);
            return false;
        }

        if (TryComp(scanner, out UseDelayComponent? useDelay) &&
            _useDelay.IsDelayed((scanner, useDelay)))
        {
            return false;
        }

        var ev = new HealthScannerAttemptTargetEvent();
        RaiseLocalEvent(target, ref ev);
        if (ev.Cancelled)
        {
            if (ev.Popup != null)
                _popup.PopupEntity(ev.Popup, target, user);

            return false;
        }

        return true;
    }

    private void UpdateUI(Entity<HealthScannerComponent> scanner)
    {
        if (scanner.Comp.Target is not { } target)
            return;

        if (TerminatingOrDeleted(target))
        {
            if (!TerminatingOrDeleted(scanner))
                _ui.CloseUi(scanner.Owner, HealthScannerUIKey.Key);

            scanner.Comp.Target = null;
            return;
        }

        if (!_rmcHands.TryGetHolder(scanner, out _))
            return;

        FixedPoint2 blood = 0;
        FixedPoint2 maxBlood = 0;
        Solution? chemicals = null;
        if (TryComp(target, out BloodstreamComponent? bloodstream))
        {
            if (_solution.TryGetSolution(target, bloodstream.BloodSolutionName, out _, out var bloodSolution))
            {
                blood = bloodSolution.Volume;
                maxBlood = bloodSolution.MaxVolume;
            }

            _solution.TryGetSolution(target, bloodstream.ChemicalSolutionName, out _, out chemicals);
        }

        var temperature = CompOrNull<TemperatureComponent>(target)?.CurrentTemperature;
        var bleeding = bloodstream is { BleedAmount: > 0 };
        var state = new HealthScannerBuiState(GetNetEntity(target), blood, maxBlood, temperature, chemicals, bleeding);

        _ui.SetUiState(scanner.Owner, HealthScannerUIKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var scanners = EntityQueryEnumerator<HealthScannerComponent>();
        while (scanners.MoveNext(out var uid, out var active))
        {
            if (time < active.UpdateAt)
                continue;

            active.UpdateAt = time + active.UpdateCooldown;
            UpdateUI((uid, active));
        }
    }
}
