// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Donate.Emerald;

public sealed class EmeraldRewardClaimPopup : Control
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private const int TitleFontSize = 18;
    private const int NameFontSize = 14;
    private const int MessageFontSize = 11;

    private Font _titleFont = default!;
    private Font _nameFont = default!;
    private Font _messageFont = default!;

    private string _itemName = "";
    private string? _protoId;
    private bool _isPremium;
    private bool _isSuccess;
    private string _message = "";

    private float _animationProgress;
    private float _particleTime;
    private List<Particle> _particles = new();
    private bool _animationComplete;

    private readonly Color _bgColor = Color.FromHex("#0f0a1e");
    private readonly Color _borderColor = Color.FromHex("#6d5a8a");
    private readonly Color _successColor = Color.FromHex("#00FFAA");
    private readonly Color _errorColor = Color.FromHex("#ff6b6b");
    private readonly Color _premiumColor = Color.FromHex("#ffd700");
    private readonly Color _textColor = Color.FromHex("#c0b3da");
    private readonly Color _glowColor = Color.FromHex("#d4c5e8");
    private readonly Color _particleColor = Color.FromHex("#00FFAA");
    private readonly Color _premiumParticleColor = Color.FromHex("#ffd700");

    private SpriteView? _spriteView;
    private TextureRect? _textureRect;
    private PanelContainer? _spriteContainer;
    private Texture? _fallbackTexture;
    private EmeraldButton? _closeButton;

    public event Action? OnClose;

    public string ItemName
    {
        get => _itemName;
        set
        {
            _itemName = value;
            InvalidateMeasure();
        }
    }

    public string? ProtoId
    {
        get => _protoId;
        set
        {
            _protoId = value;
            UpdateSprite();
        }
    }

    public bool IsPremium
    {
        get => _isPremium;
        set
        {
            _isPremium = value;
            InvalidateMeasure();
        }
    }

    public bool IsSuccess
    {
        get => _isSuccess;
        set
        {
            _isSuccess = value;
            InvalidateMeasure();
        }
    }

    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            InvalidateMeasure();
        }
    }

    public EmeraldRewardClaimPopup()
    {
        IoCManager.InjectDependencies(this);

        var fontRes = _resourceCache.GetResource<FontResource>("/Fonts/Bedstead/Bedstead.otf");
        _titleFont = new VectorFont(fontRes, TitleFontSize);
        _nameFont = new VectorFont(fontRes, NameFontSize);
        _messageFont = new VectorFont(fontRes, MessageFontSize);

        MouseFilter = MouseFilterMode.Stop;

        BuildUI();
        GenerateParticles();
    }

    private void BuildUI()
    {
        _spriteContainer = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.Transparent
            }
        };

        _spriteView = new SpriteView(_entMan)
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Stretch = SpriteView.StretchMode.Fit,
            Visible = false
        };

        _textureRect = new TextureRect
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            Visible = false
        };

        _spriteContainer.AddChild(_spriteView);
        _spriteContainer.AddChild(_textureRect);
        AddChild(_spriteContainer);

        _closeButton = new EmeraldButton
        {
            Text = "ЗАКРЫТЬ",
            MinSize = new Vector2(120, 36)
        };
        _closeButton.OnPressed += () => OnClose?.Invoke();
        AddChild(_closeButton);
    }

    private void GenerateParticles()
    {
        var random = new Random();
        for (int i = 0; i < 30; i++)
        {
            _particles.Add(new Particle
            {
                X = random.NextSingle() * 300f,
                Y = random.NextSingle() * 400f,
                VelocityX = (random.NextSingle() - 0.5f) * 100f,
                VelocityY = -random.NextSingle() * 80f - 20f,
                Size = random.NextSingle() * 4f + 2f,
                Alpha = random.NextSingle() * 0.5f + 0.5f,
                Lifetime = random.NextSingle() * 2f + 1f
            });
        }
    }

    private void UpdateSprite()
    {
        if (_spriteView == null || _textureRect == null)
            return;

        if (!string.IsNullOrEmpty(_protoId) && _protoManager.HasIndex<EntityPrototype>(_protoId))
        {
            try
            {
                var spawned = _entMan.SpawnEntity(_protoId, MapCoordinates.Nullspace);
                _spriteView.SetEntity(spawned);
                _spriteView.Visible = true;
                _spriteView.Scale = new Vector2(3f, 3f);
                _textureRect.Visible = false;
                return;
            }
            catch
            {
            }
        }

        _spriteView.Visible = false;
        _textureRect.Visible = true;

        if (_fallbackTexture == null)
        {
            try
            {
                _fallbackTexture = _resourceCache.GetResource<TextureResource>("/Textures/Interface/giftbox.png").Texture;
            }
            catch
            {
                try
                {
                    _fallbackTexture = _resourceCache.GetResource<TextureResource>("/Textures/Interface/fallback.png").Texture;
                }
                catch
                {
                    _fallbackTexture = null;
                }
            }
        }

        _textureRect.Texture = _fallbackTexture;
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        return new Vector2(320, 420);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        if (_spriteContainer != null)
        {
            var spriteSize = 100f;
            var spriteX = (finalSize.X - spriteSize) / 2f;
            var spriteY = 100f;
            _spriteContainer.Arrange(new UIBox2(spriteX, spriteY, spriteX + spriteSize, spriteY + spriteSize));
        }

        if (_closeButton != null)
        {
            var buttonSize = _closeButton.DesiredSize;
            var buttonX = (finalSize.X - buttonSize.X) / 2f;
            var buttonY = finalSize.Y - buttonSize.Y - 20f;
            _closeButton.Arrange(new UIBox2(buttonX, buttonY, buttonX + buttonSize.X, buttonY + buttonSize.Y));
        }

        return finalSize;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_animationComplete)
        {
            _animationProgress = Math.Min(1f, _animationProgress + (float)args.DeltaSeconds * 2f);
            if (_animationProgress >= 1f)
                _animationComplete = true;
        }

        _particleTime += (float)args.DeltaSeconds;

        foreach (var particle in _particles)
        {
            particle.X += particle.VelocityX * (float)args.DeltaSeconds;
            particle.Y += particle.VelocityY * (float)args.DeltaSeconds;
            particle.VelocityY += 30f * (float)args.DeltaSeconds;
            particle.Alpha = Math.Max(0f, particle.Alpha - (float)args.DeltaSeconds * 0.3f);

            if (particle.Y > PixelSize.Y || particle.Alpha <= 0)
            {
                var random = new Random();
                particle.X = random.NextSingle() * PixelSize.X;
                particle.Y = PixelSize.Y / 2f;
                particle.VelocityX = (random.NextSingle() - 0.5f) * 100f;
                particle.VelocityY = -random.NextSingle() * 80f - 20f;
                particle.Alpha = random.NextSingle() * 0.5f + 0.5f;
            }
        }

        InvalidateMeasure();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var rect = new UIBox2(0, 0, PixelSize.X, PixelSize.Y);

        handle.DrawRect(rect, _bgColor.WithAlpha(0.95f));

        var particleColor = _isPremium ? _premiumParticleColor : _particleColor;
        foreach (var particle in _particles)
        {
            if (particle.Alpha <= 0) continue;

            var particleRect = new UIBox2(
                particle.X - particle.Size / 2f,
                particle.Y - particle.Size / 2f,
                particle.X + particle.Size / 2f,
                particle.Y + particle.Size / 2f
            );
            handle.DrawRect(particleRect, particleColor.WithAlpha(particle.Alpha));
        }

        var accentColor = _isPremium ? _premiumColor : _isSuccess ? _successColor : _errorColor;

        var glowIntensity = 0.3f + MathF.Sin(_particleTime * 2f) * 0.1f;
        for (int i = 3; i >= 0; i--)
        {
            var offset = i * 2f * UIScale;
            var glowRect = new UIBox2(rect.Left - offset, rect.Top - offset, rect.Right + offset, rect.Bottom + offset);
            DrawBorder(handle, glowRect, accentColor.WithAlpha(glowIntensity / (i + 1)));
        }

        DrawBorder(handle, rect, accentColor);

        var scale = 0.5f + _animationProgress * 0.5f;
        var titleText = _isSuccess ? "НАГРАДА ПОЛУЧЕНА!" : "ОШИБКА";
        var titleWidth = GetTextWidth(titleText, _titleFont);
        var titleX = (PixelSize.X - titleWidth * scale) / 2f;
        var titleY = 30f * UIScale;

        handle.DrawString(_titleFont, new Vector2(titleX, titleY), titleText, UIScale * scale, accentColor);

        if (_isPremium)
        {
            var premiumText = "PREMIUM НАГРАДА";
            var premiumWidth = GetTextWidth(premiumText, _messageFont);
            var premiumX = (PixelSize.X - premiumWidth) / 2f;
            var premiumY = titleY + _titleFont.GetLineHeight(UIScale) + 8f * UIScale;
            handle.DrawString(_messageFont, new Vector2(premiumX, premiumY), premiumText, UIScale, _premiumColor);
        }

        var nameY = 220f * UIScale;
        var nameWidth = GetTextWidth(_itemName, _nameFont);
        var nameX = (PixelSize.X - nameWidth) / 2f;
        handle.DrawString(_nameFont, new Vector2(nameX, nameY), _itemName, UIScale, _textColor);

        if (!string.IsNullOrEmpty(_message))
        {
            var messageY = nameY + _nameFont.GetLineHeight(UIScale) + 12f * UIScale;
            var messageWidth = GetTextWidth(_message, _messageFont);
            var messageX = (PixelSize.X - messageWidth) / 2f;
            var messageColor = _isSuccess ? _textColor : _errorColor;
            handle.DrawString(_messageFont, new Vector2(messageX, messageY), _message, UIScale, messageColor);
        }

        if (_isSuccess)
        {
            var hintText = "Предмет добавлен в ваш инвентарь!";
            var hintWidth = GetTextWidth(hintText, _messageFont);
            var hintX = (PixelSize.X - hintWidth) / 2f;
            var hintY = PixelSize.Y - 80f * UIScale;
            handle.DrawString(_messageFont, new Vector2(hintX, hintY), hintText, UIScale, _successColor.WithAlpha(0.8f));
        }
    }

    private void DrawBorder(DrawingHandleScreen handle, UIBox2 rect, Color color)
    {
        var thickness = Math.Max(1f, 2f * UIScale);
        handle.DrawRect(new UIBox2(rect.Left, rect.Top, rect.Right, rect.Top + thickness), color);
        handle.DrawRect(new UIBox2(rect.Left, rect.Bottom - thickness, rect.Right, rect.Bottom), color);
        handle.DrawRect(new UIBox2(rect.Left, rect.Top, rect.Left + thickness, rect.Bottom), color);
        handle.DrawRect(new UIBox2(rect.Right - thickness, rect.Top, rect.Right, rect.Bottom), color);
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

    protected override void UIScaleChanged()
    {
        base.UIScaleChanged();
        InvalidateMeasure();
    }

    private class Particle
    {
        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;
        public float Size;
        public float Alpha;
        public float Lifetime;
    }
}
