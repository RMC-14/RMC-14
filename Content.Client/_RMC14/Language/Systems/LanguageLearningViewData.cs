using System.Linq;

namespace Content.Client._RMC14.Language.Systems;

public readonly record struct LanguageLearningViewData(
    bool RequiresFirstContact,
    bool Encountered,
    float Progress,
    Dictionary<string, float> LearnedWords)
{
    public bool IsVisible => !RequiresFirstContact || Encountered;
    public int WordCount => LearnedWords.Count;
    public float AverageWordComprehension => LearnedWords.Count == 0 ? 0f : LearnedWords.Values.Average();
}
