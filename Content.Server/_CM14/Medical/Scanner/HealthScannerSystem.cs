using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Temperature.Components;
using Content.Shared._CM14.Marines.Skills;
using Content.Shared._CM14.Medical.Scanner;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Timing;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._CM14.Medical.Scanner;

public sealed class HealthScannerSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solution = default!;
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
        if (args.Target is not { } target ||
            !CanUseHealthScannerPopup(scanner, args.User, target))
        {
            return;
        }

        var delay = _skills.GetDelay(args.User, scanner);
        var ev = new HealthScannerDoAfterEvent(GetNetEntity(target));
        var doAfter = new DoAfterArgs(EntityManager, args.User, delay, ev, scanner, null, scanner)
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
        if (!CanUseHealthScannerPopup(ent, doAfter.User, GetEntity(args.Event.Scanned)))
        {
            args.Cancel();
            return;
        }

        if (!Transform(doAfter.User).Coordinates.InRange(EntityManager, _transform, args.DoAfter.UserPosition, doAfter.MovementThreshold))
            args.Cancel();
    }

    private void OnDoAfter(Entity<HealthScannerComponent> scanner, ref HealthScannerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (TryComp(scanner, out UseDelayComponent? useDelay))
            _useDelay.TryResetDelay((scanner, useDelay));

        scanner.Comp.Target = GetEntity(args.Scanned);

        _audio.PlayPvs(scanner.Comp.Sound, scanner);
        _ui.OpenUi(scanner.Owner, HealthScannerUIKey.Key, args.User);

        UpdateUI(scanner);
    }

    private bool CanUseHealthScannerPopup(Entity<HealthScannerComponent> scanner, EntityUid user, EntityUid target)
    {
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

        var blood = _bloodstream.GetBloodLevelPercentage(target) * 100;
        var temperature = CompOrNull<TemperatureComponent>(target)?.CurrentTemperature;

        Solution? chemicals = null;
        if (TryComp(target, out BloodstreamComponent? bloodstream))
        {
            _solution.TryGetSolution(target, bloodstream.ChemicalSolutionName, out _, out chemicals);
        }

        _ui.SetUiState(scanner.Owner, HealthScannerUIKey.Key, new HealthScannerBuiState(GetNetEntity(target), blood, temperature, chemicals));
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
