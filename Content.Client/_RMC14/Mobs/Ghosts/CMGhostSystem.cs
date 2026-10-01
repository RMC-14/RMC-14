using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Mobs;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Client.Player;

namespace Content.Client._RMC14.Mobs.Ghosts
{
    public sealed class CMGhostSystem : SharedCMGhostSystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        private const ChatChannel PostDeathMutedChannels = ChatChannel.Local | ChatChannel.Whisper | ChatChannel.Radio | ChatChannel.Emotes | ChatChannel.Dead;

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
            Actions.RemoveAction(uid, comp.ToggleMarineHudEntity);
            Actions.RemoveAction(uid, comp.ToggleXenoHudEntity);
            Actions.RemoveAction(uid, comp.ToggleDeadChatEntity);
            Actions.RemoveAction(uid, comp.FindParasiteEntity);
        }

        public bool ShouldMutePostDeathChat(ChatChannel channel)
        {
            if ((channel & PostDeathMutedChannels) == 0 ||
                !Config.GetCVar(RMCCVars.RMCPostDeathChatMute) ||
                _player.LocalEntity is not { } localEntity ||
                !TryComp(localEntity, out CMGhostComponent? ghost) ||
                ghost.PostDeathChatMutedUntil is not { } mutedUntil)
            {
                return false;
            }

            return GameTiming.CurTime < mutedUntil;
        }
    }
}
