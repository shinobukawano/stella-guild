using StellaGuild.Design;
using UnityEngine;
using UnityEngine.UI;

namespace StellaGuild.UI.Chat
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ChatPageController : UIPage
    {
        [SerializeField] private UIPageRouter pageRouter;
        [SerializeField] private string statusBarText = "ステータスバー";
        [SerializeField] private string tabAllText = "全体";
        [SerializeField] private string tabGuildText = "ギルド";
        [SerializeField] private string tabRoomText = "ルーム";
        [SerializeField] private string iconText = "アイコン";
        [SerializeField] private string leftMessageText = "好きなビールは黒ラベル！";
        [SerializeField] private string rightMessageText = "こんにちは！うめはらです！";
        [SerializeField] private string inputPlaceholder = "優しいコメントを入力 ....";
        [SerializeField] private bool rebuildLayout;

        private const string RootName = "ChatPageRoot";
        private static readonly Color32 BackgroundColor = new(0xE6, 0xE0, 0xD5, 0xFF);
        private static readonly Color32 HeaderColor = new(0x00, 0x00, 0x00, 0xFF);
        private static readonly Color32 TabColor = new(0x02, 0x21, 0x6A, 0xFF);
        private static readonly Color32 BorderColor = new(0x28, 0x19, 0x0A, 0xFF);
        private static readonly Color32 BubbleColor = new(0xF8, 0xF8, 0xF8, 0xFF);

        private Font _font;
        private Sprite _roundedSprite;
        private Sprite _circleSprite;
        private Texture2D _roundedTexture;
        private Texture2D _circleTexture;
        private Button _backButton;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            EnsureLayout();
        }

        private void OnDestroy()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackPressed);
                _backButton = null;
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
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            var root = transform.Find(RootName) as RectTransform;
            if (root == null)
            {
                return;
            }

            ApplyTexts(root);
            EnsureBackButtonBinding(root);
        }

        [ContextMenu("Rebuild Chat Layout")]
        public void RebuildChatLayout()
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

            var root = transform.Find(RootName) as RectTransform;
            if (root != null)
            {
                ApplyTexts(root);
                EnsureBackButtonBinding(root);
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
            background.color = BackgroundColor;
            background.raycastTarget = false;

            BuildHeader(root);
            BuildTabs(root);
            BuildMessageArea(root);
            BuildInputArea(root);
            ApplyTexts(root);
            EnsureBackButtonBinding(root);
        }

        private void BuildHeader(RectTransform root)
        {
            var header = CreateRect(
                "Header",
                root,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -120f),
                Vector2.zero);

            var headerImage = header.gameObject.AddComponent<Image>();
            headerImage.color = HeaderColor;
            headerImage.raycastTarget = false;

            CreateText("StatusText", header, statusBarText, 58, Color.white, TextAnchor.MiddleCenter);
        }

        private void BuildTabs(RectTransform root)
        {
            var tabArea = CreateRect(
                "Tabs",
                root,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(36f, -252f),
                new Vector2(-36f, -132f));

            CreateTab(tabArea, "AllTab", tabAllText, 0f, 1f / 3f);
            CreateTab(tabArea, "GuildTab", tabGuildText, 1f / 3f, 2f / 3f);
            CreateTab(tabArea, "RoomTab", tabRoomText, 2f / 3f, 1f);
        }

        private void CreateTab(RectTransform parent, string name, string label, float minX, float maxX)
        {
            var tab = CreateRect(
                name,
                parent,
                new Vector2(minX, 0f),
                new Vector2(maxX, 1f),
                new Vector2(4f, 0f),
                new Vector2(-4f, 0f));

            var tabImage = tab.gameObject.AddComponent<Image>();
            tabImage.sprite = GetRoundedSprite();
            tabImage.type = Image.Type.Sliced;
            tabImage.color = TabColor;
            tabImage.raycastTarget = false;

            var border = tab.gameObject.AddComponent<Outline>();
            border.effectColor = BorderColor;
            border.effectDistance = new Vector2(1f, -1f);
            border.useGraphicAlpha = true;

            CreateText("Label", tab, label, 56, Color.white, TextAnchor.MiddleCenter);
        }

        private void BuildMessageArea(RectTransform root)
        {
            var area = CreateRect(
                "MessageArea",
                root,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(24f, 210f),
                new Vector2(-24f, -290f));

            CreateMessageRow(area, "Row1", 300f, true, leftMessageText);
            CreateMessageRow(area, "Row2", 190f, false, rightMessageText);
            CreateMessageRow(area, "Row3", 80f, false, rightMessageText);
            CreateMessageRow(area, "Row4", -30f, true, leftMessageText);
        }

        private void CreateMessageRow(RectTransform parent, string name, float offsetY, bool isLeftIcon, string message)
        {
            var row = CreateRect(
                name,
                parent,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, offsetY),
                new Vector2(0f, offsetY + 110f));

            var icon = CreateRect(
                "IconCircle",
                row,
                isLeftIcon ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f),
                isLeftIcon ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f),
                isLeftIcon ? new Vector2(0f, -40f) : new Vector2(-80f, -40f),
                isLeftIcon ? new Vector2(80f, 40f) : new Vector2(0f, 40f));

            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = GetCircleSprite();
            iconImage.color = BubbleColor;
            iconImage.raycastTarget = false;

            var iconBorder = icon.gameObject.AddComponent<Outline>();
            iconBorder.effectColor = BorderColor;
            iconBorder.effectDistance = new Vector2(1f, -1f);
            iconBorder.useGraphicAlpha = true;

            CreateText("IconLabel", icon, iconText, 26, BorderColor, TextAnchor.MiddleCenter);

            var bubble = CreateRect(
                "Bubble",
                row,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                isLeftIcon ? new Vector2(96f, -34f) : new Vector2(60f, -34f),
                isLeftIcon ? new Vector2(-60f, 34f) : new Vector2(-96f, 34f));

            var bubbleImage = bubble.gameObject.AddComponent<Image>();
            bubbleImage.sprite = GetRoundedSprite();
            bubbleImage.type = Image.Type.Sliced;
            bubbleImage.color = BubbleColor;
            bubbleImage.raycastTarget = false;

            var bubbleBorder = bubble.gameObject.AddComponent<Outline>();
            bubbleBorder.effectColor = BorderColor;
            bubbleBorder.effectDistance = new Vector2(1f, -1f);
            bubbleBorder.useGraphicAlpha = true;

            var text = CreateText("MessageText", bubble, message, 30, BorderColor, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = false;

            var textRect = text.rectTransform;
            textRect.offsetMin = new Vector2(28f, 0f);
            textRect.offsetMax = new Vector2(-28f, 0f);
        }

        private void BuildInputArea(RectTransform root)
        {
            var inputArea = CreateRect(
                "InputArea",
                root,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(24f, 24f),
                new Vector2(-24f, 180f));

            var backButton = CreateRect(
                "BackButton",
                inputArea,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, -42f),
                new Vector2(84f, 42f));

            var backButtonImage = backButton.gameObject.AddComponent<Image>();
            backButtonImage.sprite = GetRoundedSprite();
            backButtonImage.type = Image.Type.Sliced;
            backButtonImage.color = TabColor;
            backButtonImage.raycastTarget = true;

            var backBorder = backButton.gameObject.AddComponent<Outline>();
            backBorder.effectColor = BorderColor;
            backBorder.effectDistance = new Vector2(1f, -1f);
            backBorder.useGraphicAlpha = true;

            CreateText("BackArrow", backButton, "◀", 52, Color.white, TextAnchor.MiddleCenter);

            var inputBubble = CreateRect(
                "InputBubble",
                inputArea,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(100f, -42f),
                new Vector2(0f, 42f));

            var inputImage = inputBubble.gameObject.AddComponent<Image>();
            inputImage.sprite = GetRoundedSprite();
            inputImage.type = Image.Type.Sliced;
            inputImage.color = Color.white;
            inputImage.raycastTarget = false;

            var inputBorder = inputBubble.gameObject.AddComponent<Outline>();
            inputBorder.effectColor = BorderColor;
            inputBorder.effectDistance = new Vector2(1f, -1f);
            inputBorder.useGraphicAlpha = true;

            var placeholder = CreateText("Placeholder", inputBubble, inputPlaceholder, 36, BorderColor, TextAnchor.MiddleLeft);
            var placeholderRect = placeholder.rectTransform;
            placeholderRect.offsetMin = new Vector2(30f, 0f);
            placeholderRect.offsetMax = new Vector2(-90f, 0f);

            var send = CreateText("SendIcon", inputBubble, "▶", 54, Color.black, TextAnchor.MiddleCenter);
            var sendRect = send.rectTransform;
            sendRect.anchorMin = new Vector2(1f, 0f);
            sendRect.anchorMax = new Vector2(1f, 1f);
            sendRect.offsetMin = new Vector2(-84f, 0f);
            sendRect.offsetMax = Vector2.zero;
        }

        private void ApplyTexts(RectTransform root)
        {
            SetTextAtPath(root, "Header/StatusText", statusBarText);
            SetTextAtPath(root, "Tabs/AllTab/Label", tabAllText);
            SetTextAtPath(root, "Tabs/GuildTab/Label", tabGuildText);
            SetTextAtPath(root, "Tabs/RoomTab/Label", tabRoomText);
            SetTextAtPath(root, "InputArea/InputBubble/Placeholder", inputPlaceholder);
            SetTextAtPath(root, "MessageArea/Row1/IconCircle/IconLabel", iconText);
            SetTextAtPath(root, "MessageArea/Row2/IconCircle/IconLabel", iconText);
            SetTextAtPath(root, "MessageArea/Row3/IconCircle/IconLabel", iconText);
            SetTextAtPath(root, "MessageArea/Row4/IconCircle/IconLabel", iconText);
            SetTextAtPath(root, "MessageArea/Row1/Bubble/MessageText", leftMessageText);
            SetTextAtPath(root, "MessageArea/Row2/Bubble/MessageText", rightMessageText);
            SetTextAtPath(root, "MessageArea/Row3/Bubble/MessageText", rightMessageText);
            SetTextAtPath(root, "MessageArea/Row4/Bubble/MessageText", leftMessageText);
        }

        private void EnsureBackButtonBinding(RectTransform root)
        {
            var buttonTransform = root.Find("InputArea/BackButton");
            if (buttonTransform == null)
            {
                return;
            }

            var buttonImage = buttonTransform.GetComponent<Image>();
            var button = buttonTransform.GetComponent<Button>();
            if (button == null)
            {
                button = buttonTransform.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveListener(HandleBackPressed);
            button.onClick.AddListener(HandleBackPressed);
            _backButton = button;
        }

        private void HandleBackPressed()
        {
            var router = ResolvePageRouter();
            if (router == null)
            {
                return;
            }

            if (router.Contains(UIPageType.Home))
            {
                router.Navigate(UIPageType.Home);
            }
        }

        private UIPageRouter ResolvePageRouter()
        {
            if (pageRouter != null)
            {
                return pageRouter;
            }

            pageRouter = GetComponentInParent<Canvas>()?.GetComponentInChildren<UIPageRouter>(true);
            if (pageRouter != null)
            {
                return pageRouter;
            }

            pageRouter = FindFirstObjectByType<UIPageRouter>(FindObjectsInactive.Include);
            return pageRouter;
        }

        private void SetTextAtPath(Transform parent, string relativePath, string value)
        {
            var target = parent.Find(relativePath);
            if (target == null)
            {
                return;
            }

            var text = target.GetComponent<Text>();
            if (text != null)
            {
                text.text = value;
            }
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

        private Font GetFont()
        {
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return _font;
        }

        private Sprite GetRoundedSprite()
        {
            if (_roundedSprite == null)
            {
                _roundedSprite = CreateRoundedFallbackSprite();
            }

            return _roundedSprite;
        }

        private Sprite GetCircleSprite()
        {
            if (_circleSprite == null)
            {
                _circleSprite = CreateCircleFallbackSprite();
            }

            return _circleSprite;
        }

        private Sprite CreateCircleFallbackSprite()
        {
            const int size = 128;
            const float edge = 1.6f;

            _circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeChatCircleSprite",
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
            const int border = 16;
            const float cornerRadius = 18f;
            const float edge = 1.5f;

            _roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeChatRoundedSprite",
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
