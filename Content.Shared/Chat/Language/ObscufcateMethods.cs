using System.Text;
using Content.Shared._RMC14.Language.Systems;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Language;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ObfuscationMethod
{
    public static readonly ObfuscationMethod Default = new ReplacementObfuscation
    {
        Replacement = new List<string> { "<?>" }
    };

    internal abstract void ObfuscateInternal(StringBuilder builder, string message, SharedLanguageSystem context);
    internal abstract void ObfuscateInternalWithComprehension(StringBuilder builder, string message, SharedLanguageSystem context, float comprehension);
}

public partial class ReplacementObfuscation : ObfuscationMethod
{
    [DataField(required: true)]
    public List<string> Replacement = [];

    internal override void ObfuscateInternal(StringBuilder builder, string message, SharedLanguageSystem context)
    {
        ObfuscateInternalWithComprehension(builder, message, context, 0.0f);
    }

    internal override void ObfuscateInternalWithComprehension(StringBuilder builder, string message, SharedLanguageSystem context, float comprehension)
    {
        var idx = context.PseudoRandomNumber(message.GetHashCode(), 0, Replacement.Count - 1);
        builder.Append(Replacement[idx]);
    }
}

public sealed partial class SyllableObfuscation : ReplacementObfuscation
{
    [DataField]
    public int MinSyllables = 1;

    [DataField]
    public int MaxSyllables = 4;

    internal override void ObfuscateInternalWithComprehension(StringBuilder builder, string message, SharedLanguageSystem context, float comprehension)
    {
        const char eof = (char) 0;

        var wordBeginIndex = 0;
        var hashCode = 0;

        for (var i = 0; i <= message.Length; i++)
        {
            var ch = i < message.Length ? char.ToLower(message[i]) : eof;
            var isWordEnd = char.IsWhiteSpace(ch) || IsPunctuation(ch) || ch == eof;

            if (!isWordEnd)
                hashCode = hashCode * 31 + ch;

            if (isWordEnd)
            {
                var wordLength = i - wordBeginIndex;
                if (wordLength > 0)
                {
                    var word = message.Substring(wordBeginIndex, wordLength);
                    var wordComprehension = CalculateWordComprehension(word, comprehension, context);

                    if (wordComprehension >= 0.8f)
                    {
                        builder.Append(word);
                    }
                    else if (wordComprehension >= 0.6f && wordLength > 2)
                    {
                        builder.Append(word[0]);
                        var syllables = Math.Max(1, (int)((1.0f - wordComprehension) * MaxSyllables));
                        for (var j = 0; j < syllables; j++)
                        {
                            var index = context.PseudoRandomNumber(hashCode + j, 0, Replacement.Count - 1);
                            builder.Append(Replacement[index]);
                        }
                        builder.Append(word[word.Length - 1]);
                    }
                    else
                    {
                        var obfuscationIntensity = (1.0f - wordComprehension) * (1.0f - wordComprehension);
                        var baseSyllables = context.PseudoRandomNumber(hashCode, MinSyllables, MaxSyllables);
                        var adjustedSyllables = Math.Max(1, (int)(baseSyllables * obfuscationIntensity));

                        for (var j = 0; j < adjustedSyllables; j++)
                        {
                            var index = context.PseudoRandomNumber(hashCode + j, 0, Replacement.Count - 1);
                            builder.Append(Replacement[index]);
                        }
                    }
                }

                hashCode = 0;
                wordBeginIndex = i + 1;
            }

            if (isWordEnd && ch != eof)
                builder.Append(ch);
        }
    }

    private float CalculateWordComprehension(string word, float baseComprehension, SharedLanguageSystem context)
    {
        var random = new System.Random(word.GetHashCode());
        var variance = random.NextSingle() * 0.2f - 0.1f;
        return Math.Clamp(baseComprehension + variance, 0.0f, 1.0f);
    }

    private static bool IsPunctuation(char ch)
    {
        return ch is '.' or '!' or '?' or ',' or ':';
    }
}

public sealed partial class PhraseObfuscation : ReplacementObfuscation
{
    [DataField]
    public int MinPhrases = 1;

    [DataField]
    public int MaxPhrases = 4;

    [DataField]
    public string Separator = " ";

    [DataField]
    public float Proportion = 1f / 3;

