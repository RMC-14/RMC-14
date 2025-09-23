using System.Linq;
using Content.Shared._RMC14.Language.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Systems.Language;

public sealed class LanguageLearningProgressControl : Control
{
    private LanguagePrototype _prototype;
    private float _overallProgress;
    private int _wordCount;
    private float _avgWordComprehension;
    private Dictionary<string, float>? _learnedWords;

    private BoxContainer? _wordsContainer;
    private Button? _expandButton;
    private bool _wordsExpanded = false;
    private Label? _statsLabel;
    private ProgressBar? _progressBar;
    private PanelContainer? _headerPanel;
    private PanelContainer? _wordsPanel;
    private LineEdit? _searchBox;
    private string _searchFilter = "";
    private List<(string word, float comprehension)>? _filteredWords;

    private readonly StyleBoxFlat _headerStyle = new()
    {
        BackgroundColor = Color.FromHex("#1B1B1E"),
        BorderColor = Color.FromHex("#404040"),
        BorderThickness = new Thickness(1),
        ContentMarginTopOverride = 8,
        ContentMarginBottomOverride = 8,
        ContentMarginLeftOverride = 8,
        ContentMarginRightOverride = 8
    };

    private readonly StyleBoxFlat _wordsStyle = new()
    {
        BackgroundColor = Color.FromHex("#2A2A2E"),
        BorderColor = Color.FromHex("#404040"),
        BorderThickness = new Thickness(1, 0, 1, 1),
        ContentMarginTopOverride = 8,
        ContentMarginBottomOverride = 8,
        ContentMarginLeftOverride = 8,
        ContentMarginRightOverride = 8
    };

    public string LanguageId { get; private set; }

    public event Action<string, bool>? OnExpansionChanged;

    public LanguageLearningProgressControl(LanguagePrototype prototype, float overallProgress, int wordCount, float avgWordComprehension, Dictionary<string, float>? learnedWords, string languageId)
    {
        _prototype = prototype;
        _overallProgress = overallProgress;
        _wordCount = wordCount;
        _avgWordComprehension = avgWordComprehension;
        _learnedWords = learnedWords;
        LanguageId = languageId;

        Logger.Info($"Creating LanguageLearningProgressControl for {prototype.ID}: wordCount={wordCount}, learnedWords count={learnedWords?.Count ?? -1}");

        HorizontalExpand = true;
        BuildUI();
    }

    public void UpdateData(LanguagePrototype prototype, float overallProgress, int wordCount, float avgWordComprehension, Dictionary<string, float>? learnedWords)
    {
        _prototype = prototype;
        _overallProgress = overallProgress;
        _wordCount = wordCount;
        _avgWordComprehension = avgWordComprehension;
        _learnedWords = learnedWords;

        _filteredWords = null;

        UpdateDisplayedData();

        if (_wordsExpanded && _wordsContainer != null)
        {
            BuildWordsContainer();
        }
    }

    private void UpdateDisplayedData()
    {
        if (_statsLabel != null)
        {
            var progressText = $"Overall: {_overallProgress:P1} | Words: {_wordCount} | Avg: {_avgWordComprehension:P1}";
            _statsLabel.Text = progressText;
        }

        if (_progressBar != null)
        {
            _progressBar.Value = _overallProgress;
        }

        UpdateExpandButtonVisibility();
    }

    private void UpdateExpandButtonVisibility()
    {
        var shouldShowButton = _learnedWords != null && _learnedWords.Count > 0;

        if (_expandButton != null)
        {
            _expandButton.Visible = shouldShowButton;
        }
        else if (shouldShowButton && _headerPanel != null)
        {
            CreateExpandButton();
        }
    }

    private void CreateExpandButton()
    {
        if (_expandButton != null) return;

        _expandButton = new Button
        {
            Text = "►",
            SetWidth = 24,
            SetHeight = 24,
            VerticalAlignment = Control.VAlignment.Center,
            ToggleMode = false
        };

        _expandButton.OnPressed += args => OnExpandButtonPressed();

        if (_headerPanel?.Children.FirstOrDefault() is BoxContainer headerBox)
        {
            headerBox.AddChild(_expandButton);
        }
    }

