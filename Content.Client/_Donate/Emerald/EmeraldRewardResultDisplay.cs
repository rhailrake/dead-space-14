// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Donate.Emerald;

public sealed class EmeraldRewardResultDisplay : Control
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private const int TitleFontSize = 16;
    private const int NameFontSize = 12;
    private const int MessageFontSize = 10;

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
    private readonly List<Particle> _particles = new();
    private bool _animationComplete;
    private bool _particlesInitialized;

    private readonly Color _bgColor = Color.FromHex("#0f0a1e");
    private readonly Color _successColor = Color.FromHex("#00FFAA");
    private readonly Color _errorColor = Color.FromHex("#ff6b6b");
    private readonly Color _premiumColor = Color.FromHex("#ffd700");
    private readonly Color _textColor = Color.FromHex("#c0b3da");
    private readonly Color _particleColor = Color.FromHex("#00FFAA");
    private readonly Color _premiumParticleColor = Color.FromHex("#ffd700");

    private SpriteView? _spriteView;
    private TextureRect? _textureRect;
    private PanelContainer? _spriteContainer;
    private Texture? _fallbackTexture;
    private EmeraldButton? _closeButton;

    public event Action? OnClosePressed;

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

    public EmeraldRewardResultDisplay()
    {
        IoCManager.InjectDependencies(this);

        var fontRes = _resourceCache.GetResource<FontResource>("/Fonts/Bedstead/Bedstead.otf");
        _titleFont = new VectorFont(fontRes, TitleFontSize);
        _nameFont = new VectorFont(fontRes, NameFontSize);
        _messageFont = new VectorFont(fontRes, MessageFontSize);

        MouseFilter = MouseFilterMode.Stop;

        BuildUI();
    }

    private void BuildUI()
    {
        _spriteContainer = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Transparent }
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
            Visible = false,
        };

        _spriteContainer.AddChild(_spriteView);
        _spriteContainer.AddChild(_textureRect);
        AddChild(_spriteContainer);

        _closeButton = new EmeraldButton
        {
            Text = "ЗАКРЫТЬ",
            MinSize = new Vector2(140, 36)
        };
        _closeButton.OnPressed += () => OnClosePressed?.Invoke();
        AddChild(_closeButton);
    }

    private void GenerateParticles()
    {
        _particles.Clear();
        var random = new Random();

        var centerX = PixelSize.X / 2f;
        var centerY = PixelSize.Y / 2f;

        for (int i = 0; i < 25; i++)
        {
            _particles.Add(new Particle
            {
                X = centerX + (random.NextSingle() - 0.5f) * 120f,
                Y = centerY + (random.NextSingle() - 0.5f) * 120f,
                VelocityX = (random.NextSingle() - 0.5f) * 80f,
                VelocityY = -random.NextSingle() * 60f - 15f,
                Size = random.NextSingle() * 3f + 1.5f,
                Alpha = random.NextSingle() * 0.5f + 0.5f
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
                _spriteView.Scale = new Vector2(2.5f, 2.5f);
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
        return new Vector2(300, 380);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        if (_spriteContainer != null)
        {
            var size = 80f;
            var x = (finalSize.X - size) / 2f;
            var y = 90f;
            _spriteContainer.Arrange(new UIBox2(x, y, x + size, y + size));
        }

        if (_closeButton != null)
        {
            var s = _closeButton.DesiredSize;
            var x = (finalSize.X - s.X) / 2f;
            var y = finalSize.Y - s.Y - 50f;
            _closeButton.Arrange(new UIBox2(x, y, x + s.X, y + s.Y));
        }

        return finalSize;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_particlesInitialized && PixelSize.X > 0 && PixelSize.Y > 0)
        {
            GenerateParticles();
            _particlesInitialized = true;
        }

        if (!_animationComplete)
        {
            _animationProgress = Math.Min(1f, _animationProgress + (float)args.DeltaSeconds * 2f);
            if (_animationProgress >= 1f)
                _animationComplete = true;
        }

        _particleTime += (float)args.DeltaSeconds;
        var random = new Random();

        foreach (var particle in _particles)
        {
            particle.X += particle.VelocityX * (float)args.DeltaSeconds;
            particle.Y += particle.VelocityY * (float)args.DeltaSeconds;
            particle.VelocityY += 25f * (float)args.DeltaSeconds;
            particle.Alpha = Math.Max(0f, particle.Alpha - (float)args.DeltaSeconds * 0.25f);

            if (particle.Y > PixelSize.Y || particle.Alpha <= 0f)
            {
                var cx = PixelSize.X / 2f;
                var cy = PixelSize.Y / 2f;

                particle.X = cx + (random.NextSingle() - 0.5f) * 120f;
                particle.Y = cy;
                particle.VelocityX = (random.NextSingle() - 0.5f) * 80f;
                particle.VelocityY = -random.NextSingle() * 60f - 15f;
                particle.Alpha = random.NextSingle() * 0.5f + 0.5f;
            }
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var rect = new UIBox2(0, 0, PixelSize.X, PixelSize.Y);
        handle.DrawRect(rect, _bgColor.WithAlpha(0.95f));

        var pColor = _isPremium ? _premiumParticleColor : _particleColor;
        foreach (var p in _particles)
        {
            if (p.Alpha <= 0f) continue;

            handle.DrawRect(
                new UIBox2(
                    p.X - p.Size / 2f,
                    p.Y - p.Size / 2f,
                    p.X + p.Size / 2f,
                    p.Y + p.Size / 2f),
                pColor.WithAlpha(p.Alpha));
        }

        var accent = _isPremium ? _premiumColor : _isSuccess ? _successColor : _errorColor;

        var scale = 0.5f + _animationProgress * 0.5f;
        var title = _isSuccess ? "НАГРАДА ПОЛУЧЕНА!" : "ОШИБКА";
        var titleWidth = GetTextWidth(title, _titleFont);
        var titleX = (PixelSize.X - titleWidth * scale) / 2f;
        var titleY = 24f * UIScale;

        handle.DrawString(_titleFont, new Vector2(titleX, titleY), title, UIScale * scale, accent);

        var nameY = 185f * UIScale;
        var nameWidth = GetTextWidth(_itemName, _nameFont);
        handle.DrawString(_nameFont,
            new Vector2((PixelSize.X - nameWidth) / 2f, nameY),
            _itemName, UIScale, _textColor);
    }

    private float GetTextWidth(string text, Font font)
    {
        var w = 0f;
        foreach (var r in text.EnumerateRunes())
        {
            var m = font.GetCharMetrics(r, UIScale);
            if (m.HasValue)
                w += m.Value.Advance;
        }
        return w;
    }

    private sealed class Particle
    {
        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;
        public float Size;
        public float Alpha;
    }
}
