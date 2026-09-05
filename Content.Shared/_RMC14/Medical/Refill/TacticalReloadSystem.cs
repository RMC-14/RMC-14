using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Medical.Refill;

public sealed class TacticalReloadSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCTacticalReloadSlotComponent, AfterInteractEvent>(OnTacticalReloadInteract);
        SubscribeLocalEvent<RMCTacticalReloadSlotComponent, TacticalReloadDoAfterEvent>(OnTacticalReloadDoafter);
    }

    private void OnTacticalReloadInteract(Entity<RMCTacticalReloadSlotComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.SlotId, out var container))
            return;

        if (args.Target == null)
            return;

        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;

        if (!_slots.CanInsert(ent, args.Target.Value, args.User, slots.Slots[ent.Comp.SlotId], true))
            return;

        args.Handled = true;

        // The reload
        if (!_skills.HasSkills(args.User, ent.Comp.TacticalSkills))
        {
            _popup.PopupClient(Loc.GetString("rmc-hypospray-fail-tacreload"), args.Used, args.User);
            return;
        }

        if (container.ContainedEntities.Count == 0)
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.LoadText, ("target", ent)), args.Used, args.User);
        }
        else
        {
            if (!_slots.TryEjectToHands(ent, slots.Slots[ent.Comp.SlotId], args.User, true))
                return;
            _popup.PopupClient(Loc.GetString(ent.Comp.SwapText), args.Used, args.User);
        }

        _doafter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.TacticalReloadTime, new TacticalReloadDoAfterEvent(), ent, args.Target, ent)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
            MovementThreshold = 0.1f,
        });
    }

    private void OnTacticalReloadDoafter(Entity<RMCTacticalReloadSlotComponent> ent, ref TacticalReloadDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.SlotId, out var container))
            return;

        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;

        if (args.Target == null)
            return;

        args.Handled = true;

        _slots.TryInsert(ent, ent.Comp.SlotId, args.Target.Value, args.User, excludeUserAudio: true);
    }
}