    public void SetExpanded(bool expanded)
    {
        Logger.Info($"SetExpanded called for {_prototype.ID}: expanded={expanded}, current={_wordsExpanded}");

        if (_wordsExpanded == expanded)
            return;

        _wordsExpanded = expanded;

        if (_wordsPanel != null)
        {
            _wordsPanel.Visible = _wordsExpanded;

            if (_wordsExpanded && _learnedWords != null && _learnedWords.Count > 0)
            {
                if (_searchBox != null && !string.IsNullOrEmpty(_searchBox.Text))
                {
                    _searchBox.Text = "";
                    _searchFilter = "";
                }
                BuildWordsContainer();
            }
        }

        if (_expandButton != null)
        {
            _expandButton.Text = _wordsExpanded ? "▼" : "►";
        }

        OnExpansionChanged?.Invoke(LanguageId, _wordsExpanded);
    }

    private void BuildUI()
    {
        var mainContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true
        };

        _headerPanel = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = _headerStyle
        };

        var headerBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true
        };

        var icon = new TextureRect
        {
            SetWidth = 32,
            SetHeight = 32,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            Margin = new Thickness(0, 0, 8, 0)
        };

        if (!string.IsNullOrEmpty(_prototype.LanguageIcon))
        {
            var resourceCache = IoCManager.Resolve<IResourceCache>();
            if (resourceCache.TryGetResource<TextureResource>(_prototype.LanguageIcon, out var texture))
            {
                icon.Texture = texture;
            }
        }

        headerBox.AddChild(icon);

        var infoContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalAlignment = Control.VAlignment.Center
        };

        var nameLabel = new Label
        {
            Text = _prototype.LocalizedName,
            FontColorOverride = _prototype.TextColor ?? Color.White,
            StyleClasses = { "LabelHeading" }
        };

        var progressText = $"Overall: {_overallProgress:P1} | Words: {_wordCount} | Avg: {_avgWordComprehension:P1}";
        _statsLabel = new Label
        {
            Text = progressText,
            FontColorOverride = Color.LightGray,
            StyleClasses = { "LabelSubText" }
        };

        _progressBar = new ProgressBar
        {
            Value = _overallProgress,
            HorizontalExpand = true,
            SetHeight = 8,
            Margin = new Thickness(0, 2, 0, 0)
        };

        infoContainer.AddChild(nameLabel);
        infoContainer.AddChild(_statsLabel);
        infoContainer.AddChild(_progressBar);
        headerBox.AddChild(infoContainer);

        _headerPanel.AddChild(headerBox);
        mainContainer.AddChild(_headerPanel);

        _wordsPanel = new PanelContainer
        {
            HorizontalExpand = true,
            SetHeight = 180,
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
            SetHeight = 24,
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
        mainContainer.AddChild(_wordsPanel);

        AddChild(mainContainer);
        Logger.Info($"UI building complete for {_prototype.ID}");
    }

    private void OnSearchTextChanged(LineEdit.LineEditEventArgs args)
    {
        _searchFilter = args.Text.Trim();
        FilterWords();
    }

    private void FilterWords()
    {
        if (_learnedWords == null || _learnedWords.Count == 0)
            return;

        var allWords = _learnedWords
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key)
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();

        if (string.IsNullOrWhiteSpace(_searchFilter))
        {
            _filteredWords = allWords;
        }
        else
        {
            var searchTerms = _searchFilter.ToLowerInvariant()
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            _filteredWords = allWords.Where(tuple =>
                searchTerms.Any(term => tuple.Key.ToLowerInvariant().Contains(term))
            ).ToList();
        }

        BuildWordsDisplay();
    }

    private int CalculateWordsPerRow()
    {
        var containerWidth = _wordsContainer?.Size.X ?? _wordsPanel?.Size.X ?? 300f;

        if (containerWidth <= 0)
            containerWidth = 300f;

        var availableWidth = Math.Max(containerWidth - 16, 150);
        var estimatedChipWidth = 90f;
        var wordsPerRow = Math.Max(1, (int)(availableWidth / estimatedChipWidth));

        return Math.Clamp(wordsPerRow, 2, 10);
    }

    protected override void Resized()
    {
        base.Resized();

        if (_wordsExpanded && _filteredWords != null && _filteredWords.Count > 0)
        {
            BuildWordsDisplay();
        }
    }

    private void OnExpandButtonPressed()
    {
        Logger.Info($"Expand button pressed for {_prototype.ID}");
        SetExpanded(!_wordsExpanded);
    }

    private void BuildWordsContainer()
    {
        Logger.Info($"BuildWordsContainer called for {_prototype.ID}");

        if (_wordsContainer == null || _learnedWords == null || _learnedWords.Count == 0)
        {
            Logger.Info($"BuildWordsContainer early return: container null={_wordsContainer == null}, learnedWords null={_learnedWords == null}, count={_learnedWords?.Count ?? -1}");
            return;
        }

        Logger.Info($"Building words container with {_learnedWords.Count} words");

        if (_filteredWords == null)
        {
            FilterWords();
            return;
        }

        BuildWordsDisplay();
    }

    private void BuildWordsDisplay()
    {
        if (_wordsContainer == null || _filteredWords == null)
            return;

        _wordsContainer.RemoveAllChildren();

        if (_filteredWords.Count == 0)
        {
            var noResultsLabel = new Label
            {
                Text = "No words found matching your search.",
                FontColorOverride = Color.Gray,
                StyleClasses = { "LabelSubText" },
                Margin = new Thickness(0, 4, 0, 0)
            };
            _wordsContainer.AddChild(noResultsLabel);
            return;
        }

        var wordsPerRow = CalculateWordsPerRow();

        var currentRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 2
        };

        int wordsInCurrentRow = 0;

        foreach (var (word, comprehension) in _filteredWords)
        {
            var wordChip = CreateWordChip(word, comprehension);
            currentRow.AddChild(wordChip);
            wordsInCurrentRow++;

            if (wordsInCurrentRow >= wordsPerRow)
            {
                _wordsContainer.AddChild(currentRow);
                currentRow = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    HorizontalExpand = true,
                    SeparationOverride = 2
                };
                wordsInCurrentRow = 0;
            }
        }

        if (wordsInCurrentRow > 0)
        {
            _wordsContainer.AddChild(currentRow);
        }

        Logger.Info($"Finished building words display with {_filteredWords.Count} word entries using {wordsPerRow} words per row");
    }

    private Control CreateWordChip(string word, float comprehension)
    {
        var wordChip = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = GetComprehensionBackgroundColor(comprehension),
                BorderColor = GetComprehensionColor(comprehension),
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
            FontColorOverride = GetComprehensionColor(comprehension),
            VerticalAlignment = Control.VAlignment.Center,
            Margin = new Thickness(0, 0, 2, 0)
        };

        var comprehensionLabel = new Label
        {
            Text = comprehension.ToString("P0"),
            FontColorOverride = GetComprehensionColor(comprehension),
            VerticalAlignment = Control.VAlignment.Center,
            StyleClasses = { "LabelSubText" }
        };

        wordContainer.AddChild(wordLabel);
        wordContainer.AddChild(comprehensionLabel);
        wordChip.AddChild(wordContainer);

        return wordChip;
    }

    private Color GetComprehensionColor(float comprehension)
    {
        if (comprehension >= 0.8f) return Color.LightGreen;
        if (comprehension >= 0.6f) return Color.Yellow;
        if (comprehension >= 0.4f) return Color.Orange;
        if (comprehension >= 0.2f) return Color.Red;
        return Color.DarkRed;
    }

    private Color GetComprehensionBackgroundColor(float comprehension)
    {
        if (comprehension >= 0.8f) return Color.FromHex("#1B2F1B");
        if (comprehension >= 0.6f) return Color.FromHex("#2F2F1B");
        if (comprehension >= 0.4f) return Color.FromHex("#2F251B");
        if (comprehension >= 0.2f) return Color.FromHex("#2F1B1B");
        return Color.FromHex("#1F1B1B");
    }

    public void SetThemeColor(Color themeColor)
    {
        var hsv = Color.ToHsv(themeColor);
        var darkerTheme = Color.FromHsv(hsv with { Z = hsv.Z * 0.8f });

        _headerStyle.BackgroundColor = themeColor;
        _wordsStyle.BackgroundColor = darkerTheme;
    }
}
