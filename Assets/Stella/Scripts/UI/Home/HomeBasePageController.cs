using System;
using System.Collections.Generic;
using System.IO;
using StellaGuild.Design;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        [SerializeField] private string guildLabel = "ギルド";
        [SerializeField] private string worldLabel = "世界";
        [SerializeField] private string postButtonLabel = "ポスト";
        [SerializeField] private string cargoButtonLabel = "荷物";
        [SerializeField] private Sprite guildButtonSprite;
        [SerializeField] private Sprite worldButtonSprite;
        [SerializeField] private Sprite postButtonSprite;
        [SerializeField] private Sprite cargoButtonSprite;
        [SerializeField] private Sprite chatButtonSprite;
        [SerializeField] private bool rebuildLayout;

        private const string RootName = "HomeBaseRoot";
        private const float TopPanelHeight = 300f;
        private const float BottomAreaHeight = 320f;
        private const string MapViewportPath = "WorldMap3DBackground";
        private const float SideActionButtonDiameter = 104f;
        private static readonly Vector2 SideActionAnchor = new(0.94f, 0f);
        private const float BottomActionGlobalYOffset = 0.08f;
        private const float PostButtonAnchorY = 1.42f;
        private const float PostLabelAnchorY = 1.27f;
        private const float CargoButtonAnchorY = 0.96f;
        private const float CargoLabelAnchorY = 0.81f;
        private const string ButtonBaseFillName = "BaseFill";
        private static readonly Color32 HomeButtonBackgroundColor = new(0xC6, 0xB1, 0x98, 0xFF);
        private static readonly Vector2 MainButtonIconPadding = Vector2.zero;
        private static readonly Vector2 SideButtonIconPadding = new(6f, 6f);
        private static readonly Vector2 ChatButtonIconPadding = new(14f, 14f);

        private Font _font;
        private Sprite _circleSprite;
        private Sprite _roundedSprite;
        private Texture2D _circleTexture;
        private Texture2D _roundedTexture;
        private readonly Dictionary<string, Sprite> _uiFileSpriteCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<Texture2D> _uiFileTextures = new();

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

            for (var i = 0; i < _uiFileTextures.Count; i++)
            {
                var texture = _uiFileTextures[i];
                if (texture == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(texture);
                }
                else
                {
                    DestroyImmediate(texture);
                }
            }

            _uiFileTextures.Clear();
            _uiFileSpriteCache.Clear();
        }

        private void OnValidate()
        {
            AutoAssignUiSpritesInEditor();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ApplyEditorPreviewIfReady();
            }
#endif
        }

