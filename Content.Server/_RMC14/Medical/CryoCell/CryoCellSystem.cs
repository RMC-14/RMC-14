using System.Linq;
using Content.Shared._RMC14.Medical.CryoCell;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.CryoCell;

public sealed class CryoCellSystem : SharedCryoCellSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoCellComponent, AfterActivatableUIOpenEvent>(OnUiOpen);
        SubscribeLocalEvent<CryoCellComponent, CryoCellTogglePowerBuiMsg>(OnUiTogglePower);
        SubscribeLocalEvent<CryoCellComponent, CryoCellEjectBuiMsg>(OnUiEject);
        SubscribeLocalEvent<CryoCellComponent, CryoCellToggleAutoEjectBuiMsg>(OnUiToggleAutoEject);
        SubscribeLocalEvent<CryoCellComponent, CryoCellEjectBeakerBuiMsg>(OnUiEjectBeaker);
        SubscribeLocalEvent<CryoCellComponent, CryoCellToggleNotifyBuiMsg>(OnUiToggleNotify);
    }

    private void OnUiOpen(Entity<CryoCellComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(ent);
    }

    private void OnUiTogglePower(Entity<CryoCellComponent> ent, ref CryoCellTogglePowerBuiMsg args)
    {
        ent.Comp.On = !ent.Comp.On;
        if (ent.Comp.On)
            ent.Comp.NextTick = _timing.CurTime + ent.Comp.TickDelay;
        Dirty(ent);
        UpdateUI(ent);
    }

    private void OnUiEject(Entity<CryoCellComponent> ent, ref CryoCellEjectBuiMsg args)
    {
        if (ent.Comp.Occupant is { } occupant)
            EjectOccupant(ent, occupant);

        UpdateUI(ent);
    }

    private void OnUiToggleAutoEject(Entity<CryoCellComponent> ent, ref CryoCellToggleAutoEjectBuiMsg args)
    {
        ent.Comp.AutoEject = !ent.Comp.AutoEject;
        Dirty(ent);
        UpdateUI(ent);
    }

    private void OnUiEjectBeaker(Entity<CryoCellComponent> ent, ref CryoCellEjectBeakerBuiMsg args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.BeakerSlot, out var cont) || cont.ContainedEntities.Count == 0)
            return;

        var beaker = cont.ContainedEntities.First();
        _container.Remove(beaker, cont);
        // Drop to owner map position
        _popup.PopupEntity(Loc.GetString("rmc-cryocell-beaker-ejected", ("entity", beaker)), ent);
        UpdateUI(ent);
    }

    private void OnUiToggleNotify(Entity<CryoCellComponent> ent, ref CryoCellToggleNotifyBuiMsg args)
    {
        ent.Comp.ReleaseNotice = !ent.Comp.ReleaseNotice;
        Dirty(ent);
        UpdateUI(ent);
    }

    private void UpdateUI(Entity<CryoCellComponent> ent)
    {
        var comp = ent.Comp;
        NetEntity? occupantNet = null;
        string occupantName = string.Empty;
        float health = 0f;
        float maxHealth = 0f;
        float bodyTemp = 0f;
        var hasBeaker = TryGetBeaker(ent.Owner, comp, out _);

        if (comp.Occupant is { } occupant && TryComp(occupant, out IMobControllerComponent? mob))
        {
            occupantNet = GetNetEntity(occupant);
            // Best-effort attempts to read health/body temp — replace with your project's exact APIs.
            // TODO: Replace GetEntityHealth/GetEntityMaxHealth/GetEntityBodyTemperature with actual project helpers.
            health = GetEntityHealth(occupant);
            maxHealth = GetEntityMaxHealth(occupant);
            bodyTemp = GetEntityBodyTemperature(occupant);
            occupantName = MetaData(occupant).EntityName;
        }

        var state = new CryoCellBuiState(occupantNet, occupantName, health, maxHealth, bodyTemp, comp.On, hasBeaker, comp.AutoEject, comp.ReleaseNotice);
        if (TryComp(ent.Owner, out BoundUserInterfaceComponent? bui))
        {
            // Bound UI sending is handled by the ActivatableUI system; we simply update via raising a UI state event.
            // If your framework uses RaiseNetworkEvent for BUI state, change this accordingly.
            bui.SetState(state);
        }
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        // Iterate all cryo cells and process ticks server-side.
        foreach (var (comp, ent) in EntityQuery<CryoCellComponent>(true))
        {
            if (!comp.On)
                continue;

            if (comp.NextTick > now)
                continue;

            // Schedule next tick
            comp.NextTick = now + comp.TickDelay;
            Dirty(ent);

            // Apply temperature drop
            comp.Temperature -= comp.TemperatureDropPerTick;

            // If occupant present, apply cooling/healing logic and possibly auto-eject
            if (comp.Occupant is { } occupant)
            {
                // 1. Adjust occupant body temperature toward cryo target (server must provide API)
                ApplyBodyTemperatureChange(occupant, comp);

                // 2. Apply healing while cold — per-damage-type
                ApplyHealing(occupant, comp);

                // 3. Auto-eject dead or if special rules trigger
                if (_mobState.IsDead(occupant))
                {
                    // If literal-dead and undefibbable or other rules in DM: eject immediately
                    EjectOccupant(ent, occupant);
                    // play auto-eject sound / popup if configured
                    if (comp.ReleaseNotice)
                        _popup.PopupEntity(Loc.GetString("rmc-cryocell-auto-eject-dead", ("entity", occupant)), ent);
                }
            }
        }
    }

    private void ApplyBodyTemperatureChange(EntityUid occupant, CryoCellComponent comp)
    {
        // TODO: Insert a call to the project's body temperature API.
        // Example (pseudocode — replace with your API):
        // var current = _bodyTempSystem.GetTemperature(occupant);
        // var newTemp = current - comp.TemperatureDropPerTick; // or using more exact heat capacity math
        // _bodyTempSystem.SetTemperature(occupant, newTemp);
        //
        // For now this function is a no-op placeholder to be replaced with the correct API call.
    }

    private void ApplyHealing(EntityUid occupant, CryoCellComponent comp)
    {
        // TODO: Replace with your damage/healing API calls. The original DM code heals brute/burn/toxin
        // gradually while in cryo. Here we call a small helper with marked TODOs.

        var brute = comp.HealBrutePerTick;
        var burn = comp.HealBurnPerTick;
        var toxin = comp.HealToxinPerTick;

        // Example placeholder:
        // _damageableSystem.Heal(occupant, DamageType.Brute, brute);
        // _damageableSystem.Heal(occupant, DamageType.Heat, burn);
        // _damageableSystem.Heal(occupant, DamageType.Toxin, toxin);

        // If your repo exposes direct component fields, call them here, otherwise adapt to your damage API.
    }

    // Helper placeholder getters: replace with your game's actual APIs.
    private float GetEntityHealth(EntityUid uid)
    {
        // TODO: read actual health from Damageable/Health component
        return 0f;
    }

    private float GetEntityMaxHealth(EntityUid uid)
    {
        // TODO: read actual max health from Damageable/Health component
        return 100f;
    }

    private float GetEntityBodyTemperature(EntityUid uid)
    {
        // TODO: read actual body temperature
        return 293.15f; // default ~20C in Kelvin-like scale used in DM
    }
}
