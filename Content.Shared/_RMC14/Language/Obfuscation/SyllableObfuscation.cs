using System.Text;
using Content.Shared._RMC14.Language.Systems;

namespace Content.Shared._RMC14.Language;

public sealed partial class SyllableObfuscation : ReplacementObfuscation
{
    private const char EndOfFile = (char)0;

    [DataField]
    public float ClearWordThreshold = 0.8f;

    [DataField]
    public float PartialWordThreshold = 0.6f;

    [DataField]
    public int MinimumPartialWordLength = 3;

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
        if (Replacement.Count == 0)
            return;

        var wordProcessor = new WordProcessor(
            message,
            context,
            Replacement,
            comprehension,
            randomize,
            ClearWordThreshold,
            PartialWordThreshold,
            MinimumPartialWordLength,
            ComprehensionVariance);
        wordProcessor.ProcessWords(builder, MinSyllables, MaxSyllables);
    }

    private readonly struct WordProcessor
    {
        private readonly string _message;
        private readonly SharedLanguageSystem _context;
        private readonly IReadOnlyList<string> _replacement;
        private readonly float _comprehension;
        private readonly bool _randomize;
        private readonly float _clearWordThreshold;
        private readonly float _partialWordThreshold;
        private readonly int _minimumPartialWordLength;
        private readonly float _comprehensionVariance;

        public WordProcessor(string message, SharedLanguageSystem context,
            IReadOnlyList<string> replacement, float comprehension, bool randomize,
            float clearWordThreshold, float partialWordThreshold, int minimumPartialWordLength,
            float comprehensionVariance)
        {
            _message = message;
            _context = context;
            _replacement = replacement;
            _comprehension = comprehension;
            _randomize = randomize;
            _clearWordThreshold = clearWordThreshold;
            _partialWordThreshold = partialWordThreshold;
            _minimumPartialWordLength = minimumPartialWordLength;
            _comprehensionVariance = comprehensionVariance;
        }

        public void ProcessWords(StringBuilder builder, int minSyllables, int maxSyllables)
        {
            var wordBeginIndex = 0;
            var hashCode = 0;

            for (var i = 0; i <= _message.Length; i++)
            {
                var ch = i < _message.Length ? char.ToLowerInvariant(_message[i]) : EndOfFile;
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
            if (wordLength <= 0)
                return;

            var word = _message.Substring(wordBeginIndex, wordLength);
            var wordComprehension = CalculateWordComprehension(
                word,
                _comprehension,
                _context,
                _randomize,
                _comprehensionVariance);

            if (wordComprehension >= _clearWordThreshold)
            {
                builder.Append(word);
            }
            else if (wordComprehension >= _partialWordThreshold && wordLength >= _minimumPartialWordLength)
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

            var syllableCount = Math.Max(1, (int) ((1.0f - wordComprehension) * maxSyllables));
            AppendRandomSyllables(builder, hashCode, syllableCount);

            builder.Append(word[^1]);
        }

        private void ObfuscateWordCompletely(StringBuilder builder, int hashCode,
            int minSyllables, int maxSyllables, float wordComprehension)
        {
            var obfuscationIntensity = (1.0f - wordComprehension) * (1.0f - wordComprehension);
            var baseSyllables = _context.PseudoRandomNumber(hashCode, minSyllables, maxSyllables, _randomize);
            var adjustedSyllables = Math.Max(1, (int) (baseSyllables * obfuscationIntensity));

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
