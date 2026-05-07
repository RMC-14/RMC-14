using System.Linq;
using Content.Client._RMC14.Language.Systems;
using Content.Shared._RMC14.Language.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Systems.Language;

public sealed class LanguageLearningProgressControl : Control
{
    [Dependency] private readonly LanguageLearningSystem _languageLearning = default!;

    private const int DefaultWordsPerRow = 4;
    private const float EstimatedChipWidth = 90f;
    private const int MaxWordsPerRow = 10;
    private const int MinWordsPerRow = 2;
    private const int WordsPanelHeight = 180;
    private const int SearchBoxHeight = 24;
    private const int ExpandButtonSize = 24;
    private const int IconSize = 32;

    private static readonly Color[] ComprehensionColors =
    {
        Color.DarkRed,    // < 0.2
        Color.Red,        // 0.2-0.4
        Color.Orange,     // 0.4-0.6
        Color.Yellow,     // 0.6-0.8
        Color.LightGreen  // >= 0.8
    };

    private static readonly Color[] ComprehensionBackgroundColors =
    {
        Color.FromHex("#1F1B1B"), // < 0.2
        Color.FromHex("#2F1B1B"), // 0.2-0.4
        Color.FromHex("#2F251B"), // 0.4-0.6
        Color.FromHex("#2F2F1B"), // 0.6-0.8
        Color.FromHex("#1B2F1B")  // >= 0.8
    };

    private readonly LanguageProgressData _data;
    private readonly StyleBox _headerStyle;
    private readonly StyleBox _wordsStyle;

    private BoxContainer? _wordsContainer;
    private Button? _expandButton;
    private Label? _statsLabel;
    private ProgressBar? _progressBar;
    private PanelContainer? _wordsPanel;
    private LineEdit? _searchBox;

    private bool _wordsExpanded = false;
    private string _searchFilter = "";
    private List<WordEntry>? _filteredWords;

    public string LanguageId => _data.Prototype.ID;
    public event Action<string, bool>? OnExpansionChanged;

    public LanguageLearningProgressControl(LanguagePrototype prototype, float overallProgress,
        int wordCount, float avgWordComprehension, Dictionary<string, float>? learnedWords)
    {
        IoCManager.InjectDependencies(this);
        _data = new LanguageProgressData(prototype, overallProgress, wordCount, avgWordComprehension, learnedWords);
        _headerStyle = CreateHeaderStyle();
        _wordsStyle = CreateWordsStyle();

        HorizontalExpand = true;
        BuildUI();
        UpdateDisplayedData();
    }

    public void UpdateData(LanguagePrototype prototype, float overallProgress,
        int wordCount, float avgWordComprehension, Dictionary<string, float>? learnedWords)
    {
        _data.Update(prototype, overallProgress, wordCount, avgWordComprehension, learnedWords);
        _filteredWords = null;
        _searchFilter = "";

        UpdateDisplayedData();

        if (_searchBox != null)
            _searchBox.Text = "";

        if (_wordsExpanded && _wordsContainer != null)
            RebuildWordsDisplay();
    }

    public void SetExpanded(bool expanded)
    {
        if (_wordsExpanded == expanded)
            return;

        _wordsExpanded = expanded;
        UpdateExpansionState();
        OnExpansionChanged?.Invoke(LanguageId, _wordsExpanded);
    }

    public void SetThemeColor(Color themeColor)
    {
        var hsv = Color.ToHsv(themeColor);
        var darkerTheme = Color.FromHsv(hsv with { Z = hsv.Z * 0.8f });

        if (_headerStyle is StyleBoxFlat headerFlat)
            headerFlat.BackgroundColor = themeColor;

        if (_wordsStyle is StyleBoxFlat wordsFlat)
            wordsFlat.BackgroundColor = darkerTheme;
    }

    protected override void Resized()
    {
        base.Resized();

        if (_wordsExpanded && _filteredWords?.Count > 0)
            BuildWordsDisplay();
    }

    private void UpdateDisplayedData()
    {
        UpdateStatsLabel();
        UpdateProgressBar();
        UpdateExpandButtonVisibility();
    }

    private void UpdateStatsLabel()
    {
        if (_statsLabel == null) return;

        _statsLabel.Text = $"Overall: {_data.OverallProgress:P1} | Words: {_data.WordCount} | Avg: {_data.AvgWordComprehension:P1}";
    }

