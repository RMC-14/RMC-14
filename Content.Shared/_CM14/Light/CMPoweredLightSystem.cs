using Content.Shared._CM14.Xenos;
using Content.Shared.Popups;

namespace Content.Shared._CM14.Light;

public sealed class CMPoweredLightSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LightBurnHandAttemptEvent>(OnLightBurnHandAttempt);
    }

    private void OnLightBurnHandAttempt(ref LightBurnHandAttemptEvent ev)
    {
        ev.Cancelled = true;
        if (!HasComp<XenoComponent>(ev.User))
            _popup.PopupEntity(Loc.GetString("cm-light-failed"), ev.Light, ev.User);
    }
}
