using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Popups;
using Content.Server.Temperature.Components;
using Content.Shared._CM14.Medical.Scanner;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._CM14.Medical.Scanner;

public sealed class HealthScannerSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solution = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HealthScannerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<HealthScannerComponent> scanner, ref AfterInteractEvent args)
    {
        var target = args.Target;
        if (!args.CanReach ||
            !HasComp<DamageableComponent>(target) ||
            !HasComp<MobStateComponent>(target) ||
            !HasComp<MobThresholdsComponent>(target) ||
            !TryComp(args.User, out ActorComponent? actor))
        {
            return;
        }

        if (_net.IsClient)
            return;

        var ev = new HealthScannerAttemptTargetEvent();
        RaiseLocalEvent(target.Value, ref ev);
        if (ev.Cancelled)
        {
            if (ev.Popup != null)
                _popup.PopupEntity(ev.Popup, target.Value, args.User);

            return;
        }

        _audio.PlayPvs(scanner.Comp.Sound, scanner);
        _ui.TryOpen(scanner, HealthScannerUIKey.Key, actor.PlayerSession);

        var blood = _bloodstream.GetBloodLevelPercentage(target.Value) * 100;
        var temperature = CompOrNull<TemperatureComponent>(target)?.CurrentTemperature;

        Solution? chemicals = null;
        if (TryComp(target.Value, out BloodstreamComponent? bloodstream))
        {
            _solution.TryGetSolution(target.Value, bloodstream.ChemicalSolutionName, out _, out chemicals);
        }

        _ui.TrySetUiState(scanner, HealthScannerUIKey.Key, new HealthScannerBuiState(GetNetEntity(target.Value), blood, temperature, chemicals));
    }
}
