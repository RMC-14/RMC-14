using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared._RMC14.Mobs.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared._RMC14.Mobs
{
    public sealed class DropHatOnDeadOrCritSystem : EntitySystem
    {
        [Dependency] private readonly InventorySystem _inv = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DropHatOnDeadOrCritComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<DropHatOnDeadOrCritComponent, IsEquippingTargetAttemptEvent>(OnEquipTargetAttempt);
        }

        private void OnMobStateChanged(EntityUid uid, DropHatOnDeadOrCritComponent comp, MobStateChangedEvent args)
        {
            if (args.NewMobState != MobState.Dead && args.NewMobState != MobState.Critical)
                return;

            _inv.TryUnequip(uid, "head", force: true);
        }

        private void OnEquipTargetAttempt(EntityUid uid, DropHatOnDeadOrCritComponent comp, IsEquippingTargetAttemptEvent args)
        {
            if (args.Slot != "head")
                return;

            if (!TryComp<MobStateComponent>(uid, out var mob) || (mob.CurrentState != MobState.Dead && mob.CurrentState != MobState.Critical))
                return;

            args.Cancel();
        }
    }
}