#if UNITY_EDITOR
        private void AutoAssignUiSpritesInEditor()
        {
            EnsureTextureImportedAsSprite("Assets/Stella/UI/guild.png");
            EnsureTextureImportedAsSprite("Assets/Stella/UI/world.png");
            EnsureTextureImportedAsSprite("Assets/Stella/UI/post.png");
            EnsureTextureImportedAsSprite("Assets/Stella/UI/cargo.png");
            EnsureTextureImportedAsSprite("Assets/Stella/UI/chat.png");

            guildButtonSprite = LoadSpriteIfMissing(guildButtonSprite, "Assets/Stella/UI/guild.png");
            worldButtonSprite = LoadSpriteIfMissing(worldButtonSprite, "Assets/Stella/UI/world.png");
            postButtonSprite = LoadSpriteIfMissing(postButtonSprite, "Assets/Stella/UI/post.png");
            cargoButtonSprite = LoadSpriteIfMissing(cargoButtonSprite, "Assets/Stella/UI/cargo.png");
            chatButtonSprite = LoadSpriteIfMissing(chatButtonSprite, "Assets/Stella/UI/chat.png");
        }

        private static Sprite LoadSpriteIfMissing(Sprite current, string assetPath)
        {
            if (current != null)
            {
                return current;
            }

            EnsureTextureImportedAsSprite(assetPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void EnsureTextureImportedAsSprite(string assetPath)
        {
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter == null)
            {
                return;
            }

            var needsReimport = textureImporter.textureType != TextureImporterType.Sprite
                || textureImporter.spriteImportMode != SpriteImportMode.Single
                || textureImporter.mipmapEnabled
                || !textureImporter.alphaIsTransparency;

            if (!needsReimport)
            {
                return;
            }

            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.mipmapEnabled = false;
            textureImporter.alphaIsTransparency = true;
            textureImporter.SaveAndReimport();
        }
#else
        private void AutoAssignUiSpritesInEditor()
        {
        }
#endif

        private void ApplyEditorPreviewIfReady()
        {
            var root = transform.Find(RootName);
            if (root == null)
            {
                return;
            }

            ApplyBottomLocalizationAndLayout(root);
            ApplyVisualAssets(root);
        }

        [ContextMenu("Rebuild Home Base Layout")]
        public void RebuildHomeBaseLayout()
        {
            rebuildLayout = false;
            RemoveExistingLayout();
            BuildLayout();
        }

        [ContextMenu("Force Apply Home Button Sprites")]
        public void ForceApplyHomeButtonSprites()
        {
            AutoAssignUiSpritesInEditor();

            var root = transform.Find(RootName);
            if (root == null)
            {
                RemoveExistingLayout();
                BuildLayout();
            }
            else
            {
                ApplyBottomLocalizationAndLayout(root);
                ApplyVisualAssets(root);
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObject);
#endif
        }

        private void EnsureLayout()
        {
            MigrateBottomLabelDefaultsIfNeeded();
            AutoAssignUiSpritesInEditor();

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
                    ApplyBottomLocalizationAndLayout(root);
                    ApplyVisualAssets(root);
                }

                return;
            }

            RemovePlaceholderChildren();
            BuildLayout();
        }

        private void MigrateBottomLabelDefaultsIfNeeded()
        {
            if (string.Equals(guildLabel, "Guild", StringComparison.Ordinal))
            {
                guildLabel = "ギルド";
            }

            if (string.Equals(worldLabel, "World", StringComparison.Ordinal))
            {
                worldLabel = "世界";
            }

            if (string.Equals(postButtonLabel, "Post", StringComparison.Ordinal))
            {
                postButtonLabel = "ポスト";
            }

            if (string.Equals(cargoButtonLabel, "Cargo", StringComparison.Ordinal))
            {
                cargoButtonLabel = "荷物";
            }
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
            ApplyVisualAssets(root);
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

            var chatButton = CreateCircle("ChatButton", mainArea, new Vector2(0.12f, 0.86f), 120f, HomeButtonBackgroundColor);
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

        private void ApplyBottomLocalizationAndLayout(Transform root)
        {
            var bottom = root.Find("BottomArea");
            if (bottom == null)
            {
                return;
            }

            SetElementActive(bottom, "GuildLabel", false);
            SetElementActive(bottom, "WorldLabel", false);
            SetBottomLabelText(bottom, "PostLabel", postButtonLabel);
            SetBottomLabelText(bottom, "CargoLabel", cargoButtonLabel);

            SetAnchoredElement(bottom, "GuildButton", new Vector2(0.17f, 0.33f + BottomActionGlobalYOffset), 230f);
            SetAnchoredElement(bottom, "WorldButton", new Vector2(0.83f, 0.33f + BottomActionGlobalYOffset), 230f);

            SetAnchoredElement(bottom, "PostButton", new Vector2(SideActionAnchor.x, PostButtonAnchorY + BottomActionGlobalYOffset), SideActionButtonDiameter);
            SetAnchoredElement(bottom, "PostLabel", new Vector2(SideActionAnchor.x, PostLabelAnchorY + BottomActionGlobalYOffset));
            SetAnchoredElement(bottom, "CargoButton", new Vector2(SideActionAnchor.x, CargoButtonAnchorY + BottomActionGlobalYOffset), SideActionButtonDiameter);
            SetAnchoredElement(bottom, "CargoLabel", new Vector2(SideActionAnchor.x, CargoLabelAnchorY + BottomActionGlobalYOffset));
        }

        private void ApplyVisualAssets(Transform root)
        {
            var mainArea = root.Find("MainArea");
            if (mainArea != null)
            {
                ApplyCircleButtonBase(mainArea, "ChatButton", HomeButtonBackgroundColor);
                var hasCustomChatSprite = ApplyButtonOverlaySprite(mainArea, "ChatButton", "Icon", "chat.png", chatButtonSprite, ChatButtonIconPadding);
                var chatDots = mainArea.Find("ChatButton/ChatDots");
                if (chatDots != null)
                {
                    chatDots.gameObject.SetActive(!hasCustomChatSprite);
                }
            }

            var bottom = root.Find("BottomArea");
            if (bottom == null)
            {
                return;
            }

            ApplyCircleButtonBase(bottom, "GuildButton", HomeButtonBackgroundColor);
            ApplyCircleButtonBase(bottom, "WorldButton", HomeButtonBackgroundColor);
            ApplyCircleButtonBase(bottom, "PostButton", HomeButtonBackgroundColor);
            ApplyCircleButtonBase(bottom, "CargoButton", HomeButtonBackgroundColor);
            ApplyButtonOverlaySprite(bottom, "GuildButton", "Icon", "guild.png", guildButtonSprite, MainButtonIconPadding);
            ApplyButtonOverlaySprite(bottom, "WorldButton", "Icon", "world.png", worldButtonSprite, MainButtonIconPadding);
            ApplyButtonOverlaySprite(bottom, "PostButton", "Icon", "post.png", postButtonSprite, SideButtonIconPadding);
            ApplyButtonOverlaySprite(bottom, "CargoButton", "Icon", "cargo.png", cargoButtonSprite, SideButtonIconPadding);
        }

        private bool ApplyButtonSprite(Transform parent, string elementName, string fileName, Sprite directSprite = null)
        {
            var element = parent.Find(elementName) as RectTransform;
            if (element == null)
            {
                return false;
            }

            var image = element.GetComponent<Image>();
            if (image == null)
            {
                return false;
            }

            var sprite = ResolveButtonSprite(fileName, directSprite);
            if (sprite == null)
            {
                return false;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            return true;
        }

        private void ApplyCircleButtonBase(Transform parent, string buttonName, Color color)
        {
            var button = parent.Find(buttonName) as RectTransform;
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            // Keep parent image transparent and render the colored base on a dedicated child.
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;

            var baseFillRect = button.Find(ButtonBaseFillName) as RectTransform;
            if (baseFillRect == null)
            {
                baseFillRect = CreateRect(
                    ButtonBaseFillName,
                    button,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
            }
            else
            {
                baseFillRect.anchorMin = Vector2.zero;
                baseFillRect.anchorMax = Vector2.one;
                baseFillRect.offsetMin = Vector2.zero;
                baseFillRect.offsetMax = Vector2.zero;
                baseFillRect.anchoredPosition = Vector2.zero;
            }

            baseFillRect.SetAsFirstSibling();
            var baseFillImage = baseFillRect.GetComponent<Image>();
            if (baseFillImage == null)
            {
                baseFillImage = baseFillRect.gameObject.AddComponent<Image>();
            }

            baseFillImage.sprite = GetCircleSprite();
            baseFillImage.type = Image.Type.Simple;
            baseFillImage.preserveAspect = false;
            baseFillImage.color = color;
            baseFillImage.raycastTarget = false;

            // Legacy layout has a filled "Border" child that can cover the base fill.
            // Keep it disabled so the configured background color is visible.
            var legacyBorder = button.Find("Border");
            if (legacyBorder != null && legacyBorder.gameObject.activeSelf)
            {
                legacyBorder.gameObject.SetActive(false);
            }
        }

        private bool ApplyButtonOverlaySprite(
            Transform parent,
            string buttonName,
            string overlayName,
            string fileName,
            Sprite directSprite,
            Vector2 padding)
        {
            var button = parent.Find(buttonName) as RectTransform;
            if (button == null)
            {
                return false;
            }

            var sprite = ResolveButtonSprite(fileName, directSprite);
            if (sprite == null)
            {
                return false;
            }

            var overlay = button.Find(overlayName) as RectTransform;
            if (overlay == null)
            {
                overlay = CreateRect(
                    overlayName,
                    button,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(padding.x, padding.y),
                    new Vector2(-padding.x, -padding.y));
            }
            else
            {
                overlay.anchorMin = Vector2.zero;
                overlay.anchorMax = Vector2.one;
                overlay.offsetMin = new Vector2(padding.x, padding.y);
                overlay.offsetMax = new Vector2(-padding.x, -padding.y);
                overlay.anchoredPosition = Vector2.zero;
            }

            overlay.SetAsLastSibling();
            var overlayImage = overlay.GetComponent<Image>();
            if (overlayImage == null)
            {
                overlayImage = overlay.gameObject.AddComponent<Image>();
            }

            overlayImage.sprite = sprite;
            overlayImage.type = Image.Type.Simple;
            overlayImage.preserveAspect = true;
            overlayImage.color = Color.white;
            overlayImage.raycastTarget = false;
            return true;
        }

        private Sprite ResolveButtonSprite(string fileName, Sprite directSprite)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var assetPath = "Assets/Stella/UI/" + fileName;
                EnsureTextureImportedAsSprite(assetPath);
                var editorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (editorSprite != null)
                {
                    return editorSprite;
                }
            }
#endif

            if (directSprite != null)
            {
                return directSprite;
            }

            return LoadSpriteFromUiFile(fileName);
        }

        private Sprite LoadSpriteFromUiFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            if (_uiFileSpriteCache.TryGetValue(fileName, out var cached))
            {
                return cached;
            }

            var filePath = Path.Combine(Application.dataPath, "Stella", "UI", fileName);
            if (!File.Exists(filePath))
            {
                _uiFileSpriteCache[fileName] = null;
                return null;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to read image file " + filePath + ": " + ex.Message, this);
                _uiFileSpriteCache[fileName] = null;
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "RuntimeUI_" + fileName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                if (Application.isPlaying)
                {
                    Destroy(texture);
                }
                else
                {
                    DestroyImmediate(texture);
                }

                _uiFileSpriteCache[fileName] = null;
                return null;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);

            _uiFileTextures.Add(texture);
            _uiFileSpriteCache[fileName] = sprite;
            return sprite;
        }

        private void SetBottomLabelText(Transform bottom, string labelName, string value)
        {
            var textTransform = bottom.Find($"{labelName}/Text");
            if (textTransform == null)
            {
                return;
            }

            var text = textTransform.GetComponent<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }

        private void SetAnchoredElement(Transform parent, string childName, Vector2 anchor, float size = -1f)
        {
            var element = parent.Find(childName) as RectTransform;
            if (element == null)
            {
                return;
            }

            element.anchorMin = anchor;
            element.anchorMax = anchor;
            element.anchoredPosition = Vector2.zero;

            if (size > 0f)
            {
                element.sizeDelta = new Vector2(size, size);
            }
        }

        private void SetElementActive(Transform parent, string childName, bool active)
        {
            var element = parent.Find(childName);
            if (element == null)
            {
                return;
            }

            if (element.gameObject.activeSelf == active)
            {
                return;
            }

            element.gameObject.SetActive(active);
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

            var leftMain = CreateCircle("GuildButton", bottom, new Vector2(0.17f, 0.33f + BottomActionGlobalYOffset), 230f, HomeButtonBackgroundColor);
            AddCircleBorder(leftMain, StellaColorTokens.Get(ColorToken.TextShadow), 8f);

            var rightMain = CreateCircle("WorldButton", bottom, new Vector2(0.83f, 0.33f + BottomActionGlobalYOffset), 230f, HomeButtonBackgroundColor);
            AddCircleBorder(rightMain, StellaColorTokens.Get(ColorToken.TextShadow), 8f);

            var postButton = CreateCircle("PostButton", bottom, new Vector2(SideActionAnchor.x, PostButtonAnchorY + BottomActionGlobalYOffset), SideActionButtonDiameter, HomeButtonBackgroundColor);
            AddCircleBorder(postButton, StellaColorTokens.Get(ColorToken.TextShadow), 6f);
            var postLabelText = CreateBottomLabel("PostLabel", bottom, postButtonLabel, new Vector2(SideActionAnchor.x, PostLabelAnchorY + BottomActionGlobalYOffset), 42);
            AddTextStroke(postLabelText);

            var cargoButton = CreateCircle("CargoButton", bottom, new Vector2(SideActionAnchor.x, CargoButtonAnchorY + BottomActionGlobalYOffset), SideActionButtonDiameter, HomeButtonBackgroundColor);
            AddCircleBorder(cargoButton, StellaColorTokens.Get(ColorToken.TextShadow), 6f);
            var cargoLabelText = CreateBottomLabel("CargoLabel", bottom, cargoButtonLabel, new Vector2(SideActionAnchor.x, CargoLabelAnchorY + BottomActionGlobalYOffset), 42);
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
