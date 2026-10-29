using Content.Client.UserInterface.Systems.Chat;
using Content.Shared._RMC14.Hallucination;
using Content.Shared.Chat;

namespace Content.Client._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem
{
    private void InitializeComms()
    {
        SubscribeNetworkEvent<RMCHallucinationSpeechEvent>(OnSpeech);
    }

    private void OnSpeech(RMCHallucinationSpeechEvent ev)
    {
        EntityUid? speaker = null;
        if (ev.Body)
        {
            foreach (var body in _bodies)
            {
                if (body.Id != ev.Id)
                    continue;

                speaker = body.Uid;
                break;
            }
        }
        else if (_attackers.TryGetValue(ev.Id, out var attacker))
        {
            speaker = attacker.Uid;
        }

        var source = speaker is { } uid && !TerminatingOrDeleted(uid) ? GetNetEntity(uid) : NetEntity.Invalid;
        var msg = new ChatMessage(ev.Channel, ev.Message, ev.WrappedMessage, source, null);
        _ui.GetUIController<ChatUIController>().ProcessChatMessage(msg);
    }
}
