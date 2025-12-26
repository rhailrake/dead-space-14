// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;
using Content.Shared._Donate;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Donate.Emerald;

public sealed class EmeraldLootboxOpenWindow : EmeraldDefaultWindow
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private const int TitleFontSize = 16;
    private const int NameFontSize = 14;
    private const int RarityFontSize = 12;
    private const int MessageFontSize = 10;

    private Font _titleFont = default!;
    private Font _nameFont = default!;
    private Font _rarityFont = default!;
    private Font _messageFont = default!;

    private Texture? _commonTexture;
    private Texture? _epicTexture;
    private Texture? _mythicTexture;
    private Texture? _legendaryTexture;

    private readonly Color _bgColor = Color.FromHex("#0f0a1e");
    private readonly Color _commonColor = Color.FromHex("#9e9e9e");
    private readonly Color _epicColor = Color.FromHex("#9c27b0");
    private readonly Color _mythicColor = Color.FromHex("#e91e63");
    private readonly Color _legendaryColor = Color.FromHex("#ffd700");
    private readonly Color _textColor = Color.FromHex("#c0b3da");

    private LootboxOpenState _state = LootboxOpenState.Initial;
    private string _lootboxName = "";
    private int _userItemId;
    private bool _stelsHidden;

    private LootboxOpenResult? _result;
    private float _animationTime;
    private int _currentScrollIndex;
    private float _scrollSpeed = 8f;
    private float _scrollDeceleration = 0.97f;
    private List<LootboxRarity> _scrollSequence = new();
    private bool _animationComplete;
    private float _revealAnimationTime;
    private float _cardScale = 1f;

    private readonly List<Particle> _particles = new();
    private float _particleTime;

    private BoxContainer _mainContainer = default!;
    private Control _cardContainer = default!;
    private TextureRect _currentCardTexture = default!;
    private EmeraldButton _openButton = default!;
    private EmeraldButton _claimButton = default!;
    private EmeraldButton _closeButton = default!;
    private EmeraldLabel _statusLabel = default!;
    private EmeraldLabel _rarityLabel = default!;
    private EmeraldLabel _itemNameLabel = default!;
    private SpriteView? _itemSpriteView;
    private Control _spriteContainer = default!;

    public event Action<int, bool>? OnOpenRequested;
    public event Action? OnCloseRequested;

    private enum LootboxOpenState
    {
        Initial,
        WaitingResult,
        Scrolling,
        Revealing,
        Complete
    }

    public EmeraldLootboxOpenWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = "ОТКРЫТИЕ ЛУТБОКСА";
        MinSize = SetSize = new Vector2(400, 548);

        var fontRes = _resourceCache.GetResource<FontResource>("/Fonts/Bedstead/Bedstead.otf");
        _titleFont = new VectorFont(fontRes, TitleFontSize);
        _nameFont = new VectorFont(fontRes, NameFontSize);
        _rarityFont = new VectorFont(fontRes, RarityFontSize);
        _messageFont = new VectorFont(fontRes, MessageFontSize);

        LoadTextures();
        BuildUI();
    }

    private void LoadTextures()
    {
        try
        {
            _commonTexture = _resourceCache.GetResource<TextureResource>("/Textures/Interface/common.png").Texture;
        }
        catch { _commonTexture = null; }

        try
        {
            _epicTexture = _resourceCache.GetResource<TextureResource>("/Textures/Interface/epic.png").Texture;
        }
        catch { _epicTexture = null; }

        try
        {
            _mythicTexture = _resourceCache.GetResource<TextureResource>("/Textures/Interface/mythic.png").Texture;
        }
        catch { _mythicTexture = null; }

        try
        {
            _legendaryTexture = _resourceCache.GetResource<TextureResource>("/Textures/Interface/legendary.png").Texture;
        }
        catch { _legendaryTexture = null; }
    }

    private void BuildUI()
    {
        _mainContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            HorizontalAlignment = HAlignment.Center,
            SeparationOverride = 12
        };

        _cardContainer = new Control
        {
            MinSize = new Vector2(200, 280),
            HorizontalAlignment = HAlignment.Center
        };

        _currentCardTexture = new TextureRect
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Stretch = TextureRect.StretchMode.KeepAspectCovered,
            Texture = _commonTexture
        };
        _cardContainer.AddChild(_currentCardTexture);

        _spriteContainer = new Control
        {
            MinSize = new Vector2(80, 80),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Visible = false
        };

        _itemSpriteView = new SpriteView(_entMan)
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Stretch = SpriteView.StretchMode.Fit,
            Scale = new Vector2(2.5f, 2.5f)
        };
        _spriteContainer.AddChild(_itemSpriteView);
        _cardContainer.AddChild(_spriteContainer);

        _mainContainer.AddChild(_cardContainer);

        _rarityLabel = new EmeraldLabel
        {
            Text = "",
            Alignment = EmeraldLabel.TextAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
            TextColor = _textColor
        };
        _mainContainer.AddChild(_rarityLabel);

        _itemNameLabel = new EmeraldLabel
        {
            Text = "",
            Alignment = EmeraldLabel.TextAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
            TextColor = _textColor
        };
        _mainContainer.AddChild(_itemNameLabel);

        _statusLabel = new EmeraldLabel
        {
            Text = "Нажмите \"Открыть\" чтобы начать",
            Alignment = EmeraldLabel.TextAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
            TextColor = _textColor
        };
        _mainContainer.AddChild(_statusLabel);

        var buttonsContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            SeparationOverride = 16
        };

        _openButton = new EmeraldButton
        {
            Text = "ОТКРЫТЬ",
            MinSize = new Vector2(140, 40)
        };
        _openButton.OnPressed += OnOpenPressed;
        buttonsContainer.AddChild(_openButton);

        _claimButton = new EmeraldButton
        {
            Text = "ПОЛУЧИТЬ",
            MinSize = new Vector2(140, 40),
            Visible = false
        };
        _claimButton.OnPressed += OnClaimPressed;
        buttonsContainer.AddChild(_claimButton);

        _closeButton = new EmeraldButton
        {
            Text = "ЗАКРЫТЬ",
            MinSize = new Vector2(140, 40),
            Visible = false
        };
        _closeButton.OnPressed += () => OnCloseRequested?.Invoke();
        buttonsContainer.AddChild(_closeButton);

        _mainContainer.AddChild(buttonsContainer);

        AddContent(_mainContainer);
    }

    public void SetLootbox(string name, int userItemId, bool stelsHidden)
    {
        _lootboxName = name;
        _userItemId = userItemId;
        _stelsHidden = stelsHidden;
        _state = LootboxOpenState.Initial;

        Title = $"ОТКРЫТИЕ: {name.ToUpper()}";
        _statusLabel.Text = "Нажмите \"Открыть\" чтобы начать";
        _rarityLabel.Text = "";
        _itemNameLabel.Text = "";

        _openButton.Visible = true;
        _openButton.Disabled = false;
        _claimButton.Visible = false;
        _closeButton.Visible = false;

        _currentCardTexture.Visible = true;
        _spriteContainer.Visible = false;

        UpdateCardTexture(LootboxRarity.Common);
    }

    private void OnOpenPressed()
    {
        if (_state != LootboxOpenState.Initial)
            return;

        _state = LootboxOpenState.WaitingResult;
        _openButton.Disabled = true;
        _statusLabel.Text = "Открываем...";

        OnOpenRequested?.Invoke(_userItemId, _stelsHidden);
    }

    public void HandleOpenResult(LootboxOpenResult result)
    {
        _result = result;

        if (!result.Success)
        {
            _state = LootboxOpenState.Complete;
            _statusLabel.Text = result.Message;
            _openButton.Visible = false;
            _closeButton.Visible = true;
            return;
        }

        if (result.StelsOpen && result.Sequence != null && result.Sequence.Count > 0)
        {
            _scrollSequence = new List<LootboxRarity>(result.Sequence);
            _currentScrollIndex = 0;
            _animationTime = 0;
            _scrollSpeed = 12f + new Random().NextSingle() * 4f;
            _animationComplete = false;
            _state = LootboxOpenState.Scrolling;
            _statusLabel.Text = "Прокрутка...";
            GenerateParticles();
        }
        else
        {
            _state = LootboxOpenState.Revealing;
            _revealAnimationTime = 0;
            _cardScale = 0.5f;
            _statusLabel.Text = "";
            StartReveal();
        }
    }

    private void OnClaimPressed()
    {
        if (_state != LootboxOpenState.Complete)
            return;

        OnCloseRequested?.Invoke();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        switch (_state)
        {
            case LootboxOpenState.Scrolling:
                UpdateScrollAnimation(args.DeltaSeconds);
                break;

            case LootboxOpenState.Revealing:
                UpdateRevealAnimation(args.DeltaSeconds);
                break;

            case LootboxOpenState.Complete:
                UpdateParticles(args.DeltaSeconds);
                break;
        }
    }

    private void UpdateScrollAnimation(float delta)
    {
        _animationTime += delta;
        _particleTime += delta;

        var scrollDelta = _scrollSpeed * delta;
        _currentScrollIndex += (int)(scrollDelta * 3);

        if (_scrollSequence.Count > 0)
        {
            var displayIndex = _currentScrollIndex % _scrollSequence.Count;
            UpdateCardTexture(_scrollSequence[displayIndex]);
            _rarityLabel.Text = GetRarityName(_scrollSequence[displayIndex]);
            _rarityLabel.TextColor = GetRarityColor(_scrollSequence[displayIndex]);
        }

        _scrollSpeed *= MathF.Pow(_scrollDeceleration, delta * 60);

        if (_scrollSpeed < 0.5f)
        {
            _animationComplete = true;
            _state = LootboxOpenState.Revealing;
            _revealAnimationTime = 0;
            _cardScale = 0.8f;
            StartReveal();
        }

        UpdateParticles(delta);
    }

    private void UpdateRevealAnimation(float delta)
    {
        _revealAnimationTime += delta;
        _particleTime += delta;

        _cardScale = Math.Min(1.1f, _cardScale + delta * 0.8f);

        if (_revealAnimationTime > 0.5f)
        {
            if (_result?.Item != null)
            {
                UpdateCardTexture(_result.Item.Rarity);
                _rarityLabel.Text = GetRarityName(_result.Item.Rarity);
                _rarityLabel.TextColor = GetRarityColor(_result.Item.Rarity);
                _itemNameLabel.Text = _result.Item.Name.ToUpper();
                _itemNameLabel.TextColor = GetRarityColor(_result.Item.Rarity);

                if (!string.IsNullOrEmpty(_result.Item.ItemIdInGame) &&
                    _protoManager.HasIndex<EntityPrototype>(_result.Item.ItemIdInGame))
                {
                    try
                    {
                        var spawned = _entMan.SpawnEntity(_result.Item.ItemIdInGame, MapCoordinates.Nullspace);
                        _itemSpriteView?.SetEntity(spawned);
                        _spriteContainer.Visible = true;
                    }
                    catch { }
                }
            }
        }

        if (_revealAnimationTime > 1.2f)
        {
            _state = LootboxOpenState.Complete;
            _cardScale = 1f;
            _statusLabel.Text = "Поздравляем!";
            _openButton.Visible = false;
            _claimButton.Visible = true;
            _closeButton.Visible = true;

            GenerateParticles();
        }

        UpdateParticles(delta);
    }

    private void StartReveal()
    {
        if (_result?.Item != null)
        {
            GenerateParticles();
        }
    }

    private void GenerateParticles()
    {
        _particles.Clear();
        var random = new Random();

        var centerX = _cardContainer.PixelSize.X / 2f + _cardContainer.Position.X;
        var centerY = _cardContainer.PixelSize.Y / 2f + _cardContainer.Position.Y;

        var particleColor = _result?.Item != null ? GetRarityColor(_result.Item.Rarity) : _commonColor;

        for (int i = 0; i < 30; i++)
        {
            _particles.Add(new Particle
            {
                X = centerX + (random.NextSingle() - 0.5f) * 150f,
                Y = centerY + (random.NextSingle() - 0.5f) * 150f,
                VelocityX = (random.NextSingle() - 0.5f) * 100f,
                VelocityY = -random.NextSingle() * 80f - 20f,
                Size = random.NextSingle() * 4f + 2f,
                Alpha = random.NextSingle() * 0.6f + 0.4f,
                Color = particleColor
            });
        }
    }

    private void UpdateParticles(float delta)
    {
        var random = new Random();

        foreach (var p in _particles)
        {
            p.X += p.VelocityX * delta;
            p.Y += p.VelocityY * delta;
            p.VelocityY += 30f * delta;
            p.Alpha = Math.Max(0f, p.Alpha - delta * 0.3f);

            if (p.Alpha <= 0f)
            {
                var centerX = _cardContainer.PixelSize.X / 2f + _cardContainer.Position.X;
                var centerY = _cardContainer.PixelSize.Y / 2f + _cardContainer.Position.Y;

                p.X = centerX + (random.NextSingle() - 0.5f) * 150f;
                p.Y = centerY;
                p.VelocityX = (random.NextSingle() - 0.5f) * 100f;
                p.VelocityY = -random.NextSingle() * 80f - 20f;
                p.Alpha = random.NextSingle() * 0.6f + 0.4f;
            }
        }
    }

    private void UpdateCardTexture(LootboxRarity rarity)
    {
        var texture = rarity switch
        {
            LootboxRarity.Common => _commonTexture,
            LootboxRarity.Epic => _epicTexture,
            LootboxRarity.Mythic => _mythicTexture,
            LootboxRarity.Legendary => _legendaryTexture,
            _ => _commonTexture
        };

        _currentCardTexture.Texture = texture;
    }

    private string GetRarityName(LootboxRarity rarity)
    {
        return rarity switch
        {
            LootboxRarity.Common => "ОБЫЧНЫЙ",
            LootboxRarity.Epic => "ЭПИЧЕСКИЙ",
            LootboxRarity.Mythic => "МИФИЧЕСКИЙ",
            LootboxRarity.Legendary => "ЛЕГЕНДАРНЫЙ",
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

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        foreach (var p in _particles)
        {
            if (p.Alpha <= 0f) continue;

            handle.DrawRect(
                new UIBox2(
                    p.X - p.Size / 2f,
                    p.Y - p.Size / 2f,
                    p.X + p.Size / 2f,
                    p.Y + p.Size / 2f),
                p.Color.WithAlpha(p.Alpha));
        }
    }

    private sealed class Particle
    {
        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;
        public float Size;
        public float Alpha;
        public Color Color;
    }
}
