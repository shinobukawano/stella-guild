using System;
using System.Collections.Generic;
using StellaGuild.Design;
using UnityEngine;
using UnityEngine.UI;

namespace StellaGuild.UI.Home
{
    public sealed class HomeBasePageController : UIPage
    {
        [Serializable]
        private struct StatRow
        {
            public string label;
            public string value;
        }

        [SerializeField] private List<StatRow> leftStats = new()
        {
            new StatRow { label = "Level", value = "12,345" },
            new StatRow { label = "Power", value = "12,345" },
            new StatRow { label = "Coins", value = "12,345" }
        };

        [SerializeField] private List<StatRow> rightStats = new()
        {
            new StatRow { label = "Time", value = "12,345" },
            new StatRow { label = "Population", value = "12,345" },
            new StatRow { label = "Quest", value = "List" }
        };

        [SerializeField] private string guildLabel = "Guild";
        [SerializeField] private string worldLabel = "World";
        [SerializeField] private string postButtonLabel = "Post";
        [SerializeField] private string cargoButtonLabel = "Cargo";
        [SerializeField] private bool rebuildLayout;

        private const string RootName = "HomeBaseRoot";
        private const float TopPanelHeight = 300f;
        private const float BottomAreaHeight = 320f;
        private const string MapViewportPath = "WorldMap3DBackground";

        private Font _font;
        private Sprite _circleSprite;
        private Sprite _roundedSprite;
        private Texture2D _circleTexture;
        private Texture2D _roundedTexture;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            EnsureLayout();
        }

        private void OnDestroy()
        {
            if (_circleTexture != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_circleTexture);
                }
                else
                {
                    DestroyImmediate(_circleTexture);
                }

