using System.Linq;
using Content.Server._RMC14.Chat.Chat;
using Content.Server.Radio;
using Content.Shared._RMC14.Deafness;
using Content.Shared.Chat;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Deafness;

public sealed class DeafnessSystem : SharedDeafnessSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly List<string> _punctuation = new List<string> { ",", "!", ".", ";", "?" };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeafComponent, RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<DeafComponent, ChatMessageOverrideInVoiceRangeEvent>(OnOverrideInVoiceRange);
    }

    private void OnRadioReceiveAttempt(Entity<DeafComponent> ent, ref RadioReceiveAttemptEvent args)
    {
        if (args.RadioReceiver != ent.Owner)
            return;

        args.Cancelled = true;
    }

    private void OnOverrideInVoiceRange(Entity<DeafComponent> ent, ref ChatMessageOverrideInVoiceRangeEvent args)
    {
        if (args.Channel == ChatChannel.Emotes
            || args.Channel == ChatChannel.Damage
            || args.Channel == ChatChannel.Visual
            || args.Channel == ChatChannel.Notifications
            || args.Channel == ChatChannel.OOC
            || args.Channel == ChatChannel.LOOC
        )
            return;

        if (_random.Prob(ent.Comp.HearChance))
        {
            var words = args.Message.Split(' ');
            var heardWord = words[_random.Next(words.Length)];
            var finalWord = RemovePunctuation(heardWord, _punctuation);

            var isSelf = ent.Owner != args.Source ? "rmc-deaf-hear-others" : "rmc-deaf-hear-self";
            args.WrappedMessage = Loc.GetString(isSelf, ("message", finalWord));
            args.Message = finalWord;
        }
        else
        {
            var message = Loc.GetString(ent.Owner != args.Source ? "rmc-deaf-talk-others" : "rmc-deaf-talk-self");
            args.WrappedMessage = message;
            args.Message = message;
        }
    }

    public static string RemovePunctuation(string word, List<string> punctuation)
    {
        if (punctuation.Contains(word.FirstOrDefault().ToString()))
            word = word.Substring(1);

        if (punctuation.Contains(word.LastOrDefault().ToString()))
            word = word.Substring(0, word.Length - 1);

        return word;
    }
}
