using Content.Server.Light.Components;
using Content.Shared._RMC14.Xenonids.Acid;

namespace Content.Server._RMC14.Xenonids.Acid;

public sealed class XenoAcidSystem : SharedXenoAcidSystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpendableLightComponent, ServerCorrodingEvent>(OnServerCorrodingEvent);
    }

    private void OnServerCorrodingEvent(Entity<ExpendableLightComponent> target, ref ServerCorrodingEvent args)
    {
        // Rationale and formula: https://github.com/RMC-14/RMC-14/issues/2952#issuecomment-2227035752
        var expendable_light = target.Comp;
        var expendableLightDps = args.ExpendableLightDps + 1;
        expendable_light.StateExpiryTime /= expendableLightDps;
        // In case expandable light is activated shortly after being corroded. Or in case we decide to not destroy corrosive lights on timers like the rest of items for whatever the reason.
        expendable_light.GlowDuration /= expendableLightDps;
        expendable_light.FadeOutDuration /= expendableLightDps;
    }
}

