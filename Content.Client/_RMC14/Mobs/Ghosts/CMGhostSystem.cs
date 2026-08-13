using Content.Shared._RMC14.Mobs;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Robust.Client.Player;

namespace Content.Client._RMC14.Mobs.Ghosts
{
    public sealed class CMGhostSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CMGhostComponent, ComponentRemove>(OnCMGhostRemove);
            SubscribeLocalEvent<CMGhostComponent, ToggleDeadChatActionEvent>(OnToggleDeadChat);
        }

        private void OnToggleDeadChat(Entity<CMGhostComponent> ent, ref ToggleDeadChatActionEvent args)
        {
            if (args.Handled || ent.Owner != _player.LocalEntity)
                return;

            args.Handled = true;
            args.Toggle = true;

            var msg = "rmc-ghost-dead-chat-unmuted";
            if (!RemComp<RMCDeadChatMutedComponent>(ent))
            {
                EnsureComp<RMCDeadChatMutedComponent>(ent);
                msg = "rmc-ghost-dead-chat-muted";
            }

            _popup.PopupClient(Loc.GetString(msg), ent, ent);
        }

        private void OnCMGhostRemove(EntityUid uid, CMGhostComponent comp, ComponentRemove remove)
        {
            _actions.RemoveAction(uid, comp.ToggleMarineHudEntity);
            _actions.RemoveAction(uid, comp.ToggleXenoHudEntity);
            _actions.RemoveAction(uid, comp.ToggleDeadChatEntity);
            _actions.RemoveAction(uid, comp.FindParasiteEntity);
        }
    }
}
