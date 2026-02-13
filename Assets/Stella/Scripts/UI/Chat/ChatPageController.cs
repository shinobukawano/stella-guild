using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using StellaGuild.Design;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StellaGuild.UI.Chat
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ChatPageController : UIPage
    {
        private readonly struct ChatMessageEntry
        {
            public ChatMessageEntry(bool isLeft, string text)
            {
                IsLeft = isLeft;
                Text = text;
            }

            public bool IsLeft { get; }
            public string Text { get; }
        }

        [SerializeField] private UIPageRouter pageRouter;
        [SerializeField] private string statusBarText = "ステータスバー";
        [SerializeField] private string tabAllText = "全体";
        [SerializeField] private string tabGuildText = "ギルド";
        [SerializeField] private string tabRoomText = "ルーム";
        [SerializeField] private string iconText = "アイコン";
        [SerializeField] private string leftMessageText = "好きなビールは黒ラベル！";
        [SerializeField] private string rightMessageText = "こんにちは！うめはらです！";
        [SerializeField] private string inputPlaceholder = "優しいコメントを入力 ....";
        [SerializeField] private Sprite leftSpeakerIconSprite;
        [SerializeField] private Sprite rightSpeakerIconSprite;
        [SerializeField] private bool rebuildLayout;

        private const string RootName = "ChatPageRoot_v20260213_inputsend_v2";
        private const string RootNamePrefix = "ChatPageRoot_";
        private const float ChatTextScale = 1.12f;
        private const float MessageRowHeight = 122f;
        private const float MessageIconDiameter = 84f;
        private const float MessageBubbleHalfHeight = 40f;
        private const float MessageBottomOffsetY = -30f;
        private const float MessageRowSpacing = 110f;
        private const int MaxVisibleRows = 5;
        private const int MaxHistoryCount = 50;
        private const string LeftSpeakerIconAssetPath = "Assets/Stella/UI/icon.jpg";
        private const string RightSpeakerIconAssetPath = "Assets/Stella/UI/icon-ume.png";
        private const string LeftSpeakerIconFileName = "icon.jpg";
        private const string RightSpeakerIconFileName = "icon-ume.png";
        private static readonly Vector2 AvatarPadding = new(4f, 4f);
        private const string TopSafeAreaFillName = "TopSafeAreaFill";
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
        private Button _sendButton;
        private InputField _messageInputField;
        private RectTransform _messageArea;
        private Coroutine _reactivateInputCoroutine;
        private readonly List<ChatMessageEntry> _messageHistory = new();
        private readonly Dictionary<string, Sprite> _uiFileSpriteCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<Texture2D> _uiFileTextures = new();

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

            if (_sendButton != null)
            {
                _sendButton.onClick.RemoveListener(HandleSendPressed);
                _sendButton = null;
            }

            if (_messageInputField != null)
            {
                _messageInputField = null;
            }

            if (_reactivateInputCoroutine != null)
            {
                StopCoroutine(_reactivateInputCoroutine);
                _reactivateInputCoroutine = null;
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
            if (Application.isPlaying)
            {
                return;
            }

            AutoAssignSpeakerSpritesInEditor();
            RemoveStaleLayoutRoots();

            var root = transform.Find(RootName) as RectTransform;
            if (root == null)
            {
                BuildLayout();
                root = transform.Find(RootName) as RectTransform;
                if (root == null)
                {
                    return;
                }
            }

            ApplyTexts(root);
            ApplyMessageIcons(root);
            EnsureBackButtonBinding(root);
            EnsureInputBindings(root);
            ApplyTopSafeArea(root);
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
            AutoAssignSpeakerSpritesInEditor();
            RemoveStaleLayoutRoots();

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
                ApplyMessageIcons(root);
                EnsureBackButtonBinding(root);
                EnsureInputBindings(root);
                ApplyTopSafeArea(root);
                return;
            }

            RemovePlaceholderChildren();
            BuildLayout();
        }

        private void RemoveStaleLayoutRoots()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (child.name == RootName || !child.name.StartsWith(RootNamePrefix, StringComparison.Ordinal))
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
            ApplyMessageIcons(root);
            EnsureBackButtonBinding(root);
            EnsureInputBindings(root);
            ApplyTopSafeArea(root);
        }

        private void ApplyTopSafeArea(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            var topInset = GetTopSafeInsetInCanvasUnits(root);

            var header = root.Find("Header") as RectTransform;
            if (header != null)
            {
                header.offsetMin = new Vector2(0f, -120f - topInset);
                header.offsetMax = new Vector2(0f, -topInset);
            }

            var tabs = root.Find("Tabs") as RectTransform;
            if (tabs != null)
            {
                tabs.offsetMin = new Vector2(36f, -252f - topInset);
                tabs.offsetMax = new Vector2(-36f, -132f - topInset);
            }

            var messageArea = root.Find("MessageArea") as RectTransform;
            if (messageArea != null)
            {
                messageArea.offsetMin = new Vector2(24f, 210f);
                messageArea.offsetMax = new Vector2(-24f, -290f - topInset);
            }

            var safeFill = root.Find(TopSafeAreaFillName) as RectTransform;
            if (safeFill == null)
            {
                safeFill = CreateRect(
                    TopSafeAreaFillName,
                    root,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, -topInset),
                    Vector2.zero);
            }
            else
            {
                safeFill.anchorMin = new Vector2(0f, 1f);
                safeFill.anchorMax = new Vector2(1f, 1f);
                safeFill.offsetMin = new Vector2(0f, -topInset);
                safeFill.offsetMax = Vector2.zero;
            }

            var fillImage = safeFill.GetComponent<Image>();
            if (fillImage == null)
            {
                fillImage = safeFill.gameObject.AddComponent<Image>();
            }

            fillImage.sprite = null;
            fillImage.type = Image.Type.Simple;
            fillImage.color = HeaderColor;
            fillImage.raycastTarget = false;
            safeFill.gameObject.SetActive(topInset > 0.5f);

            if (header != null)
            {
                safeFill.SetSiblingIndex(Mathf.Max(0, header.GetSiblingIndex()));
            }
        }

        private static float GetTopSafeInsetInCanvasUnits(RectTransform root)
        {
            if (ShouldIgnoreTopSafeInsetOnIos())
            {
                return 0f;
            }

            if (root == null)
            {
                return 0f;
            }

            var topInsetPixels = Mathf.Max(0f, Screen.height - Screen.safeArea.yMax);
            if (topInsetPixels <= 0.01f)
            {
                return 0f;
            }

            var canvas = root.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return topInsetPixels;
            }

            return topInsetPixels / Mathf.Max(0.0001f, canvas.scaleFactor);
        }

        private static bool ShouldIgnoreTopSafeInsetOnIos()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
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

            CreateText("StatusText", header, statusBarText, 62, Color.white, TextAnchor.MiddleCenter);
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

            CreateText("Label", tab, label, 58, Color.white, TextAnchor.MiddleCenter);
        }

        private void BuildMessageArea(RectTransform root)
        {
            _messageArea = CreateRect(
                "MessageArea",
                root,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(24f, 210f),
                new Vector2(-24f, -290f));

            EnsureMessageHistoryInitialized();
            RebuildMessageRows();
        }

        private void EnsureMessageHistoryInitialized()
        {
            if (!Application.isPlaying)
            {
                _messageHistory.Clear();
            }

            if (_messageHistory.Count > 0)
            {
                return;
            }

            _messageHistory.Add(new ChatMessageEntry(true, leftMessageText));
            _messageHistory.Add(new ChatMessageEntry(false, rightMessageText));
            _messageHistory.Add(new ChatMessageEntry(false, rightMessageText));
            _messageHistory.Add(new ChatMessageEntry(true, leftMessageText));
        }

        private void RebuildMessageRows()
        {
            if (_messageArea == null)
            {
                return;
            }

            EnsureMessageHistoryInitialized();

            for (var i = _messageArea.childCount - 1; i >= 0; i--)
            {
                var child = _messageArea.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            if (_messageHistory.Count == 0)
            {
                return;
            }

            var leftIcon = ResolveSpeakerSprite(LeftSpeakerIconFileName, leftSpeakerIconSprite);
            var rightIcon = ResolveSpeakerSprite(RightSpeakerIconFileName, rightSpeakerIconSprite);

            var firstVisibleIndex = Mathf.Max(0, _messageHistory.Count - MaxVisibleRows);
            var visibleCount = _messageHistory.Count - firstVisibleIndex;
            var firstOffsetY = MessageBottomOffsetY + (visibleCount - 1) * MessageRowSpacing;
            for (var i = 0; i < visibleCount; i++)
            {
                var entry = _messageHistory[firstVisibleIndex + i];
                var speakerSprite = entry.IsLeft ? leftIcon : rightIcon;
                var offsetY = firstOffsetY - i * MessageRowSpacing;
                CreateMessageRow(_messageArea, "Row" + (i + 1), offsetY, entry.IsLeft, entry.Text, speakerSprite);
            }
        }

        private void CreateMessageRow(RectTransform parent, string name, float offsetY, bool isLeftIcon, string message, Sprite speakerSprite)
        {
            var row = CreateRect(
                name,
                parent,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, offsetY),
                new Vector2(0f, offsetY + MessageRowHeight));

            var icon = CreateRect(
                "IconCircle",
                row,
                isLeftIcon ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f),
                isLeftIcon ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f),
                isLeftIcon ? new Vector2(0f, -MessageIconDiameter * 0.5f) : new Vector2(-MessageIconDiameter, -MessageIconDiameter * 0.5f),
                isLeftIcon ? new Vector2(MessageIconDiameter, MessageIconDiameter * 0.5f) : new Vector2(0f, MessageIconDiameter * 0.5f));

            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = GetCircleSprite();
            iconImage.color = BubbleColor;
            iconImage.raycastTarget = false;

            var iconBorder = icon.gameObject.AddComponent<Outline>();
            iconBorder.effectColor = BorderColor;
            iconBorder.effectDistance = new Vector2(1f, -1f);
            iconBorder.useGraphicAlpha = true;

            CreateText("IconLabel", icon, iconText, 28, BorderColor, TextAnchor.MiddleCenter);
            ApplyMessageAvatar(icon, speakerSprite);

            var bubble = CreateRect(
                "Bubble",
                row,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                isLeftIcon ? new Vector2(104f, -MessageBubbleHalfHeight) : new Vector2(64f, -MessageBubbleHalfHeight),
                isLeftIcon ? new Vector2(-64f, MessageBubbleHalfHeight) : new Vector2(-104f, MessageBubbleHalfHeight));

            var bubbleImage = bubble.gameObject.AddComponent<Image>();
            bubbleImage.sprite = GetRoundedSprite();
            bubbleImage.type = Image.Type.Sliced;
            bubbleImage.color = BubbleColor;
            bubbleImage.raycastTarget = false;

            var bubbleBorder = bubble.gameObject.AddComponent<Outline>();
            bubbleBorder.effectColor = BorderColor;
            bubbleBorder.effectDistance = new Vector2(1f, -1f);
            bubbleBorder.useGraphicAlpha = true;

            var text = CreateText("MessageText", bubble, message, 36, BorderColor, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = false;
            text.lineSpacing = 1.05f;

            var textRect = text.rectTransform;
            textRect.offsetMin = new Vector2(30f, 0f);
            textRect.offsetMax = new Vector2(-30f, 0f);
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
                new Vector2(0f, -48f),
                new Vector2(96f, 48f));

            var backButtonImage = backButton.gameObject.AddComponent<Image>();
            backButtonImage.sprite = GetRoundedSprite();
            backButtonImage.type = Image.Type.Sliced;
            backButtonImage.color = TabColor;
            backButtonImage.raycastTarget = true;

            var backBorder = backButton.gameObject.AddComponent<Outline>();
            backBorder.effectColor = BorderColor;
            backBorder.effectDistance = new Vector2(1f, -1f);
            backBorder.useGraphicAlpha = true;

            CreateText("BackArrow", backButton, "◀", 56, Color.white, TextAnchor.MiddleCenter);

            var inputBubble = CreateRect(
                "InputBubble",
                inputArea,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(112f, -48f),
                new Vector2(0f, 48f));

            var inputImage = inputBubble.gameObject.AddComponent<Image>();
            inputImage.sprite = GetRoundedSprite();
            inputImage.type = Image.Type.Sliced;
            inputImage.color = Color.white;
            inputImage.raycastTarget = true;

            var inputBorder = inputBubble.gameObject.AddComponent<Outline>();
            inputBorder.effectColor = BorderColor;
            inputBorder.effectDistance = new Vector2(1f, -1f);
            inputBorder.useGraphicAlpha = true;

            var inputText = CreateText("InputText", inputBubble, string.Empty, 38, BorderColor, TextAnchor.MiddleLeft);
            var inputTextRect = inputText.rectTransform;
            inputTextRect.offsetMin = new Vector2(32f, 0f);
            inputTextRect.offsetMax = new Vector2(-108f, 0f);
            inputText.horizontalOverflow = HorizontalWrapMode.Wrap;
            inputText.verticalOverflow = VerticalWrapMode.Truncate;
            inputText.resizeTextForBestFit = false;

            var placeholder = CreateText("Placeholder", inputBubble, inputPlaceholder, 38, BorderColor, TextAnchor.MiddleLeft);
            var placeholderRect = placeholder.rectTransform;
            placeholderRect.offsetMin = new Vector2(32f, 0f);
            placeholderRect.offsetMax = new Vector2(-108f, 0f);
            placeholder.color = new Color32(0x6E, 0x62, 0x56, 0xFF);

            var inputField = inputBubble.gameObject.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholder;
            inputField.contentType = InputField.ContentType.Standard;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.characterLimit = 120;
            inputField.targetGraphic = inputImage;
            inputField.transition = Selectable.Transition.None;
            inputField.customCaretColor = true;
            inputField.caretColor = BorderColor;
            inputField.selectionColor = new Color32(0x3A, 0x2A, 0x19, 0x66);

            var sendButton = CreateRect(
                "SendButton",
                inputBubble,
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(-96f, 0f),
                Vector2.zero);

            var sendButtonImage = sendButton.gameObject.AddComponent<Image>();
            sendButtonImage.color = new Color(1f, 1f, 1f, 0.01f);
            sendButtonImage.raycastTarget = true;

            var sendButtonComponent = sendButton.gameObject.AddComponent<Button>();
            sendButtonComponent.targetGraphic = sendButtonImage;
            sendButtonComponent.transition = Selectable.Transition.None;

            var send = CreateText("SendIcon", sendButton, "▶", 58, Color.black, TextAnchor.MiddleCenter);
            var sendRect = send.rectTransform;
            sendRect.anchorMin = Vector2.zero;
            sendRect.anchorMax = Vector2.one;
            sendRect.offsetMin = Vector2.zero;
            sendRect.offsetMax = Vector2.zero;
        }

        private void ApplyTexts(RectTransform root)
        {
            SetTextAtPath(root, "Header/StatusText", statusBarText);
            SetTextAtPath(root, "Tabs/AllTab/Label", tabAllText);
            SetTextAtPath(root, "Tabs/GuildTab/Label", tabGuildText);
            SetTextAtPath(root, "Tabs/RoomTab/Label", tabRoomText);
            SetTextAtPath(root, "InputArea/InputBubble/Placeholder", inputPlaceholder);

            if (_messageInputField != null && _messageInputField.placeholder is Text placeholderText)
            {
                placeholderText.text = inputPlaceholder;
            }
        }

        private void ApplyMessageIcons(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            if (_messageArea == null)
            {
                _messageArea = root.Find("MessageArea") as RectTransform;
            }

            RebuildMessageRows();
        }

        private void ApplyMessageAvatar(RectTransform iconCircle, Sprite sprite)
        {
            if (iconCircle == null)
            {
                return;
            }

            var iconImage = iconCircle.GetComponent<Image>();
            if (iconImage == null)
            {
                return;
            }

            var mask = iconCircle.GetComponent<Mask>();
            if (mask == null)
            {
                mask = iconCircle.gameObject.AddComponent<Mask>();
            }

            mask.showMaskGraphic = true;

            var avatar = iconCircle.Find("Avatar") as RectTransform;
            if (avatar == null)
            {
                avatar = CreateRect(
                    "Avatar",
                    iconCircle,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(AvatarPadding.x, AvatarPadding.y),
                    new Vector2(-AvatarPadding.x, -AvatarPadding.y));
            }
            else
            {
                avatar.anchorMin = Vector2.zero;
                avatar.anchorMax = Vector2.one;
                avatar.offsetMin = new Vector2(AvatarPadding.x, AvatarPadding.y);
                avatar.offsetMax = new Vector2(-AvatarPadding.x, -AvatarPadding.y);
                avatar.anchoredPosition = Vector2.zero;
            }

            avatar.SetAsLastSibling();

            var avatarImage = avatar.GetComponent<Image>();
            if (avatarImage == null)
            {
                avatarImage = avatar.gameObject.AddComponent<Image>();
            }

            avatarImage.raycastTarget = false;

            var iconLabel = iconCircle.Find("IconLabel");
            if (sprite == null)
            {
                avatar.gameObject.SetActive(false);
                if (iconLabel != null)
                {
                    iconLabel.gameObject.SetActive(true);
                }

                return;
            }

            avatar.gameObject.SetActive(true);
            avatarImage.sprite = sprite;
            avatarImage.type = Image.Type.Simple;
            avatarImage.preserveAspect = false;
            avatarImage.color = Color.white;

            var avatarAspectFitter = avatar.GetComponent<AspectRatioFitter>();
            if (avatarAspectFitter == null)
            {
                avatarAspectFitter = avatar.gameObject.AddComponent<AspectRatioFitter>();
            }

            avatarAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            avatarAspectFitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);

            if (iconLabel != null)
            {
                iconLabel.gameObject.SetActive(false);
            }
        }

        private Sprite ResolveSpeakerSprite(string fileName, Sprite directSprite)
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
            catch (IOException)
            {
                _uiFileSpriteCache[fileName] = null;
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "RuntimeChatAvatar_" + fileName,
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

#if UNITY_EDITOR
        private void AutoAssignSpeakerSpritesInEditor()
        {
            EnsureTextureImportedAsSprite(LeftSpeakerIconAssetPath);
            EnsureTextureImportedAsSprite(RightSpeakerIconAssetPath);
            leftSpeakerIconSprite = LoadSpriteIfMissing(leftSpeakerIconSprite, LeftSpeakerIconAssetPath);
            rightSpeakerIconSprite = LoadSpriteIfMissing(rightSpeakerIconSprite, RightSpeakerIconAssetPath);
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
        private void AutoAssignSpeakerSpritesInEditor()
        {
        }
#endif

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

        private void EnsureInputBindings(RectTransform root)
        {
            var inputBubble = root.Find("InputArea/InputBubble");
            if (inputBubble != null)
            {
                _messageInputField = inputBubble.GetComponent<InputField>();
                if (_messageInputField != null && _messageInputField.placeholder is Text placeholderText)
                {
                    placeholderText.text = inputPlaceholder;
                }
            }

            var sendTransform = root.Find("InputArea/InputBubble/SendButton");
            if (sendTransform == null)
            {
                return;
            }

            var sendImage = sendTransform.GetComponent<Image>();
            var sendButton = sendTransform.GetComponent<Button>();
            if (sendButton == null)
            {
                sendButton = sendTransform.gameObject.AddComponent<Button>();
            }

            sendButton.targetGraphic = sendImage;
            sendButton.transition = Selectable.Transition.None;
            sendButton.onClick.RemoveListener(HandleSendPressed);
            sendButton.onClick.AddListener(HandleSendPressed);
            _sendButton = sendButton;
        }

        private void HandleSendPressed()
        {
            TrySendCurrentMessage();
        }

        private void TrySendCurrentMessage()
        {
            if (_messageInputField == null)
            {
                return;
            }

            var message = _messageInputField.text?.Trim();
            if (string.IsNullOrEmpty(message))
            {
                _messageInputField.ActivateInputField();
                return;
            }

            _messageHistory.Add(new ChatMessageEntry(true, message));
            if (_messageHistory.Count > MaxHistoryCount)
            {
                _messageHistory.RemoveAt(0);
            }

            _messageInputField.DeactivateInputField();
            _messageInputField.text = string.Empty;
            _messageInputField.MoveTextEnd(false);
            _messageInputField.ForceLabelUpdate();
            RebuildMessageRows();
            ScheduleInputReactivate();
        }

        private void ScheduleInputReactivate()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || _messageInputField == null)
            {
                return;
            }

            if (_reactivateInputCoroutine != null)
            {
                StopCoroutine(_reactivateInputCoroutine);
            }

            _reactivateInputCoroutine = StartCoroutine(ReactivateInputFieldNextFrame());
        }

        private IEnumerator ReactivateInputFieldNextFrame()
        {
            yield return null;
            _reactivateInputCoroutine = null;

            if (_messageInputField == null || !_messageInputField.isActiveAndEnabled || !_messageInputField.interactable)
            {
                yield break;
            }

            _messageInputField.ActivateInputField();
            _messageInputField.MoveTextEnd(false);
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
            text.fontSize = Mathf.Max(12, Mathf.RoundToInt(fontSize * ChatTextScale));
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