                _circleTexture = null;
            }

            if (_roundedTexture != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_roundedTexture);
                }
                else
                {
                    DestroyImmediate(_roundedTexture);
                }

                _roundedTexture = null;
            }
        }

        [ContextMenu("Rebuild Home Base Layout")]
        public void RebuildHomeBaseLayout()
        {
            rebuildLayout = false;
            RemoveExistingLayout();
            BuildLayout();
        }

        private void EnsureLayout()
        {
            if (rebuildLayout)
            {
                rebuildLayout = false;
                RemoveExistingLayout();
                BuildLayout();
                return;
            }

            if (transform.Find(RootName) != null)
            {
                var root = transform.Find(RootName);
                if (root != null && root.Find(MapViewportPath) == null)
                {
                    RemoveExistingLayout();
                    BuildLayout();
                    return;
                }

                if (root != null)
                {
                    EnsureMapViewportComponents(root);
                }

                return;
            }

            RemovePlaceholderChildren();
            BuildLayout();
        }

        private void RemovePlaceholderChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == RootName)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void RemoveExistingLayout()
        {
            var existing = transform.Find(RootName);
            if (existing == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        private void BuildLayout()
        {
            var root = CreateRect(RootName, transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var background = root.gameObject.AddComponent<Image>();
            ConfigurePanelImage(background, StellaColorTokens.Get(ColorToken.BaseBackground));

            BuildFullScreenMap(root);
            BuildTopPanel(root);
            BuildMainArea(root);
            BuildBottomButtons(root);
        }

        private void BuildFullScreenMap(RectTransform root)
        {
            var mapViewport = CreateRect(
                "WorldMap3DBackground",
                root,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            mapViewport.SetAsFirstSibling();

            var mapImage = mapViewport.gameObject.AddComponent<RawImage>();
            mapImage.color = Color.white;
            mapImage.raycastTarget = false;

            var background3D = mapViewport.gameObject.AddComponent<HomeBaseBackground3D>();
            background3D.Initialize();
        }

        private void BuildTopPanel(RectTransform root)
        {
            var topPanel = CreateRect(
                "TopPanel",
                root,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -TopPanelHeight),
                new Vector2(0f, 0f));

            var panelImage = topPanel.gameObject.AddComponent<Image>();
            var topPanelColor = StellaColorTokens.Get(ColorToken.SecondaryBackground);
            topPanelColor.a = 0.72f;
            ConfigurePanelImage(panelImage, topPanelColor);

            var header = CreateRect(
                "Header",
                topPanel,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -76f),
                new Vector2(0f, 0f));
            var headerImage = header.gameObject.AddComponent<Image>();
            ConfigurePanelImage(headerImage, new Color(0f, 0f, 0f, 0.62f));

            var headerLabel = CreateText("HeaderLabel", header, "Status Bar", 58, StellaColorTokens.Get(ColorToken.BaseBackground), TextAnchor.MiddleCenter);
            AddTextShadow(headerLabel, new Color(0f, 0f, 0f, 0.3f));

            BuildStatusColumns(topPanel);
            BuildCenterIcon(topPanel);
        }

        private void BuildStatusColumns(RectTransform topPanel)
        {
            BuildStatusColumn(topPanel, leftStats, new Vector2(0f, 1f), new Vector2(0f, -110f), false);
            BuildStatusColumn(topPanel, rightStats, new Vector2(1f, 1f), new Vector2(0f, -110f), true);
        }

        private void BuildStatusColumn(RectTransform parent, List<StatRow> rows, Vector2 anchor, Vector2 startOffset, bool alignRight)
        {
            var column = CreateRect(
                alignRight ? "RightStats" : "LeftStats",
                parent,
                anchor,
                anchor,
                startOffset,
                startOffset);

            column.sizeDelta = new Vector2(320f, 170f);
            column.pivot = alignRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            column.anchoredPosition += alignRight ? new Vector2(-24f, 0f) : new Vector2(24f, 0f);

            for (var i = 0; i < Mathf.Min(3, rows.Count); i++)
            {
                var row = rows[i];
                var y = -i * 52f;

                var labelRect = CreateRect(
                    $"{(alignRight ? "R" : "L")}Label{i}",
                    column,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, y - 34f),
                    new Vector2(84f, y));
                var labelImage = labelRect.gameObject.AddComponent<Image>();
                ConfigureRoundedImage(labelImage, Color.black);
                CreateText("LabelText", labelRect, row.label, 14, StellaColorTokens.Get(ColorToken.BaseBackground), TextAnchor.MiddleCenter);

                var valueRect = CreateRect(
                    $"{(alignRight ? "R" : "L")}Value{i}",
                    column,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(94f, y - 34f),
                    new Vector2(308f, y));
                var valueImage = valueRect.gameObject.AddComponent<Image>();
                ConfigureRoundedImage(valueImage, Color.white);
                CreateText("ValueText", valueRect, row.value, 28, StellaColorTokens.Get(ColorToken.TextShadow), TextAnchor.MiddleCenter);
            }
        }

        private void BuildCenterIcon(RectTransform topPanel)
        {
            var ring = CreateCircle("CenterIconRing", topPanel, new Vector2(0.5f, 0.22f), 178f, StellaColorTokens.Get(ColorToken.TextShadow));
            var plate = CreateCircle("CenterIconPlate", topPanel, new Vector2(0.5f, 0.22f), 150f, StellaColorTokens.Get(ColorToken.MainButtonSecondary));
            plate.SetSiblingIndex(ring.GetSiblingIndex() + 1);

            var iconLabel = CreateText("CenterIconLabel", plate, "Icon", 42, StellaColorTokens.Get(ColorToken.BaseBackground), TextAnchor.MiddleCenter);
            AddTextShadow(iconLabel, StellaColorTokens.Get(ColorToken.TextShadow));
        }

        private void BuildMainArea(RectTransform root)
        {
            var mainArea = CreateRect(
                "MainArea",
                root,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, BottomAreaHeight),
                new Vector2(0f, -TopPanelHeight));

            var chatButton = CreateCircle("ChatButton", mainArea, new Vector2(0.12f, 0.86f), 120f, Color.white);
            AddCircleBorder(chatButton, StellaColorTokens.Get(ColorToken.TextShadow), 8f);
            var chatText = CreateText("ChatDots", chatButton, "...", 52, StellaColorTokens.Get(ColorToken.TextShadow), TextAnchor.MiddleCenter);
            AddTextShadow(chatText, new Color(0f, 0f, 0f, 0.2f));

            var badge = CreateCircle("Badge", chatButton, new Vector2(0.83f, 0.8f), 32f, StellaColorTokens.Get(ColorToken.Attention));
            var badgeText = CreateText("BadgeText", badge, "1", 18, Color.white, TextAnchor.MiddleCenter);
            AddTextShadow(badgeText, StellaColorTokens.Get(ColorToken.TextShadow));
        }

        private void EnsureMapViewportComponents(Transform root)
        {
            var mapViewport = root.Find(MapViewportPath);
            if (mapViewport == null)
            {
                return;
            }

            var mapImage = mapViewport.GetComponent<RawImage>();
            if (mapImage == null)
            {
                mapImage = mapViewport.gameObject.AddComponent<RawImage>();
            }

            mapImage.color = Color.white;
            mapImage.raycastTarget = false;

            var background3D = mapViewport.GetComponent<HomeBaseBackground3D>();
            if (background3D == null)
            {
                background3D = mapViewport.gameObject.AddComponent<HomeBaseBackground3D>();
            }

            background3D.Initialize();
        }

        private void BuildBottomButtons(RectTransform root)
        {
            var bottom = CreateRect(
                "BottomArea",
                root,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, BottomAreaHeight));

            var leftMain = CreateCircle("GuildButton", bottom, new Vector2(0.17f, 0.33f), 230f, StellaColorTokens.Get(ColorToken.MainButtonPrimary));
            AddCircleBorder(leftMain, StellaColorTokens.Get(ColorToken.TextShadow), 8f);
            var leftLabel = CreateBottomLabel("GuildLabel", bottom, guildLabel, new Vector2(0.17f, 0.18f));
            AddTextStroke(leftLabel);

            var rightMain = CreateCircle("WorldButton", bottom, new Vector2(0.83f, 0.33f), 230f, StellaColorTokens.Get(ColorToken.Point));
            AddCircleBorder(rightMain, StellaColorTokens.Get(ColorToken.TextShadow), 8f);
            var rightLabel = CreateBottomLabel("WorldLabel", bottom, worldLabel, new Vector2(0.83f, 0.18f));
            AddTextStroke(rightLabel);

            var postButton = CreateCircle("PostButton", bottom, new Vector2(0.9f, 0.76f), 120f, StellaColorTokens.Get(ColorToken.Attention));
            AddCircleBorder(postButton, StellaColorTokens.Get(ColorToken.TextShadow), 6f);
            var postLabelText = CreateBottomLabel("PostLabel", bottom, postButtonLabel, new Vector2(0.9f, 0.62f), 46);
            AddTextStroke(postLabelText);

            var cargoButton = CreateCircle("CargoButton", bottom, new Vector2(0.9f, 0.48f), 120f, StellaColorTokens.Get(ColorToken.MainButtonSecondary));
            AddCircleBorder(cargoButton, StellaColorTokens.Get(ColorToken.TextShadow), 6f);
            var cargoLabelText = CreateBottomLabel("CargoLabel", bottom, cargoButtonLabel, new Vector2(0.9f, 0.34f), 46);
            AddTextStroke(cargoLabelText);
        }

        private Text CreateBottomLabel(string name, RectTransform parent, string value, Vector2 anchor, int fontSize = 62)
        {
            var labelRect = CreateRect(
                name,
                parent,
                anchor,
                anchor,
                new Vector2(-150f, -36f),
                new Vector2(150f, 36f));

            var text = CreateText("Text", labelRect, value, fontSize, Color.white, TextAnchor.MiddleCenter);
            AddTextShadow(text, StellaColorTokens.Get(ColorToken.TextShadow));
            return text;
        }

        private RectTransform CreateCircle(string name, RectTransform parent, Vector2 anchor, float diameter, Color color)
        {
            var rect = CreateRect(
                name,
                parent,
                anchor,
                anchor,
                new Vector2(-diameter * 0.5f, -diameter * 0.5f),
                new Vector2(diameter * 0.5f, diameter * 0.5f));

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = GetCircleSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private void AddCircleBorder(RectTransform circleRect, Color borderColor, float borderThickness)
        {
            var border = CreateRect(
                "Border",
                circleRect,
                Vector2.zero,
                Vector2.one,
                new Vector2(-borderThickness, -borderThickness),
                new Vector2(borderThickness, borderThickness));
            border.SetAsFirstSibling();

            var borderImage = border.gameObject.AddComponent<Image>();
            borderImage.sprite = GetCircleSprite();
            borderImage.color = borderColor;
            borderImage.raycastTarget = false;
        }

        private RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private Text CreateText(string name, RectTransform parent, string value, int fontSize, Color color, TextAnchor alignment)
        {
            var textRect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var text = textRect.gameObject.AddComponent<Text>();
            text.font = GetFont();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private void ConfigurePanelImage(Image image, Color color)
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
        }

        private void ConfigureRoundedImage(Image image, Color color)
        {
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
        }

        private void AddTextShadow(Text text, Color color)
        {
            var shadow = text.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = text.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = color;
            shadow.effectDistance = new Vector2(0f, -2f);
            shadow.useGraphicAlpha = true;
        }

        private void AddTextStroke(Text text)
        {
            var outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = StellaColorTokens.Get(ColorToken.TextShadow);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }

        private Font GetFont()
        {
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return _font;
        }

        private Sprite GetCircleSprite()
        {
            if (_circleSprite == null)
            {
                _circleSprite = CreateCircleFallbackSprite();
            }

            return _circleSprite;
        }

        private Sprite GetRoundedSprite()
        {
            if (_roundedSprite == null)
            {
                _roundedSprite = CreateRoundedFallbackSprite();
            }

            return _roundedSprite;
        }

        private Sprite CreateCircleFallbackSprite()
        {
            const int size = 128;
            const float edge = 1.6f;

            _circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeCircleSprite",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var colors = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var radius = center - 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01((radius - distance) / edge);
                    var a = (byte)Mathf.RoundToInt(alpha * 255f);
                    colors[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            _circleTexture.SetPixels32(colors);
            _circleTexture.Apply(false, false);

            return Sprite.Create(
                _circleTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect);
        }

        private Sprite CreateRoundedFallbackSprite()
        {
            const int size = 64;
            const int border = 14;
            const float cornerRadius = 16f;
            const float edge = 1.5f;

            _roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeRoundedSprite",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var colors = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var nx = Mathf.Abs((x + 0.5f) - size * 0.5f) - (size * 0.5f - cornerRadius);
                    var ny = Mathf.Abs((y + 0.5f) - size * 0.5f) - (size * 0.5f - cornerRadius);
                    var ox = Mathf.Max(nx, 0f);
                    var oy = Mathf.Max(ny, 0f);
                    var outside = Mathf.Sqrt(ox * ox + oy * oy);
                    var alpha = Mathf.Clamp01((cornerRadius - outside) / edge);
                    var a = (byte)Mathf.RoundToInt(alpha * 255f);
                    colors[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            _roundedTexture.SetPixels32(colors);
            _roundedTexture.Apply(false, false);

            return Sprite.Create(
                _roundedTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }
    }
}
