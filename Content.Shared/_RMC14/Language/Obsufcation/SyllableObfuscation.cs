using System.Text;
using Content.Shared._RMC14.Language.Systems;

namespace Content.Shared._RMC14.Language;

public sealed partial class SyllableObfuscation : ReplacementObfuscation
{
    private const char EndOfFile = (char)0;
    private const float FullComprehensionThreshold = 0.8f;
    private const float PartialComprehensionThreshold = 0.6f;

    [DataField]
    public int MinSyllables = 1;

    [DataField]
    public int MaxSyllables = 4;

    internal override void ObfuscateInternalWithComprehension(
        StringBuilder builder,
        string message,
        SharedLanguageSystem context,
        bool randomize,
        float comprehension)
    {
        if (Replacement.Count == 0) return;

        var wordProcessor = new WordProcessor(message, context, Replacement, comprehension, randomize);
        wordProcessor.ProcessWords(builder, MinSyllables, MaxSyllables);
    }

    private readonly struct WordProcessor
    {
        private readonly string _message;
        private readonly SharedLanguageSystem _context;
        private readonly IReadOnlyList<string> _replacement;
        private readonly float _comprehension;
        private readonly bool _randomize;

        public WordProcessor(string message, SharedLanguageSystem context,
            IReadOnlyList<string> replacement, float comprehension, bool randomize)
        {
            _message = message;
            _context = context;
            _replacement = replacement;
            _comprehension = comprehension;
            _randomize = randomize;
        }

        public void ProcessWords(StringBuilder builder, int minSyllables, int maxSyllables)
        {
            var wordBeginIndex = 0;
            var hashCode = 0;

            for (var i = 0; i <= _message.Length; i++)
            {
                var ch = i < _message.Length ? char.ToLower(_message[i]) : EndOfFile;
                var isWordEnd = char.IsWhiteSpace(ch) || IsPunctuation(ch) || ch == EndOfFile;

                if (!isWordEnd)
                {
                    hashCode = hashCode * 31 + ch;
                    continue;
                }

                ProcessWord(builder, wordBeginIndex, i, hashCode, minSyllables, maxSyllables);

                if (isWordEnd && ch != EndOfFile)
                    builder.Append(ch);

                hashCode = 0;
                wordBeginIndex = i + 1;
            }
        }

        private void ProcessWord(StringBuilder builder, int wordBeginIndex, int wordEndIndex,
            int hashCode, int minSyllables, int maxSyllables)
        {
            var wordLength = wordEndIndex - wordBeginIndex;
            if (wordLength <= 0) return;

            var word = _message.Substring(wordBeginIndex, wordLength);
            var wordComprehension = CalculateWordComprehension(word, _comprehension, _context, _randomize);

            if (wordComprehension >= FullComprehensionThreshold)
            {
                builder.Append(word);
            }
            else if (wordComprehension >= PartialComprehensionThreshold && wordLength > 2)
            {
                ObfuscateWordPartially(builder, word, hashCode, maxSyllables, wordComprehension);
            }
            else
            {
                ObfuscateWordCompletely(builder, hashCode, minSyllables, maxSyllables, wordComprehension);
            }
        }

        private void ObfuscateWordPartially(StringBuilder builder, string word, int hashCode,
            int maxSyllables, float wordComprehension)
        {
            builder.Append(word[0]);

            var syllableCount = Math.Max(1, (int)((1.0f - wordComprehension) * maxSyllables));
            AppendRandomSyllables(builder, hashCode, syllableCount);

            builder.Append(word[^1]);
        }

        private void ObfuscateWordCompletely(StringBuilder builder, int hashCode,
            int minSyllables, int maxSyllables, float wordComprehension)
        {
            var obfuscationIntensity = (1.0f - wordComprehension) * (1.0f - wordComprehension);
            var baseSyllables = _context.PseudoRandomNumber(hashCode, minSyllables, maxSyllables, _randomize);
            var adjustedSyllables = Math.Max(1, (int)(baseSyllables * obfuscationIntensity));

            AppendRandomSyllables(builder, hashCode, adjustedSyllables);
        }

        private void AppendRandomSyllables(StringBuilder builder, int hashCode, int syllableCount)
        {
            for (var i = 0; i < syllableCount; i++)
            {
                var index = _context.PseudoRandomNumber(hashCode + i, 0, _replacement.Count - 1, _randomize);
                builder.Append(_replacement[index]);
            }
        }
    }
}
