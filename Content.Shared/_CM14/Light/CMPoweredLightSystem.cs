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
            _popup.PopupEntity("You try to remove the light tube, but it's too hot and you don't want to burn your hand.", ev.Light, ev.User);
    }
}
