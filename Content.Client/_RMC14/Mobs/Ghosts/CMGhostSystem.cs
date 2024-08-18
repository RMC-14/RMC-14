using Content.Shared.Ghost;
using Content.Shared._RMC14.Mobs;
using Content.Shared.Actions;

namespace Content.Client._RMC14.Mobs.Ghosts
{
    public sealed class CMGhostSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CMGhostComponent, ComponentRemove>(OnCMGhostRemove);
        }

        private void OnCMGhostRemove(EntityUid uid, CMGhostComponent comp, ComponentRemove remove)
        {
            _actions.RemoveAction(uid, comp.ToggleMarineHudEntity);
            _actions.RemoveAction(uid, comp.ToggleXenoHudEntity);
        }
    }
}