    internal override void ObfuscateInternalWithComprehension(StringBuilder builder, string message, SharedLanguageSystem context, float comprehension)
    {
        var sentenceBeginIndex = 0;
        var hashCode = 0;

        for (var i = 0; i < message.Length; i++)
        {
            var ch = char.ToLower(message[i]);
            if (!IsPunctuation(ch) && i != message.Length - 1)
            {
                hashCode = hashCode * 31 + ch;
                continue;
            }

            var length = i - sentenceBeginIndex;
            if (length >= 0)
            {
                var sentence = message.Substring(sentenceBeginIndex, length);

                if (comprehension >= 0.8f)
                {
                    builder.Append(sentence);
                }
                else
                {
                    var obfuscationIntensity = (1.0f - comprehension) * (1.0f - comprehension);
                    var basePhrases = (int) Math.Clamp(Math.Pow(length, Proportion) - 1, MinPhrases, MaxPhrases);
                    var adjustedPhrases = Math.Max(1, (int)(basePhrases * obfuscationIntensity));

                    for (var j = 0; j < adjustedPhrases; j++)
                    {
                        var phraseIdx = context.PseudoRandomNumber(hashCode + j, 0, Replacement.Count - 1);
                        var phrase = Replacement[phraseIdx];
                        builder.Append(phrase);
                        if (j < adjustedPhrases - 1)
                            builder.Append(Separator);
                    }
                }
            }
            sentenceBeginIndex = i + 1;

            if (IsPunctuation(ch))
                builder.Append(ch).Append(' ');
        }
    }

    private static bool IsPunctuation(char ch)
    {
        return ch is '.' or '!' or '?';
    }
}

public sealed partial class XenoObfuscation : ObfuscationMethod
{
    [DataField]
    public List<string> XenoSounds = new() { "hss", "sss", "tss", "chk", "krr", "hrr", "skr", "tsk", "rrk", "hsk", "ssk" };

    [DataField]
    public char[] GarbleChars = { '~', '?', '*', '#', '¿', '§' };

    [DataField]
    public float IntensityMultiplier = 1.0f;

    internal override void ObfuscateInternal(StringBuilder builder, string message, SharedLanguageSystem context)
    {
        ObfuscateInternalWithComprehension(builder, message, context, 0.0f);
    }

    internal override void ObfuscateInternalWithComprehension(StringBuilder builder, string message, SharedLanguageSystem context, float comprehension)
    {
        // If comprehension is essentially zero (non-speakers), completely obfuscate
        if (comprehension <= 0.01f)
        {
            ObfuscateCompletelyAsXeno(builder, message, context);
            return;
        }

        var random = new System.Random(message.GetHashCode());
        var currentWordStart = 0;
        var currentWord = new StringBuilder();

        for (var i = 0; i <= message.Length; i++)
        {
            var ch = i < message.Length ? message[i] : '\0';
            var isWordEnd = char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || i == message.Length;

            if (!isWordEnd)
            {
                currentWord.Append(ch);
                continue;
            }

            if (currentWord.Length > 0)
            {
                var word = currentWord.ToString();
                var wordComprehension = CalculateWordComprehension(word, comprehension, random);

                if (wordComprehension >= 0.85f)
                {
                    builder.Append(word);
                }
                else
                {
                    var obfuscationChance = (1.0f - wordComprehension) * 0.7f * IntensityMultiplier;

                    for (var j = 0; j < word.Length; j++)
                    {
                        var wordChar = word[j];
                        if (char.IsLetter(wordChar))
                        {
                            if (random.NextDouble() < obfuscationChance)
                            {
                                if (random.Next(0, 3) == 0)
                                {
                                    builder.Append(XenoSounds[random.Next(XenoSounds.Count)]);
                                }
                                else
                                {
                                    builder.Append(GarbleChars[random.Next(GarbleChars.Length)]);
                                }
                            }
                            else
                            {
                                builder.Append(wordChar);
                            }
                        }
                        else
                        {
                            builder.Append(wordChar);
                        }
                    }
                }

                currentWord.Clear();
            }

            if (ch != '\0')
                builder.Append(ch);
        }
    }

    private void ObfuscateCompletelyAsXeno(StringBuilder builder, string message, SharedLanguageSystem context)
    {
        var random = new System.Random(message.GetHashCode());
        var wordCount = 0;
        var inWord = false;

        // Count words to determine how many xeno sounds to generate
        for (var i = 0; i < message.Length; i++)
        {
            var ch = message[i];
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

        // Generate xeno sounds for each word
        for (var i = 0; i < wordCount; i++)
        {
            if (i > 0)
                builder.Append(' ');

            // Generate 1-3 xeno sounds per word
            var soundCount = random.Next(1, 4);
            for (var j = 0; j < soundCount; j++)
            {
                if (j > 0 && random.Next(0, 3) == 0)
                    builder.Append('-');

                builder.Append(XenoSounds[random.Next(XenoSounds.Count)]);
            }
        }

        // Preserve sentence ending punctuation
        if (message.Length > 0)
        {
            var lastChar = message[message.Length - 1];
            if (lastChar is '.' or '!' or '?')
                builder.Append(lastChar);
        }
    }

    private float CalculateWordComprehension(string word, float baseComprehension, System.Random random)
    {
        var variance = (float)(random.NextDouble() * 0.2 - 0.1);
        return Math.Clamp(baseComprehension + variance, 0.0f, 1.0f);
    }
}
