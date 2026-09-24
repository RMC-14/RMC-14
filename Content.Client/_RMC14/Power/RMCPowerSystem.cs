using Content.Client.Power.Components;
using Content.Shared._RMC14.Power;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Power;

public sealed class RMCPowerSystem : SharedRMCPowerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCApcComponent, AfterAutoHandleStateEvent>(OnApcState);

        SubscribeLocalEvent<RMCReactorPoweredLightComponent, AppearanceChangeEvent>(OnReactorPoweredLightAppearanceChange);
    }

    public override bool IsPowered(EntityUid ent)
    {
        return TryComp(ent, out ApcPowerReceiverComponent? receiver) && receiver.Powered;
    }

    private void OnApcState(Entity<RMCApcComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is RMCApcBui apcUi)
                    apcUi.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(RMCApcBui)}\n{e}");
        }
    }

    private void OnReactorPoweredLightAppearanceChange(Entity<RMCReactorPoweredLightComponent> ent, ref AppearanceChangeEvent args)
    {
        Pointlight.SetEnabled(ent, ent.Comp.Enabled);
    }
}
