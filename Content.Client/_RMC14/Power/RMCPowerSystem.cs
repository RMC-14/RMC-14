using Content.Client.Power.Components;
using Content.Shared._RMC14.Power;

namespace Content.Client._RMC14.Power;

public sealed class RMCPowerSystem : SharedRMCPowerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCApcComponent, AfterAutoHandleStateEvent>(OnApcState);
        SubscribeLocalEvent<RMCPortableGeneratorComponent, AfterAutoHandleStateEvent>(OnPortableGeneratorState);
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

    private void OnPortableGeneratorState(Entity<RMCPortableGeneratorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is RMCPortableGeneratorBui genUi)
                    genUi.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(RMCPortableGeneratorBui)}\n{e}");
        }
    }
}
