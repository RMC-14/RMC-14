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
        SubscribeLocalEvent<RMCSmesComponent, AfterAutoHandleStateEvent>(OnSmesState);
        SubscribeLocalEvent<RMCPowerStorageComponent, AfterAutoHandleStateEvent>(OnStorageState);
        SubscribeLocalEvent<RMCPowerMonitorComponent, AfterAutoHandleStateEvent>(OnMonitorState);

        SubscribeLocalEvent<RMCReactorPoweredLightComponent, AppearanceChangeEvent>(OnReactorPoweredLightAppearanceChange);
    }

    private void OnMonitorState(Entity<RMCPowerMonitorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(ent, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is RMCPowerMonitorBui monitorUi)
                monitorUi.Refresh();
        }
    }

    private void OnSmesState(Entity<RMCSmesComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshSmes(ent);
    }

    private void OnStorageState(Entity<RMCPowerStorageComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshSmes(ent);
    }

    private void RefreshSmes(EntityUid uid)
    {
        if (!TryComp(uid, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is RMCSmesBui smesUi)
                smesUi.Refresh();
        }
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
