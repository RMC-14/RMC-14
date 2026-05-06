using System.Text;
using Content.Shared._RMC14.Language.Systems;

namespace Content.Shared._RMC14.Language;

public sealed partial class XenoObfuscation : ObfuscationMethod
{
    private const float MinComprehensionThreshold = 0.01f;
    private const float HighComprehensionThreshold = 0.85f;
    private const float BaseObfuscationChance = 0.7f;

    [DataField]
    public List<string> XenoSounds = new()
    {
        "hss", "sss", "tss", "chk", "krr", "hrr",
        "skr", "tsk", "rrk", "hsk", "ssk"
    };

    [DataField]
    public char[] GarbleChars = { '~', '?', '*', '#', '¿', '§' };

    [DataField]
    public float IntensityMultiplier = 1.0f;

    internal override void ObfuscateInternal(
        StringBuilder builder,
        string message,
        SharedLanguageSystem context,
        bool randomize)
    {
        ObfuscateInternalWithComprehension(builder, message, context, randomize, 0.0f);
    }

    internal override void ObfuscateInternalWithComprehension(
        StringBuilder builder,
        string message,
        SharedLanguageSystem context,
        bool randomize,
        float comprehension)
    {
        if (XenoSounds.Count == 0 || GarbleChars.Length == 0) return;

        if (comprehension <= MinComprehensionThreshold)
        {
            ObfuscateCompletelyAsXeno(builder, message, context, randomize);
            return;
        }

        var processor = new XenoWordProcessor(
            message,
            XenoSounds,
            GarbleChars,
            comprehension,
            IntensityMultiplier,
            context,
            randomize);
        processor.ProcessMessage(builder);
    }

    private void ObfuscateCompletelyAsXeno(
        StringBuilder builder,
        string message,
        SharedLanguageSystem context,
        bool randomize)
    {
        var random = context.CreateRandom(message.GetHashCode(), randomize);
        var wordCount = CountWords(message);

        for (var i = 0; i < wordCount; i++)
        {
            if (i > 0)
                builder.Append(' ');

            AppendXenoSoundsForWord(builder, random);
        }

        PreserveSentenceEndingPunctuation(builder, message);
    }

    private static int CountWords(string message)
    {
        var wordCount = 0;
        var inWord = false;

        foreach (var ch in message)
        {
            var isLetter = char.IsLetter(ch);
            if (isLetter && !inWord)
            {
                wordCount++;
                inWord = true;
            }
            else if (!isLetter && inWord)
            {
                inWord = false;
            }
        }

        return wordCount;
    }

    private void AppendXenoSoundsForWord(StringBuilder builder, System.Random random)
    {
        var soundCount = random.Next(1, 4);

        for (var i = 0; i < soundCount; i++)
        {
            if (i > 0 && random.Next(0, 3) == 0)
                builder.Append('-');

            builder.Append(XenoSounds[random.Next(XenoSounds.Count)]);
        }
    }

    private static void PreserveSentenceEndingPunctuation(StringBuilder builder, string message)
    {
        if (message.Length > 0 && IsSentenceEndPunctuation(message[^1]))
            builder.Append(message[^1]);
    }

    private readonly struct XenoWordProcessor
    {
        private readonly string _message;
        private readonly IReadOnlyList<string> _xenoSounds;
        private readonly char[] _garbleChars;
        private readonly float _comprehension;
        private readonly float _intensityMultiplier;
        private readonly SharedLanguageSystem _context;
        private readonly bool _randomize;

        public XenoWordProcessor(string message, IReadOnlyList<string> xenoSounds,
            char[] garbleChars, float comprehension, float intensityMultiplier,
            SharedLanguageSystem context, bool randomize)
        {
            _message = message;
            _xenoSounds = xenoSounds;
            _garbleChars = garbleChars;
            _comprehension = comprehension;
            _intensityMultiplier = intensityMultiplier;
            _context = context;
            _randomize = randomize;
        }

        public void ProcessMessage(StringBuilder builder)
        {
            var random = _context.CreateRandom(_message.GetHashCode(), _randomize);
            var currentWord = new StringBuilder();

            for (var i = 0; i <= _message.Length; i++)
            {
                var ch = i < _message.Length ? _message[i] : '\0';
                var isWordEnd = char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || i == _message.Length;

                if (!isWordEnd)
                {
                    currentWord.Append(ch);
                    continue;
                }

                ProcessWord(builder, currentWord.ToString(), random);
                currentWord.Clear();

                if (ch != '\0')
                    builder.Append(ch);
            }
        }

        private void ProcessWord(StringBuilder builder, string word, System.Random random)
        {
            if (string.IsNullOrEmpty(word)) return;

            var wordComprehension = CalculateWordComprehension(word, _comprehension, _context, _randomize);

            if (wordComprehension >= HighComprehensionThreshold)
            {
                builder.Append(word);
                return;
            }

            var obfuscationChance = (1.0f - wordComprehension) * BaseObfuscationChance * _intensityMultiplier;

            foreach (var ch in word)
            {
                if (char.IsLetter(ch))
                {
                    if (random.NextDouble() < obfuscationChance)
                    {
                        AppendObfuscatedCharacter(builder, random);
                    }
                    else
                    {
                        builder.Append(ch);
                    }
                }
                else
                {
                    builder.Append(ch);
                }
            }
        }

        private void AppendObfuscatedCharacter(StringBuilder builder, System.Random random)
        {
            // 1/3 chance for xeno sound, 2/3 chance for garble character
            if (random.Next(0, 3) == 0)
            {
                builder.Append(_xenoSounds[random.Next(_xenoSounds.Count)]);
            }
            else
            {
                builder.Append(_garbleChars[random.Next(_garbleChars.Length)]);
            }
        }
    }
}
