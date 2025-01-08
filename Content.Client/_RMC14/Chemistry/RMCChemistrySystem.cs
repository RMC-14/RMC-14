using Content.Shared._RMC14.Chemistry;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._RMC14.Chemistry;

public sealed class RMCChemistryUISystem : SharedRMCChemistrySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCChemicalDispenserComponent, AfterAutoHandleStateEvent>(OnDispenserAfterState);
    }

    private void OnDispenserAfterState(Entity<RMCChemicalDispenserComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateDispenserUI(ent);
    }

    private void UpdateDispenserUI(Entity<RMCChemicalDispenserComponent> ent)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is RMCChemicalDispenserBui dispenserUi)
                    dispenserUi.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(RMCChemicalDispenserBui)}\n{e}");
        }
    }

    protected override void DispenserUpdated(Entity<RMCChemicalDispenserComponent> ent)
    {
        base.DispenserUpdated(ent);
        UpdateDispenserUI(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_player.LocalEntity is not { } ent)
            return;

        foreach (var actorUi in _ui.GetActorUis(ent))
        {
            if (actorUi.Key is not RMCChemicalDispenserUi.Key)
                continue;

            if (!TryComp(actorUi.Entity, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is RMCChemicalDispenserBui dispenserUi)
                    dispenserUi.Refresh();
            }
        }
    }
}
