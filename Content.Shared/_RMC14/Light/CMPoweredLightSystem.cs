using Content.Shared._RMC14.Xenonids;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Light;

public sealed class CMPoweredLightSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LightBurnHandAttemptEvent>(OnLightBurnHandAttempt);

        SubscribeLocalEvent<PreventAttackLightOffComponent, GettingAttackedAttemptEvent>(OnPreventAttackLightOffAttackedAttempt);
    }

    private void OnLightBurnHandAttempt(ref LightBurnHandAttemptEvent ev)
    {
        ev.Cancelled = true;
        if (!HasComp<XenoComponent>(ev.User))
            _popup.PopupClient(Loc.GetString("cm-light-failed"), ev.Light, ev.User);
    }

    private void OnPreventAttackLightOffAttackedAttempt(Entity<PreventAttackLightOffComponent> ent, ref GettingAttackedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_pointLight.TryGetLight(ent, out var pointLight) ||
            !pointLight.Enabled)
        {
            args.Cancelled = true;
        }
    }
}