    private void UpdateProgressBar()
    {
        if (_progressBar == null) return;

        _progressBar.Value = _data.OverallProgress;
    }

    private void UpdateExpandButtonVisibility()
    {
        var shouldShowButton = _data.HasLearnedWords;

        if (_expandButton != null)
        {
            _expandButton.Visible = shouldShowButton;
        }
        else if (shouldShowButton)
        {
            CreateAndAddExpandButton();
        }
    }

    private void UpdateExpansionState()
    {
        if (_wordsPanel != null)
        {
            _wordsPanel.Visible = _wordsExpanded;

            if (_wordsExpanded && _data.HasLearnedWords)
                RebuildWordsDisplay();
        }

        if (_expandButton != null)
            _expandButton.Text = _wordsExpanded ? "▼" : "►";
    }

    private void BuildUI()
    {
        var mainContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true
        };

        mainContainer.AddChild(CreateHeaderPanel());
        mainContainer.AddChild(CreateWordsPanel());

        AddChild(mainContainer);
    }

    private Control CreateHeaderPanel()
    {
        var headerPanel = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = _headerStyle
        };

        var headerBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true
        };

        headerBox.AddChild(CreateLanguageIcon());
        headerBox.AddChild(CreateInfoContainer());

        headerPanel.AddChild(headerBox);
        return headerPanel;
    }

    private Control CreateLanguageIcon()
    {
        var icon = new TextureRect
        {
            SetWidth = IconSize,
            SetHeight = IconSize,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            Margin = new Thickness(0, 0, 8, 0)
        };

        LoadLanguageIcon(icon);
        return icon;
    }

    private void LoadLanguageIcon(TextureRect icon)
    {
        if (string.IsNullOrEmpty(_data.Prototype.LanguageIcon))
            return;

        var resourceCache = IoCManager.Resolve<IResourceCache>();
        if (resourceCache.TryGetResource<TextureResource>(_data.Prototype.LanguageIcon, out var texture))
            icon.Texture = texture;
    }

    private Control CreateInfoContainer()
    {
        var infoContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center
        };

        var nameLabel = new Label
        {
            Text = _data.Prototype.LocalizedName,
            FontColorOverride = Color.White,
            StyleClasses = { "LabelHeading" }
        };

        _statsLabel = new Label
        {
            FontColorOverride = Color.LightGray,
            StyleClasses = { "LabelSubText" }
        };

        _progressBar = new ProgressBar
        {
            HorizontalExpand = true,
            SetHeight = 8,
            Margin = new Thickness(0, 2, 0, 0)
        };

        infoContainer.AddChild(nameLabel);
        infoContainer.AddChild(_statsLabel);
        infoContainer.AddChild(_progressBar);

        return infoContainer;
    }

    private Control CreateWordsPanel()
    {
        _wordsPanel = new PanelContainer
        {
            HorizontalExpand = true,
            SetHeight = WordsPanelHeight,
            PanelOverride = _wordsStyle,
            Visible = false
        };

        var wordsMainContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 2
        };

        _searchBox = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = "Search words...",
            SetHeight = SearchBoxHeight,
            Margin = new Thickness(6, 4, 6, 2)
        };
        _searchBox.OnTextChanged += OnSearchTextChanged;

        var scrollContainer = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false,
            VScrollEnabled = true,
            ReturnMeasure = true
        };

        _wordsContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 3,
            Margin = new Thickness(6, 2, 6, 4)
        };

        scrollContainer.AddChild(_wordsContainer);
        wordsMainContainer.AddChild(_searchBox);
        wordsMainContainer.AddChild(scrollContainer);
        _wordsPanel.AddChild(wordsMainContainer);

        return _wordsPanel;
    }

    private void CreateAndAddExpandButton()
    {
        _expandButton = new Button
        {
            Text = "►",
            SetWidth = ExpandButtonSize,
            SetHeight = ExpandButtonSize,
            VerticalAlignment = VAlignment.Center,
            ToggleMode = false
        };

        _expandButton.OnPressed += _ => SetExpanded(!_wordsExpanded);

        // Add to header container
        if (Children.FirstOrDefault()?.Children.FirstOrDefault()?.Children.FirstOrDefault() is BoxContainer headerBox)
            headerBox.AddChild(_expandButton);
    }

    private void OnSearchTextChanged(LineEdit.LineEditEventArgs args)
    {
        _searchFilter = args.Text.Trim();
        FilterAndDisplayWords();
    }

    private void RebuildWordsDisplay()
    {
        _filteredWords = null;
        FilterAndDisplayWords();
    }

    private void FilterAndDisplayWords()
    {
        if (!_data.HasLearnedWords)
            return;

        FilterWords();
        BuildWordsDisplay();
    }

    private void FilterWords()
    {
        var searchTerms = GetSearchTerms(_searchFilter);

        if (searchTerms.Length == 0)
        {
            _filteredWords = _data.LearnedWords
                .Select(kvp => CreateWordEntry(kvp.Key, kvp.Value, 1))
                .OrderByDescending(entry => entry.Comprehension)
                .ThenBy(entry => entry.Word)
                .ToList();
            return;
        }

        var exactMatches = new List<WordEntry>();
        var otherMatches = new List<WordEntry>();

        // find words that exactly match search terms in order
        for (var i = 0; i < searchTerms.Length; i++)
        {
            var term = searchTerms[i];
            if (_data.LearnedWords.TryGetValue(term, out var comprehension))
            {
                exactMatches.Add(CreateWordEntry(term, comprehension, 1000 + (searchTerms.Length - i)));
            }
        }

        // find other matching words
        foreach (var kvp in _data.LearnedWords)
        {
            var word = kvp.Key;
            // skip if already added as exact match
            if (searchTerms.Contains(word.ToLowerInvariant()))
                continue;

            var relevance = CalculateSearchRelevance(word, searchTerms);
            if (relevance > 0)
            {
                otherMatches.Add(CreateWordEntry(word, kvp.Value, relevance));
            }
        }

        // sort other matches by relevance, then comprehension
        otherMatches = otherMatches
            .OrderByDescending(entry => entry.Relevance)
            .ThenByDescending(entry => entry.Comprehension)
            .ThenBy(entry => entry.Word)
            .ToList();

        _filteredWords = exactMatches.Concat(otherMatches).ToList();
    }

    private static string[] GetSearchTerms(string searchFilter)
    {
        return string.IsNullOrWhiteSpace(searchFilter)
            ? Array.Empty<string>()
            : searchFilter.ToLowerInvariant().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static int CalculateSearchRelevance(string word, string[] searchTerms)
    {
        if (searchTerms.Length == 0)
            return 1;

        var wordLower = word.ToLowerInvariant();
        var relevance = 0;

        foreach (var term in searchTerms)
        {
            relevance += wordLower switch
            {
                var w when w == term => 1000,
                var w when w.StartsWith(term) => 500,
                var w when w.EndsWith(term) => 300,
                var w when w.Contains(term) => 100 + (word.Length <= 10 ? 50 : 0),
                _ => 0
            };
        }

        return relevance;
    }

    private void BuildWordsDisplay()
    {
        if (_wordsContainer == null || _filteredWords == null)
            return;

        _wordsContainer.RemoveAllChildren();

        if (_filteredWords.Count == 0)
        {
            ShowNoResultsMessage();
            return;
        }

        CreateWordChips();
    }

    private void ShowNoResultsMessage()
    {
        var noResultsLabel = new Label
        {
            Text = "No words found matching your search.",
            FontColorOverride = Color.Gray,
            StyleClasses = { "LabelSubText" },
            Margin = new Thickness(0, 4, 0, 0)
        };
        _wordsContainer!.AddChild(noResultsLabel);
    }

    private void CreateWordChips()
    {
        var wordsPerRow = CalculateWordsPerRow();
        var currentRow = CreateNewRow();
        var wordsInCurrentRow = 0;

        foreach (var entry in _filteredWords!)
        {
            if (wordsInCurrentRow >= wordsPerRow)
            {
                _wordsContainer!.AddChild(currentRow);
                currentRow = CreateNewRow();
                wordsInCurrentRow = 0;
            }

            currentRow.AddChild(CreateWordChip(entry.DisplayWord, entry.Comprehension));
            wordsInCurrentRow++;
        }

        if (wordsInCurrentRow > 0)
            _wordsContainer!.AddChild(currentRow);
    }

    private static BoxContainer CreateNewRow()
    {
        return new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 2
        };
    }

    private int CalculateWordsPerRow()
    {
        var containerWidth = _wordsContainer?.Size.X ?? _wordsPanel?.Size.X ?? 300f;
        if (containerWidth <= 0) containerWidth = 300f;

        var availableWidth = Math.Max(containerWidth - 16, 150);
        var wordsPerRow = Math.Max(1, (int)(availableWidth / EstimatedChipWidth));

        return Math.Clamp(wordsPerRow, MinWordsPerRow, MaxWordsPerRow);
    }

    private WordEntry CreateWordEntry(string word, float comprehension, int relevance)
    {
        var displayWord = _languageLearning.ProcessWordForDisplay(
            word,
            _data.Prototype.ID,
            comprehension,
            _data.OverallProgress);

        return new WordEntry(word, displayWord, comprehension, relevance);
    }

    private static Control CreateWordChip(string word, float comprehension)
    {
        var colorIndex = GetComprehensionColorIndex(comprehension);

        var wordChip = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = ComprehensionBackgroundColors[colorIndex],
                BorderColor = ComprehensionColors[colorIndex],
                BorderThickness = new Thickness(1),
                ContentMarginTopOverride = 1,
                ContentMarginBottomOverride = 1,
                ContentMarginLeftOverride = 4,
                ContentMarginRightOverride = 4
            }
        };

        var wordContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            VerticalAlignment = Control.VAlignment.Center
        };

        var wordLabel = new Label
        {
            Text = word,
            FontColorOverride = ComprehensionColors[colorIndex],
            VerticalAlignment = Control.VAlignment.Center,
            Margin = new Thickness(0, 0, 2, 0)
        };

        var comprehensionLabel = new Label
        {
            Text = comprehension.ToString("P0"),
            FontColorOverride = ComprehensionColors[colorIndex],
            VerticalAlignment = Control.VAlignment.Center,
            StyleClasses = { "LabelSubText" }
        };

        wordContainer.AddChild(wordLabel);
        wordContainer.AddChild(comprehensionLabel);
        wordChip.AddChild(wordContainer);

        return wordChip;
    }

    private static int GetComprehensionColorIndex(float comprehension)
    {
        return comprehension switch
        {
            >= 0.8f => 4,
            >= 0.6f => 3,
            >= 0.4f => 2,
            >= 0.2f => 1,
            _ => 0
        };
    }

    private static StyleBox CreateHeaderStyle()
    {
        return new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#1B1B1E"),
            BorderColor = Color.FromHex("#404040"),
            BorderThickness = new Thickness(1),
            ContentMarginTopOverride = 8,
            ContentMarginBottomOverride = 8,
            ContentMarginLeftOverride = 8,
            ContentMarginRightOverride = 8
        };
    }

    private static StyleBox CreateWordsStyle()
    {
        return new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#2A2A2E"),
            BorderColor = Color.FromHex("#404040"),
            BorderThickness = new Thickness(1, 0, 1, 1),
            ContentMarginTopOverride = 8,
            ContentMarginBottomOverride = 8,
            ContentMarginLeftOverride = 8,
            ContentMarginRightOverride = 8
        };
    }

    private readonly struct WordEntry
    {
        public readonly string Word;
        public readonly string DisplayWord;
        public readonly float Comprehension;
        public readonly int Relevance;

        public WordEntry(string word, string displayWord, float comprehension, int relevance)
        {
            Word = word;
            DisplayWord = displayWord;
            Comprehension = comprehension;
            Relevance = relevance;
        }
    }

    private sealed class LanguageProgressData
    {
        public LanguagePrototype Prototype { get; private set; }
        public float OverallProgress { get; private set; }
        public int WordCount { get; private set; }
        public float AvgWordComprehension { get; private set; }
        public Dictionary<string, float> LearnedWords { get; private set; }

        public bool HasLearnedWords => LearnedWords.Count > 0;

        public LanguageProgressData(LanguagePrototype prototype, float overallProgress,
            int wordCount, float avgWordComprehension, Dictionary<string, float>? learnedWords)
        {
            Update(prototype, overallProgress, wordCount, avgWordComprehension, learnedWords);
        }

        public void Update(LanguagePrototype prototype, float overallProgress,
            int wordCount, float avgWordComprehension, Dictionary<string, float>? learnedWords)
        {
            Prototype = prototype;
            OverallProgress = overallProgress;
            WordCount = wordCount;
            AvgWordComprehension = avgWordComprehension;
            LearnedWords = learnedWords ?? new Dictionary<string, float>();
        }
    }
}
