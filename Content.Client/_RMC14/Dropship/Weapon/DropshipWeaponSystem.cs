using Content.Client.Light.Components;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared.Light.Components;

namespace Content.Client._RMC14.Dropship.Weapon;

public sealed class DropshipWeaponSystem : SharedDropshipWeaponSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DropshipTerminalWeaponsComponent, AfterAutoHandleStateEvent>(OnWeaponsState);
    }

    private void OnWeaponsState(Entity<DropshipTerminalWeaponsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshWeaponsUI(ent);
    }

    protected override void RefreshWeaponsUI(Entity<DropshipTerminalWeaponsComponent> terminal)
    {
        try
        {
            base.RefreshWeaponsUI(terminal);
            if (!TryComp(terminal, out UserInterfaceComponent? ui))
                return;

            foreach (var open in ui.ClientOpenInterfaces.Values)
            {
                if (open is DropshipWeaponsBui bui)
                    bui.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(DropshipWeaponsBui)}:\n{e}");
        }
    }

    protected override bool IsFlareBurning(EntityUid ent)
    {
        if (!TryComp(ent, out ExpendableLightComponent? light))
            return false;

        return light.CurrentState is ExpendableLightState.Lit or ExpendableLightState.Fading;
    }
}
