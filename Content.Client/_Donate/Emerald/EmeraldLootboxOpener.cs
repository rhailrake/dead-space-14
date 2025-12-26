// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;
using Content.Shared._Donate;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._Donate.Emerald;

public sealed class EmeraldLootboxOpener : Control
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private const int TitleFontSize = 14;
    private const int NameFontSize = 12;
    private const int RarityFontSize = 10;

    private const float CardWidth = 100f;
    private const float CardHeight = 130f;
    private const float CardSpacing = 8f;
    private const float StripHeight = 160f;

    private const int TotalScrollCards = 50;

    private Font _titleFont = default!;
    private Font _nameFont = default!;
    private Font _rarityFont = default!;

    private readonly Color _bgColor = Color.FromHex("#0f0a1e");
    private readonly Color _stripBgColor = Color.FromHex("#1a0f2e");
    private readonly Color _borderColor = Color.FromHex("#4a3a6a");
    private readonly Color _indicatorColor = Color.FromHex("#00FFAA");
    private readonly Color _textColor = Color.FromHex("#c0b3da");

    private readonly Color _commonColor = Color.FromHex("#9e9e9e");
    private readonly Color _commonBgColor = Color.FromHex("#2a2a2a");
    private readonly Color _epicColor = Color.FromHex("#9c27b0");
    private readonly Color _epicBgColor = Color.FromHex("#2a1a3a");
    private readonly Color _mythicColor = Color.FromHex("#e91e63");
    private readonly Color _mythicBgColor = Color.FromHex("#3a1a2a");
    private readonly Color _legendaryColor = Color.FromHex("#ffd700");
    private readonly Color _legendaryBgColor = Color.FromHex("#3a2a1a");

    private OpenerState _state = OpenerState.Initial;
    private string _lootboxName = "";
    private int _userItemId;
    private bool _stelsHidden;

    private LootboxOpenResult? _result;
    private List<LootboxRarity> _scrollCards = new();
    private int _winningCardIndex;

    private float _scrollOffset;
    private float _targetScrollOffset;
    private float _scrollVelocity;
    private float _animationTime;
    private float _revealScale = 1f;
    private float _revealAlpha = 1f;

    private EmeraldButton _openButton = default!;
    private EmeraldButton _closeButton = default!;
    private EmeraldLabel _statusLabel = default!;
    private EmeraldLabel _titleLabel = default!;
    private EmeraldLabel _resultLabel = default!;

    public event Action<int, bool>? OnOpenRequested;
    public event Action? OnCloseRequested;

    private enum OpenerState
    {
        Initial,
        WaitingResult,
        Scrolling,
        Revealing,
        Complete
    }

    public EmeraldLootboxOpener()
    {
        IoCManager.InjectDependencies(this);

        var fontRes = _resourceCache.GetResource<FontResource>("/Fonts/Bedstead/Bedstead.otf");
        _titleFont = new VectorFont(fontRes, TitleFontSize);
        _nameFont = new VectorFont(fontRes, NameFontSize);
        _rarityFont = new VectorFont(fontRes, RarityFontSize);

        BuildUI();
    }

    private void BuildUI()
    {
        _titleLabel = new EmeraldLabel
        {
            Text = "ОТКРЫТИЕ ЛУТБОКСА",
            Alignment = EmeraldLabel.TextAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
            TextColor = _legendaryColor
        };
        AddChild(_titleLabel);

        _statusLabel = new EmeraldLabel
        {
            Text = "",
            Alignment = EmeraldLabel.TextAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
            TextColor = _textColor
        };
        AddChild(_statusLabel);

        _resultLabel = new EmeraldLabel
        {
            Text = "",
            Alignment = EmeraldLabel.TextAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
            TextColor = _indicatorColor,
            Visible = false
        };
        AddChild(_resultLabel);

        _openButton = new EmeraldButton
        {
            Text = "ОТКРЫТЬ",
            MinSize = new Vector2(140, 36)
        };
        _openButton.OnPressed += OnOpenPressed;
        AddChild(_openButton);

        _closeButton = new EmeraldButton
        {
            Text = "ЗАБРАТЬ",
            MinSize = new Vector2(140, 36),
            Visible = false
        };
        _closeButton.OnPressed += () => OnCloseRequested?.Invoke();
        AddChild(_closeButton);
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        _titleLabel.Measure(availableSize);
        _statusLabel.Measure(availableSize);
        _resultLabel.Measure(availableSize);
        _openButton.Measure(availableSize);
        _closeButton.Measure(availableSize);

        return new Vector2(
            float.IsPositiveInfinity(availableSize.X) ? 700 : availableSize.X,
            350
        );
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var titleY = 10f;
        _titleLabel.Arrange(new UIBox2(0, titleY, finalSize.X, titleY + _titleLabel.DesiredSize.Y));

        var stripY = 50f;
        var stripBottom = stripY + StripHeight;

        var statusY = stripBottom + 15f;
        _statusLabel.Arrange(new UIBox2(0, statusY, finalSize.X, statusY + _statusLabel.DesiredSize.Y));

        var resultY = statusY + 25f;
        _resultLabel.Arrange(new UIBox2(0, resultY, finalSize.X, resultY + _resultLabel.DesiredSize.Y));

        var buttonY = finalSize.Y - 50f;
        var buttonX = (finalSize.X - _openButton.DesiredSize.X) / 2f;
        _openButton.Arrange(new UIBox2(buttonX, buttonY, buttonX + _openButton.DesiredSize.X, buttonY + _openButton.DesiredSize.Y));

        var closeX = (finalSize.X - _closeButton.DesiredSize.X) / 2f;
        _closeButton.Arrange(new UIBox2(closeX, buttonY, closeX + _closeButton.DesiredSize.X, buttonY + _closeButton.DesiredSize.Y));

        return finalSize;
    }

    public void SetLootbox(string name, int userItemId, bool stelsHidden)
    {
        _lootboxName = name;
        _userItemId = userItemId;
        _stelsHidden = stelsHidden;
        _state = OpenerState.Initial;
        _result = null;
        _scrollCards.Clear();
        _scrollOffset = 0;
        _animationTime = 0;

        _titleLabel.Text = $"ОТКРЫТИЕ: {name.ToUpper()}";
        _statusLabel.Text = "Нажмите ОТКРЫТЬ чтобы испытать удачу";
        _resultLabel.Visible = false;
        _resultLabel.Text = "";

        _openButton.Visible = true;
        _openButton.Disabled = false;
        _closeButton.Visible = false;

        GeneratePreviewCards();
    }

    private void GeneratePreviewCards()
    {
        _scrollCards.Clear();
        var random = new Random(42);

        for (var i = 0; i < 15; i++)
        {
            var r = random.NextSingle();
            LootboxRarity rarity;
            if (r < 0.5f)
                rarity = LootboxRarity.Common;
            else if (r < 0.75f)
                rarity = LootboxRarity.Epic;
            else if (r < 0.9f)
                rarity = LootboxRarity.Mythic;
            else
                rarity = LootboxRarity.Legendary;

            _scrollCards.Add(rarity);
        }
    }

    private void OnOpenPressed()
    {
        if (_state != OpenerState.Initial)
            return;

        _state = OpenerState.WaitingResult;
        _openButton.Disabled = true;
        _statusLabel.Text = "Открываем...";

        OnOpenRequested?.Invoke(_userItemId, _stelsHidden);
    }

    public void HandleOpenResult(LootboxOpenResult result)
    {
        _result = result;

        if (!result.Success)
        {
            _state = OpenerState.Complete;
            _statusLabel.Text = result.Message;
            _statusLabel.TextColor = Color.FromHex("#ff6b6b");
            _openButton.Visible = false;
            _closeButton.Visible = true;
            return;
        }

        var winRarity = result.Item?.Rarity ?? LootboxRarity.Common;

        if (result.StelsOpen && result.Sequence != null && result.Sequence.Count > 0)
        {
            GenerateScrollCards(result.Sequence, winRarity);
            StartScrollAnimation();
        }
        else
        {
            GenerateScrollCardsInstant(winRarity);
            ShowInstantResult();
        }
    }

    private void GenerateScrollCards(List<LootboxRarity> sequence, LootboxRarity winRarity)
    {
        _scrollCards.Clear();
        var random = new Random();

        foreach (var rarity in sequence)
        {
            _scrollCards.Add(rarity);
        }

        while (_scrollCards.Count < TotalScrollCards)
        {
            var r = random.NextSingle();
            LootboxRarity rarity;
            if (r < 0.5f)
                rarity = LootboxRarity.Common;
            else if (r < 0.75f)
                rarity = LootboxRarity.Epic;
            else if (r < 0.9f)
                rarity = LootboxRarity.Mythic;
            else
                rarity = LootboxRarity.Legendary;

            _scrollCards.Add(rarity);
        }

        _winningCardIndex = TotalScrollCards - 8 + random.Next(5);
        if (_winningCardIndex >= _scrollCards.Count)
            _winningCardIndex = _scrollCards.Count - 3;

        _scrollCards[_winningCardIndex] = winRarity;
    }

    private void GenerateScrollCardsInstant(LootboxRarity winRarity)
    {
        _scrollCards.Clear();
        var random = new Random();

        for (var i = 0; i < 15; i++)
        {
            var r = random.NextSingle();
            LootboxRarity rarity;
            if (r < 0.5f)
                rarity = LootboxRarity.Common;
            else if (r < 0.75f)
                rarity = LootboxRarity.Epic;
            else if (r < 0.9f)
                rarity = LootboxRarity.Mythic;
            else
                rarity = LootboxRarity.Legendary;

            _scrollCards.Add(rarity);
        }

        _winningCardIndex = 7;
        _scrollCards[_winningCardIndex] = winRarity;
    }

    private void StartScrollAnimation()
    {
        _state = OpenerState.Scrolling;
        _animationTime = 0;
        _scrollOffset = 0;

        var cardFullWidth = CardWidth + CardSpacing;
        var centerOffset = Size.X / 2f - CardWidth / 2f;
        _targetScrollOffset = _winningCardIndex * cardFullWidth - centerOffset;

        _scrollVelocity = 2000f;
        _statusLabel.Text = "";
    }

    private void ShowInstantResult()
    {
        _state = OpenerState.Revealing;
        _animationTime = 0;
        _revealScale = 0.3f;
        _revealAlpha = 0f;

        var cardFullWidth = CardWidth + CardSpacing;
        var centerOffset = Size.X / 2f - CardWidth / 2f;
        _scrollOffset = _winningCardIndex * cardFullWidth - centerOffset;

        _statusLabel.Text = "";
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        switch (_state)
        {
            case OpenerState.Scrolling:
                UpdateScrollAnimation(args.DeltaSeconds);
                break;

            case OpenerState.Revealing:
                UpdateRevealAnimation(args.DeltaSeconds);
                break;
        }
    }

    private void UpdateScrollAnimation(float delta)
    {
        _animationTime += delta;

        var remaining = _targetScrollOffset - _scrollOffset;

        if (remaining > 1f)
        {
            var progress = Math.Min(1f, _scrollOffset / _targetScrollOffset);
            var easeOut = 1f - MathF.Pow(1f - progress, 3f);
            var speedFactor = 1f - easeOut * 0.95f;
            var currentVelocity = Math.Max(30f, _scrollVelocity * speedFactor);

            _scrollOffset += currentVelocity * delta;

            if (_scrollOffset >= _targetScrollOffset - 0.5f)
            {
                _scrollOffset = _targetScrollOffset;
                FinishScrollAnimation();
            }
        }
        else
        {
            _scrollOffset = _targetScrollOffset;
            FinishScrollAnimation();
        }
    }

    private void FinishScrollAnimation()
    {
        _state = OpenerState.Revealing;
        _animationTime = 0;
        _revealScale = 1f;
        _revealAlpha = 1f;
    }

    private void UpdateRevealAnimation(float delta)
    {
        _animationTime += delta;

        _revealScale = Math.Min(1.15f, 0.3f + _animationTime * 2f);
        _revealAlpha = Math.Min(1f, _animationTime * 3f);

        if (_animationTime > 0.6f && _state == OpenerState.Revealing)
        {
            _state = OpenerState.Complete;
            _revealScale = 1.1f;
            ShowFinalResult();
        }
    }

    private void ShowFinalResult()
    {
        if (_result?.Item != null)
        {
            var rarityName = GetRarityName(_result.Item.Rarity);
            var rarityColor = GetRarityColor(_result.Item.Rarity);
            _statusLabel.Text = "ПОЗДРАВЛЯЕМ!";
            _statusLabel.TextColor = _indicatorColor;

            _resultLabel.Visible = true;
            _resultLabel.Text = $"{rarityName}: {_result.Item.Name.ToUpper()}";
            _resultLabel.TextColor = rarityColor;
        }
        else
        {
            _statusLabel.Text = "Награда получена!";
            _statusLabel.TextColor = _indicatorColor;
        }

        _openButton.Visible = false;
        _closeButton.Visible = true;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var stripY = 50f * UIScale;
        var stripHeight = StripHeight * UIScale;
        var stripRect = new UIBox2(0, stripY, PixelSize.X, stripY + stripHeight);

        handle.DrawRect(stripRect, _stripBgColor);

        var borderThickness = 2f * UIScale;
        handle.DrawRect(new UIBox2(stripRect.Left, stripRect.Top, stripRect.Right, stripRect.Top + borderThickness), _borderColor);
        handle.DrawRect(new UIBox2(stripRect.Left, stripRect.Bottom - borderThickness, stripRect.Right, stripRect.Bottom), _borderColor);

        var cardWidth = CardWidth * UIScale;
        var cardHeight = CardHeight * UIScale;
        var spacing = CardSpacing * UIScale;
        var cardFullWidth = cardWidth + spacing;

        var centerX = PixelSize.X / 2f;
        var cardY = stripY + (stripHeight - cardHeight) / 2f;

        var scaledScrollOffset = _scrollOffset * UIScale;

        if (_scrollCards.Count > 0)
        {
            var startIndex = (int)(scaledScrollOffset / cardFullWidth) - 6;
            var endIndex = startIndex + 14;

            startIndex = Math.Max(0, startIndex);
            endIndex = Math.Min(_scrollCards.Count, endIndex);

            for (var i = startIndex; i < endIndex; i++)
            {
                var cardX = i * cardFullWidth - scaledScrollOffset + centerX - cardWidth / 2f;

                if (cardX + cardWidth < -50 || cardX > PixelSize.X + 50)
                    continue;

                var distanceFromCenter = Math.Abs(cardX + cardWidth / 2f - centerX);
                var maxDistance = PixelSize.X / 2f;
                var alpha = 1f - Math.Min(0.6f, distanceFromCenter / maxDistance * 0.6f);

                var isWinningCard = i == _winningCardIndex && (_state == OpenerState.Complete || _state == OpenerState.Revealing);
                var scale = isWinningCard ? _revealScale : 1f;
                var cardAlpha = isWinningCard ? Math.Max(alpha, _revealAlpha) : alpha;

                if (scale != 1f)
                {
                    var scaledWidth = cardWidth * scale;
                    var scaledHeight = cardHeight * scale;
                    var offsetX = (scaledWidth - cardWidth) / 2f;
                    var offsetY = (scaledHeight - cardHeight) / 2f;
                    DrawCard(handle, cardX - offsetX, cardY - offsetY, scaledWidth, scaledHeight, _scrollCards[i], cardAlpha, isWinningCard);
                }
                else
                {
                    DrawCard(handle, cardX, cardY, cardWidth, cardHeight, _scrollCards[i], cardAlpha, isWinningCard);
                }
            }
        }

        var indicatorWidth = 4f * UIScale;
        var indicatorX = centerX - indicatorWidth / 2f;
        var triangleSize = 12f * UIScale;

        var topTriangle = new Vector2[]
        {
            new(indicatorX + indicatorWidth / 2f, stripRect.Top + triangleSize),
            new(indicatorX - triangleSize / 2f + indicatorWidth / 2f, stripRect.Top),
            new(indicatorX + triangleSize / 2f + indicatorWidth / 2f, stripRect.Top)
        };

        var bottomTriangle = new Vector2[]
        {
            new(indicatorX + indicatorWidth / 2f, stripRect.Bottom - triangleSize),
            new(indicatorX - triangleSize / 2f + indicatorWidth / 2f, stripRect.Bottom),
            new(indicatorX + triangleSize / 2f + indicatorWidth / 2f, stripRect.Bottom)
        };

        handle.DrawLine(topTriangle[0], topTriangle[1], _indicatorColor);
        handle.DrawLine(topTriangle[1], topTriangle[2], _indicatorColor);
        handle.DrawLine(topTriangle[2], topTriangle[0], _indicatorColor);

        handle.DrawLine(bottomTriangle[0], bottomTriangle[1], _indicatorColor);
        handle.DrawLine(bottomTriangle[1], bottomTriangle[2], _indicatorColor);
        handle.DrawLine(bottomTriangle[2], bottomTriangle[0], _indicatorColor);

        handle.DrawRect(new UIBox2(indicatorX, stripRect.Top, indicatorX + indicatorWidth, stripRect.Top + triangleSize), _indicatorColor);
        handle.DrawRect(new UIBox2(indicatorX, stripRect.Bottom - triangleSize, indicatorX + indicatorWidth, stripRect.Bottom), _indicatorColor);
    }

    private void DrawCard(DrawingHandleScreen handle, float x, float y, float width, float height, LootboxRarity rarity, float alpha, bool highlight = false)
    {
        var (bgColor, borderColor) = GetRarityColors(rarity);
        var rect = new UIBox2(x, y, x + width, y + height);

        handle.DrawRect(rect, bgColor.WithAlpha(alpha * 0.9f));

        var borderThickness = highlight ? 3f * UIScale : 2f * UIScale;
        var bc = borderColor.WithAlpha(alpha);

        handle.DrawRect(new UIBox2(rect.Left, rect.Top, rect.Right, rect.Top + borderThickness), bc);
        handle.DrawRect(new UIBox2(rect.Left, rect.Bottom - borderThickness, rect.Right, rect.Bottom), bc);
        handle.DrawRect(new UIBox2(rect.Left, rect.Top, rect.Left + borderThickness, rect.Bottom), bc);
        handle.DrawRect(new UIBox2(rect.Right - borderThickness, rect.Top, rect.Right, rect.Bottom), bc);

        if (highlight)
        {
            var glowOffset = 4f * UIScale;
            var glowRect = new UIBox2(rect.Left - glowOffset, rect.Top - glowOffset, rect.Right + glowOffset, rect.Bottom + glowOffset);
            var glowThickness = 2f * UIScale;
            var gc = borderColor.WithAlpha(alpha * 0.4f);

            handle.DrawRect(new UIBox2(glowRect.Left, glowRect.Top, glowRect.Right, glowRect.Top + glowThickness), gc);
            handle.DrawRect(new UIBox2(glowRect.Left, glowRect.Bottom - glowThickness, glowRect.Right, glowRect.Bottom), gc);
            handle.DrawRect(new UIBox2(glowRect.Left, glowRect.Top, glowRect.Left + glowThickness, glowRect.Bottom), gc);
            handle.DrawRect(new UIBox2(glowRect.Right - glowThickness, glowRect.Top, glowRect.Right, glowRect.Bottom), gc);
        }

        var gemSize = 24f * UIScale;
        var gemX = x + (width - gemSize) / 2f;
        var gemY = y + 20f * UIScale;
        var gemCenterX = gemX + gemSize / 2f;
        var gemCenterY = gemY + gemSize / 2f;
        var gemColor = borderColor.WithAlpha(alpha);

        handle.DrawLine(new Vector2(gemCenterX, gemY), new Vector2(gemX + gemSize, gemCenterY), gemColor);
        handle.DrawLine(new Vector2(gemX + gemSize, gemCenterY), new Vector2(gemCenterX, gemY + gemSize), gemColor);
        handle.DrawLine(new Vector2(gemCenterX, gemY + gemSize), new Vector2(gemX, gemCenterY), gemColor);
        handle.DrawLine(new Vector2(gemX, gemCenterY), new Vector2(gemCenterX, gemY), gemColor);
        handle.DrawLine(new Vector2(gemX, gemCenterY), new Vector2(gemX + gemSize, gemCenterY), gemColor);
        handle.DrawLine(new Vector2(gemCenterX, gemY), new Vector2(gemCenterX, gemY + gemSize), gemColor);

        var rarityName = GetRarityName(rarity);
        var textWidth = GetTextWidth(rarityName, _rarityFont);
        var textX = x + (width - textWidth) / 2f;
        var textY = y + height - 35f * UIScale;

        handle.DrawString(_rarityFont, new Vector2(textX, textY), rarityName, UIScale, borderColor.WithAlpha(alpha));
    }

    private (Color bg, Color border) GetRarityColors(LootboxRarity rarity)
    {
        return rarity switch
        {
            LootboxRarity.Common => (_commonBgColor, _commonColor),
            LootboxRarity.Epic => (_epicBgColor, _epicColor),
            LootboxRarity.Mythic => (_mythicBgColor, _mythicColor),
            LootboxRarity.Legendary => (_legendaryBgColor, _legendaryColor),
            _ => (_commonBgColor, _commonColor)
        };
    }

    private string GetRarityName(LootboxRarity rarity)
    {
        return rarity switch
        {
            LootboxRarity.Common => "ОБЫЧНЫЙ",
            LootboxRarity.Epic => "ЭПИЧЕСКИЙ",
            LootboxRarity.Mythic => "МИФИЧЕСКИЙ",
            LootboxRarity.Legendary => "ЛЕГЕНДА",
            _ => "ОБЫЧНЫЙ"
        };
    }

    private Color GetRarityColor(LootboxRarity rarity)
    {
        return rarity switch
        {
            LootboxRarity.Common => _commonColor,
            LootboxRarity.Epic => _epicColor,
            LootboxRarity.Mythic => _mythicColor,
            LootboxRarity.Legendary => _legendaryColor,
            _ => _commonColor
        };
    }

    private float GetTextWidth(string text, Font font)
    {
        if (string.IsNullOrEmpty(text))
            return 0f;

        var width = 0f;
        foreach (var rune in text.EnumerateRunes())
        {
            var metrics = font.GetCharMetrics(rune, UIScale);
            if (metrics.HasValue)
                width += metrics.Value.Advance;
        }
        return width;
    }
}
