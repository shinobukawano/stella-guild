using System;
using System.Collections.Generic;
using System.IO;
using StellaGuild.Design;
using StellaGuild.UI.Chat;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
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
            new StatRow { label = "レベル", value = "12,345" },
            new StatRow { label = "戦力", value = "12,345" },
            new StatRow { label = "コイン", value = "12,345" }
        };

        [SerializeField] private List<StatRow> rightStats = new()
        {
            new StatRow { label = "時間", value = "12,345" },
            new StatRow { label = "人口", value = "12,345" },
            new StatRow { label = "クエスト", value = "12,345" }
        };

        [SerializeField] private string statusBarLabel = "しのねこ";
        [SerializeField] private string centerIconFallbackLabel = "アイコン";
        [SerializeField] private string guildLabel = "ギルド";
        [SerializeField] private string worldLabel = "世界";
        [SerializeField] private string postButtonLabel = "ポスト";
        [SerializeField] private string cargoButtonLabel = "荷物";
        [SerializeField] private Sprite guildButtonSprite;
        [SerializeField] private Sprite worldButtonSprite;
        [SerializeField] private Sprite postButtonSprite;
        [SerializeField] private Sprite cargoButtonSprite;
        [SerializeField] private Sprite chatButtonSprite;
        [SerializeField] private Sprite homeCenterIconSprite;
        [SerializeField] private AudioClip homeBgmClip;
        [SerializeField, Range(0f, 1f)] private float homeBgmVolume = 0.55f;
        [SerializeField] private bool rebuildLayout;

        private const string RootName = "HomeBaseRoot";
        private const string MapViewportPath = "WorldMap3DBackground";
        private const string LayoutSignatureName = "LayoutSignature_v20260213_refined_status_v2";
        private const float DesignWidth = 404f;
        private const float DesignHeight = 874f;
        private const float MainButtonDiameter = 112f;
        private const float SideActionButtonDiameter = 64f;
        private const float ChatButtonDiameter = 68f;
        private const float ChatBadgeDiameter = 22f;
        private const float GuildBackdropCenterX = 20f;
        private const float GuildBackdropCenterY = 857f;
        private const float WorldBackdropCenterX = 358f;
        private const float WorldBackdropCenterY = 857f;
        private const float GuildButtonCenterX = 54f;
        private const float GuildButtonCenterY = 820f;
        private const float WorldButtonCenterX = 350f;
        private const float WorldButtonCenterY = 820f;
        private const float SideActionButtonCenterX = 368f;
        private const float PostButtonCenterY = 694f;
        private const float CargoButtonCenterY = 590f;
        private const float SideActionLabelWidth = 120f;
        private const float SideActionLabelHeight = 56f;
        private const float SideActionLabelYOffset = 15f;
        private const int SideActionLabelFontSize = 50;
        private const float StatusRowStartY = 71f;
        private const float StatusRowSpacing = 31.5f;
        private const float StatusLabelWidth = 38f;
        private const float StatusValueWidth = 108f;
        private const float StatusRowHeight = 25f;
        private const float LeftStatusLabelStartX = 0f;
        private const float LeftStatusValueStartX = 40f;
        private const float RightStatusLabelStartX = 255f;
        private const float RightStatusValueStartX = 296f;
        private const float HomeTextScale = 1.12f;
        private const string ButtonBaseFillName = "BaseFill";
        private const string ChatTapCatcherName = "TapCatcher";
        private const string ChatTapHotspotName = "ChatTapHotspot";
        private const string TopSafeAreaFillName = "TopSafeAreaFill";
        private const float ChatTapHotspotPadding = 18f;
        private const string HomeCenterIconFileName = "icon.jpg";
        private static readonly Color32 HomeButtonBackgroundColor = new(0xC6, 0xB1, 0x98, 0xFF);
        private static readonly Color ChatButtonBackgroundColor = new(1f, 1f, 1f, 0f);
        private static readonly Color32 HomeCircleBorderColor = new(0x28, 0x19, 0x0A, 0xFF);
        private static readonly Color32 StatusBarBackgroundColor = new(0x00, 0x00, 0x00, 0xFF);
        private static readonly Vector2 MainButtonIconPadding = Vector2.zero;
        private static readonly Vector2 SideButtonIconPadding = new(2f, 2f);
        private static readonly Vector2 ChatButtonIconPadding = new(8f, 8f);
        private static readonly Vector2 HomeCenterIconPadding = new(8f, 8f);
        private static readonly string[] HomeBgmAssetPaths =
        {
            "Assets/Stella/Sound/home.wav",
            "Assets/Stella/Sound/home.mp3",
            "Assets/Stella/Sound/home.ogg",
            "Assets/Stella/Sound/home.m4a",
        };
        private static readonly HashSet<string> LoggedInvalidAudioImportPaths = new(StringComparer.OrdinalIgnoreCase);

        private Font _font;
        private Sprite _circleSprite;
        private Sprite _roundedSprite;
        private Texture2D _circleTexture;
        private Texture2D _roundedTexture;
        private readonly Dictionary<string, Sprite> _uiFileSpriteCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<Texture2D> _uiFileTextures = new();
        private Button _chatNavigationButton;
        private RectTransform _chatButtonRect;
        private RectTransform _chatTapHotspotRect;
        private AudioSource _homeBgmAudioSource;
        private bool _homeBgmMissingWarningLogged;
        private VideoPlayer _homeBgmVideoPlayer;
        private bool _homeBgmVideoPrepared;
        private bool _homeBgmVideoLoadFailed;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            EnsureLayout();
            EnsureHomeBgmPlayback();
        }

        private void Update()
        {
            HandleChatTapFallback();
        }

        private void OnDestroy()
        {
            if (_chatNavigationButton != null)
            {
                _chatNavigationButton.onClick.RemoveListener(HandleChatButtonPressed);
                _chatNavigationButton = null;
            }

            _chatButtonRect = null;
            _chatTapHotspotRect = null;
            _homeBgmAudioSource = null;
            _homeBgmVideoPrepared = false;
            _homeBgmVideoLoadFailed = false;
            if (_homeBgmVideoPlayer != null)
            {
                _homeBgmVideoPlayer.prepareCompleted -= HandleHomeBgmVideoPrepared;
                _homeBgmVideoPlayer.errorReceived -= HandleHomeBgmVideoError;
                _homeBgmVideoPlayer.Stop();
                _homeBgmVideoPlayer = null;
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
            MigrateLocalizedDefaultsIfNeeded();
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
            EnsureTextureImportedAsSprite("Assets/Stella/UI/icon.jpg");
            EnsureTextureImportedAsSprite("Assets/Stella/UI/icon.jpeg");

            guildButtonSprite = LoadSpriteIfMissing(guildButtonSprite, "Assets/Stella/UI/guild.png");
            worldButtonSprite = LoadSpriteIfMissing(worldButtonSprite, "Assets/Stella/UI/world.png");
            postButtonSprite = LoadSpriteIfMissing(postButtonSprite, "Assets/Stella/UI/post.png");
            cargoButtonSprite = LoadSpriteIfMissing(cargoButtonSprite, "Assets/Stella/UI/cargo.png");
            chatButtonSprite = LoadSpriteIfMissing(chatButtonSprite, "Assets/Stella/UI/chat.png");
            homeCenterIconSprite = LoadSpriteIfMissing(homeCenterIconSprite, "Assets/Stella/UI/icon.jpg");
            homeCenterIconSprite = LoadSpriteIfMissing(homeCenterIconSprite, "Assets/Stella/UI/icon.jpeg");
            homeBgmClip = LoadAudioClipIfMissing(homeBgmClip, HomeBgmAssetPaths);
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

        private static AudioClip LoadAudioClipIfMissing(AudioClip current, params string[] assetPaths)
        {
            if (IsUsableAudioClip(current))
            {
                return current;
            }

            if (assetPaths == null || assetPaths.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < assetPaths.Length; i++)
            {
                var path = assetPaths[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    return clip;
                }

                WarnIfAudioAssetIsNotImportable(path);
            }

            return null;
        }

        private static void WarnIfAudioAssetIsNotImportable(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                return;
            }

            if (importer is AudioImporter)
            {
                return;
            }

            if (!LoggedInvalidAudioImportPaths.Add(assetPath))
            {
                return;
            }

            Debug.LogWarning(
                "Audio asset is not imported as AudioClip: "
                + assetPath
                + ". Convert to .wav/.mp3 or fix the import settings.");
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
            var root = FindLayoutRoot();
            if (root == null)
            {
                return;
            }

            if (HasDuplicateLayoutRoots() || root.Find(MapViewportPath) == null || !HasCurrentLayoutSignature(root))
            {
                RemoveExistingLayout();
                BuildLayout();
                root = FindLayoutRoot();
                if (root == null)
                {
                    return;
                }
            }

            ApplyTopLocalizationAndLayout(root);
            ApplyBottomLocalizationAndLayout(root);
            ApplyVisualAssets(root);
            EnsureChatNavigationAndRegistration();
        }

        [ContextMenu("Rebuild Home Base Layout")]
        public void RebuildHomeBaseLayout()
        {
            rebuildLayout = false;
            RemoveExistingLayout();
            BuildLayout();
            EnsureChatNavigationAndRegistration();
        }

        [ContextMenu("Force Apply Home Button Sprites")]
        public void ForceApplyHomeButtonSprites()
        {
            AutoAssignUiSpritesInEditor();

            var root = FindLayoutRoot();
            if (root == null)
            {
                RemoveExistingLayout();
                BuildLayout();
            }
            else
            {
                ApplyTopLocalizationAndLayout(root);
                ApplyBottomLocalizationAndLayout(root);
                ApplyVisualAssets(root);
            }

            EnsureChatNavigationAndRegistration();

#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObject);
#endif
        }

        private void EnsureLayout()
        {
            MigrateLocalizedDefaultsIfNeeded();
            AutoAssignUiSpritesInEditor();

            if (rebuildLayout)
            {
                rebuildLayout = false;
                RemoveExistingLayout();
                BuildLayout();
                EnsureChatNavigationAndRegistration();
                return;
            }

            var root = FindLayoutRoot();
            if (root != null)
            {
                if (HasDuplicateLayoutRoots() || root.Find(MapViewportPath) == null || !HasCurrentLayoutSignature(root))
                {
                    RemoveExistingLayout();
                    BuildLayout();
                    EnsureChatNavigationAndRegistration();
                    return;
                }

                EnsureMapViewportComponents(root);
                ApplyTopLocalizationAndLayout(root);
                ApplyBottomLocalizationAndLayout(root);
                ApplyVisualAssets(root);

                EnsureChatNavigationAndRegistration();
                return;
            }

            RemovePlaceholderChildren();
            BuildLayout();
            EnsureChatNavigationAndRegistration();
        }

        private void EnsureHomeBgmPlayback()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureAudioListenerExists();

            var playbackClip = homeBgmClip;
            if (!IsUsableAudioClip(playbackClip))
            {
                playbackClip = null;
            }

            if (_homeBgmAudioSource == null)
            {
                _homeBgmAudioSource = GetComponent<AudioSource>();
            }

            if (_homeBgmAudioSource == null)
            {
                _homeBgmAudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (playbackClip == null)
            {
                EnsureHomeBgmVideoPlayback();

                if (!_homeBgmMissingWarningLogged)
                {
                    Debug.LogWarning(
                        "Home BGM clip is missing. Assign one of: "
                        + string.Join(", ", HomeBgmAssetPaths)
                        + " (m4a URL playback fallback enabled).",
                        this);
                    _homeBgmMissingWarningLogged = true;
                }

                return;
            }

            if (_homeBgmVideoPlayer != null && _homeBgmVideoPlayer.isPlaying)
            {
                _homeBgmVideoPlayer.Stop();
            }

            _homeBgmMissingWarningLogged = false;

            _homeBgmAudioSource.playOnAwake = false;
            _homeBgmAudioSource.loop = true;
            _homeBgmAudioSource.spatialBlend = 0f;
            _homeBgmAudioSource.volume = Mathf.Clamp01(homeBgmVolume);

            if (_homeBgmAudioSource.clip != playbackClip)
            {
                _homeBgmAudioSource.clip = playbackClip;
            }

            if (!_homeBgmAudioSource.isPlaying)
            {
                _homeBgmAudioSource.Play();
            }
        }

        private static bool IsUsableAudioClip(AudioClip clip)
        {
            if (clip == null)
            {
                return false;
            }

            if (clip.loadState == AudioDataLoadState.Failed)
            {
                return false;
            }

            return clip.samples > 0 && clip.channels > 0 && clip.frequency > 0 && clip.length > 0f;
        }

        private void EnsureHomeBgmVideoPlayback()
        {
            if (_homeBgmVideoLoadFailed)
            {
                return;
            }

            var filePath = Path.Combine(Application.dataPath, "Stella", "Sound", "home.m4a");
            if (!File.Exists(filePath))
            {
                _homeBgmVideoLoadFailed = true;
                Debug.LogWarning("Home BGM fallback file was not found: " + filePath, this);
                return;
            }

            if (_homeBgmAudioSource == null)
            {
                _homeBgmAudioSource = GetComponent<AudioSource>();
            }

            if (_homeBgmAudioSource == null)
            {
                _homeBgmAudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (_homeBgmVideoPlayer == null)
            {
                _homeBgmVideoPlayer = GetComponent<VideoPlayer>();
            }

            if (_homeBgmVideoPlayer == null)
            {
                _homeBgmVideoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            _homeBgmAudioSource.playOnAwake = false;
            _homeBgmAudioSource.loop = true;
            _homeBgmAudioSource.spatialBlend = 0f;
            _homeBgmAudioSource.volume = Mathf.Clamp01(homeBgmVolume);

            _homeBgmVideoPlayer.playOnAwake = false;
            _homeBgmVideoPlayer.isLooping = true;
            _homeBgmVideoPlayer.skipOnDrop = true;
            _homeBgmVideoPlayer.waitForFirstFrame = false;
            _homeBgmVideoPlayer.renderMode = VideoRenderMode.APIOnly;
            _homeBgmVideoPlayer.source = VideoSource.Url;
            _homeBgmVideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _homeBgmVideoPlayer.SetTargetAudioSource(0, _homeBgmAudioSource);
            _homeBgmVideoPlayer.EnableAudioTrack(0, true);
            _homeBgmVideoPlayer.SetDirectAudioMute(0, false);
            _homeBgmVideoPlayer.SetDirectAudioVolume(0, Mathf.Clamp01(homeBgmVolume));

            var fileUri = new Uri(filePath).AbsoluteUri;
            if (!string.Equals(_homeBgmVideoPlayer.url, fileUri, StringComparison.Ordinal))
            {
                _homeBgmVideoPrepared = false;
                _homeBgmVideoPlayer.url = fileUri;
            }

            _homeBgmVideoPlayer.prepareCompleted -= HandleHomeBgmVideoPrepared;
            _homeBgmVideoPlayer.errorReceived -= HandleHomeBgmVideoError;
            _homeBgmVideoPlayer.prepareCompleted += HandleHomeBgmVideoPrepared;
            _homeBgmVideoPlayer.errorReceived += HandleHomeBgmVideoError;

            if (_homeBgmVideoPrepared)
            {
                if (!_homeBgmVideoPlayer.isPlaying)
                {
                    _homeBgmVideoPlayer.Play();
                }

                return;
            }

            if (!_homeBgmVideoPlayer.isPrepared)
            {
                _homeBgmVideoPlayer.Prepare();
                return;
            }

            _homeBgmVideoPrepared = true;
            _homeBgmVideoPlayer.Play();
        }

        private void HandleHomeBgmVideoPrepared(VideoPlayer source)
        {
            _homeBgmVideoPrepared = true;
            if (source != null && !source.isPlaying)
            {
                source.Play();
            }
        }

        private void HandleHomeBgmVideoError(VideoPlayer source, string message)
        {
            _homeBgmVideoLoadFailed = true;
            Debug.LogWarning("Home BGM fallback video playback failed: " + message, this);
        }

        private void EnsureAudioListenerExists()
        {
            var existingListener = FindFirstObjectByType<AudioListener>(FindObjectsInactive.Include);
            if (existingListener != null)
            {
                return;
            }

            var listener = GetComponent<AudioListener>();
            if (listener == null)
            {
                listener = gameObject.AddComponent<AudioListener>();
            }

            listener.enabled = true;
        }

        private void EnsureChatNavigationAndRegistration()
        {
            var root = FindLayoutRoot();
            if (root != null)
            {
                ApplyTopSafeArea(root);
                EnsureChatButtonBinding(root);
            }

            var router = ResolvePageRouter();
            if (router == null)
            {
                return;
            }

            var chatPage = EnsureRuntimeChatPage();
            if (chatPage != null)
            {
                router.RegisterRuntimePage(UIPageType.Chat, chatPage);
            }
        }

        private void ApplyTopSafeArea(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            var topInset = GetTopSafeInsetInCanvasUnits(root);
            var topPanel = root.Find("TopPanel") as RectTransform;
            if (topPanel != null)
            {
                topPanel.offsetMin = new Vector2(0f, -topInset);
                topPanel.offsetMax = new Vector2(0f, -topInset);
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

            ConfigurePanelImage(fillImage, StatusBarBackgroundColor);
            safeFill.gameObject.SetActive(topInset > 0.5f);

            if (topPanel != null)
            {
                safeFill.SetSiblingIndex(Mathf.Max(0, topPanel.GetSiblingIndex()));
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

        private void EnsureChatButtonBinding(Transform root)
        {
            var chatButtonRect = root.Find("MainArea/ChatButton") as RectTransform;
            if (chatButtonRect == null)
            {
                return;
            }

            _chatButtonRect = chatButtonRect;

            var chatButtonImage = chatButtonRect.GetComponent<Image>();
            var button = chatButtonRect.GetComponent<Button>();
            if (button == null)
            {
                button = chatButtonRect.gameObject.AddComponent<Button>();
            }

            var targetGraphic = chatButtonRect.Find(ButtonBaseFillName)?.GetComponent<Image>();
            if (targetGraphic == null)
            {
                targetGraphic = chatButtonImage;
            }

            if (targetGraphic == null)
            {
                return;
            }

            targetGraphic.raycastTarget = true;
            if (chatButtonImage != null && chatButtonImage != targetGraphic)
            {
                chatButtonImage.raycastTarget = false;
            }

            button.targetGraphic = targetGraphic;
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveListener(HandleChatButtonPressed);
            button.onClick.AddListener(HandleChatButtonPressed);

            var tapCatcher = chatButtonRect.Find(ChatTapCatcherName) as RectTransform;
            if (tapCatcher == null)
            {
                tapCatcher = CreateRect(ChatTapCatcherName, chatButtonRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            else
            {
                tapCatcher.anchorMin = Vector2.zero;
                tapCatcher.anchorMax = Vector2.one;
                tapCatcher.offsetMin = Vector2.zero;
                tapCatcher.offsetMax = Vector2.zero;
                tapCatcher.anchoredPosition = Vector2.zero;
            }

            tapCatcher.SetAsLastSibling();
            var tapImage = tapCatcher.GetComponent<Image>();
            if (tapImage == null)
            {
                tapImage = tapCatcher.gameObject.AddComponent<Image>();
            }

            tapImage.sprite = null;
            tapImage.type = Image.Type.Simple;
            tapImage.color = new Color(1f, 1f, 1f, 0.003f);
            tapImage.raycastTarget = true;

            var tapButton = tapCatcher.GetComponent<Button>();
            if (tapButton == null)
            {
                tapButton = tapCatcher.gameObject.AddComponent<Button>();
            }

            tapButton.targetGraphic = tapImage;
            tapButton.transition = Selectable.Transition.None;
            tapButton.onClick.RemoveListener(HandleChatButtonPressed);
            tapButton.onClick.AddListener(HandleChatButtonPressed);
            _chatNavigationButton = tapButton;
            EnsureRootChatHotspotBinding(root, chatButtonRect);
        }

        private void EnsureRootChatHotspotBinding(Transform root, RectTransform chatButtonRect)
        {
            if (root == null || chatButtonRect == null)
            {
                _chatTapHotspotRect = null;
                return;
            }

            var mainArea = root.Find("MainArea") as RectTransform;
            if (mainArea == null)
            {
                _chatTapHotspotRect = null;
                return;
            }

            var hotspot = mainArea.Find(ChatTapHotspotName) as RectTransform;
            if (hotspot == null)
            {
                hotspot = CreateRect(ChatTapHotspotName, mainArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            hotspot.anchorMin = chatButtonRect.anchorMin;
            hotspot.anchorMax = chatButtonRect.anchorMax;
            hotspot.offsetMin = chatButtonRect.offsetMin - new Vector2(ChatTapHotspotPadding, ChatTapHotspotPadding);
            hotspot.offsetMax = chatButtonRect.offsetMax + new Vector2(ChatTapHotspotPadding, ChatTapHotspotPadding);
            hotspot.anchoredPosition = chatButtonRect.anchoredPosition;
            hotspot.localScale = Vector3.one;
            hotspot.localRotation = Quaternion.identity;
            hotspot.SetAsLastSibling();

            var hotspotImage = hotspot.GetComponent<Image>();
            if (hotspotImage == null)
            {
                hotspotImage = hotspot.gameObject.AddComponent<Image>();
            }

            hotspotImage.sprite = null;
            hotspotImage.type = Image.Type.Simple;
            hotspotImage.color = new Color(1f, 1f, 1f, 0.003f);
            hotspotImage.raycastTarget = true;

            var hotspotButton = hotspot.GetComponent<Button>();
            if (hotspotButton == null)
            {
                hotspotButton = hotspot.gameObject.AddComponent<Button>();
            }

            hotspotButton.targetGraphic = hotspotImage;
            hotspotButton.transition = Selectable.Transition.None;
            hotspotButton.onClick.RemoveListener(HandleChatButtonPressed);
            hotspotButton.onClick.AddListener(HandleChatButtonPressed);
            _chatTapHotspotRect = hotspot;
        }

        private void HandleChatTapFallback()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null && !canvasGroup.interactable)
            {
                return;
            }

            if (_chatButtonRect == null || _chatTapHotspotRect == null)
            {
                var root = FindLayoutRoot();
                if (root == null)
                {
                    return;
                }

                EnsureChatButtonBinding(root);
                if (_chatButtonRect == null && _chatTapHotspotRect == null)
                {
                    return;
                }
            }

            if (!TryGetPointerDownPosition(out var pointerPosition))
            {
                return;
            }

            if (ContainsPointer(_chatTapHotspotRect, pointerPosition) || ContainsPointer(_chatButtonRect, pointerPosition))
            {
                HandleChatButtonPressed();
            }
        }

        private static bool ContainsPointer(RectTransform rect, Vector2 pointerPosition)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy)
            {
                return false;
            }

            var eventCamera = ResolveEventCamera(rect);
            return RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition, eventCamera);
        }

        private static Camera ResolveEventCamera(Component component)
        {
            if (component == null)
            {
                return null;
            }

            var canvas = component.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        private static bool TryGetPointerDownPosition(out Vector2 pointerPosition)
        {
            pointerPosition = default;

#if ENABLE_INPUT_SYSTEM
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                pointerPosition = pointer.position.ReadValue();
                return true;
            }

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.wasPressedThisFrame || touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    pointerPosition = touch.position.ReadValue();
                    return true;
                }

                var touches = touchscreen.touches;
                for (var i = 0; i < touches.Count; i++)
                {
                    var extraTouch = touches[i];
                    if (extraTouch.press.wasPressedThisFrame || extraTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        pointerPosition = extraTouch.position.ReadValue();
                        return true;
                    }
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                pointerPosition = mouse.position.ReadValue();
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    pointerPosition = touch.position;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                pointerPosition = Input.mousePosition;
                return true;
            }
#endif

            return false;
        }

        private ChatPageController EnsureRuntimeChatPage()
        {
            var pageParent = transform.parent as RectTransform;
            if (pageParent == null)
            {
                return null;
            }

            var chatPageTransform = pageParent.Find("ChatPage") as RectTransform;
            if (chatPageTransform == null)
            {
                chatPageTransform = CreateRect("ChatPage", pageParent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            var chatPageImage = chatPageTransform.GetComponent<Image>();
            if (chatPageImage == null)
            {
                chatPageImage = chatPageTransform.gameObject.AddComponent<Image>();
            }

            chatPageImage.sprite = null;
            chatPageImage.type = Image.Type.Simple;
            chatPageImage.color = StellaColorTokens.Get(ColorToken.BaseBackground);
            chatPageImage.raycastTarget = false;

            if (chatPageTransform.GetComponent<CanvasGroup>() == null)
            {
                chatPageTransform.gameObject.AddComponent<CanvasGroup>();
            }

            var chatPageController = chatPageTransform.GetComponent<ChatPageController>();
            if (chatPageController == null)
            {
                chatPageController = chatPageTransform.gameObject.AddComponent<ChatPageController>();
            }

            return chatPageController;
        }

        private void HandleChatButtonPressed()
        {
            var router = ResolvePageRouter();
            var navigated = false;
            if (router != null)
            {
                if (!router.Contains(UIPageType.Chat))
                {
                    var chatPage = EnsureRuntimeChatPage();
                    if (chatPage != null)
                    {
                        router.RegisterRuntimePage(UIPageType.Chat, chatPage);
                    }
                }

                if (router.Contains(UIPageType.Chat))
                {
                    router.Navigate(UIPageType.Chat);
                    navigated = true;
                }
            }

            if (!navigated)
            {
                ForceNavigateToChatWithoutRouter();
            }
        }

        private void ForceNavigateToChatWithoutRouter()
        {
            var chatPage = EnsureRuntimeChatPage();
            if (chatPage == null)
            {
                chatPage = FindFirstObjectByType<ChatPageController>(FindObjectsInactive.Include);
            }

            if (chatPage == null)
            {
                return;
            }

            Hide();
            chatPage.Show();
        }

        private UIPageRouter ResolvePageRouter()
        {
            var router = GetComponentInParent<Canvas>()?.GetComponentInChildren<UIPageRouter>(true);
            if (router != null)
            {
                return router;
            }

            return FindFirstObjectByType<UIPageRouter>(FindObjectsInactive.Include);
        }

        private void MigrateLocalizedDefaultsIfNeeded()
        {
            statusBarLabel = ToJapaneseIfEnglish(statusBarLabel);
            if (statusBarLabel == "ステータス" || statusBarLabel == "ステータスバー")
            {
                statusBarLabel = "しのねこ";
            }

            centerIconFallbackLabel = ToJapaneseIfEnglish(centerIconFallbackLabel);
            guildLabel = ToJapaneseIfEnglish(guildLabel);
            worldLabel = ToJapaneseIfEnglish(worldLabel);
            postButtonLabel = ToJapaneseIfEnglish(postButtonLabel);
            cargoButtonLabel = ToJapaneseIfEnglish(cargoButtonLabel);
            MigrateStatRowsIfNeeded(leftStats);
            MigrateStatRowsIfNeeded(rightStats);
        }

        private static void MigrateStatRowsIfNeeded(List<StatRow> rows)
        {
            if (rows == null)
            {
                return;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                row.label = ToJapaneseIfEnglish(row.label);
                row.value = ToJapaneseIfEnglish(row.value);
                if (row.label == "クエスト" && row.value == "一覧")
                {
                    row.value = "12,345";
                }

                rows[i] = row;
            }
        }

        private static string ToJapaneseIfEnglish(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value switch
            {
                "Status Bar" => "しのねこ",
                "Icon" => "アイコン",
                "Guild" => "ギルド",
                "World" => "世界",
                "Post" => "ポスト",
                "Cargo" => "荷物",
                "Level" => "レベル",
                "Power" => "戦力",
                "Coins" => "コイン",
                "Time" => "時間",
                "Population" => "人口",
                "Quest" => "クエスト",
                "List" => "一覧",
                "Settings" => "設定",
                "Start" => "開始",
                _ => value
            };
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
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name != RootName)
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

        private bool HasCurrentLayoutSignature(Transform root)
        {
            return root != null && root.Find(LayoutSignatureName) != null;
        }

        private RectTransform FindLayoutRoot()
        {
            RectTransform fallback = null;
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == RootName)
                {
                    if (!child.gameObject.activeInHierarchy)
                    {
                        fallback ??= child as RectTransform;
                        continue;
                    }

                    if (child.Find(LayoutSignatureName) != null)
                    {
                        return child as RectTransform;
                    }

                    fallback ??= child as RectTransform;
                }
            }

            return fallback;
        }

        private bool HasDuplicateLayoutRoots()
        {
            var count = 0;
            for (var i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).name != RootName)
                {
                    continue;
                }

                count++;
                if (count > 1)
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateLayoutSignature(RectTransform root)
        {
            if (root == null || root.Find(LayoutSignatureName) != null)
            {
                return;
            }

            var signature = CreateRect(LayoutSignatureName, root, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            signature.sizeDelta = Vector2.zero;
            signature.gameObject.SetActive(false);
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
            CreateLayoutSignature(root);
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
            var topPanel = CreateRect("TopPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panelBase = CreateDesignRect("PanelBase", topPanel, 0f, 54f, 404f, 136f);
            var panelBaseImage = panelBase.gameObject.AddComponent<Image>();
            ConfigurePanelImage(panelBaseImage, HomeButtonBackgroundColor);

            var panelRightDrop = CreateDesignRect("PanelRightDrop", topPanel, 282f, 159f, 122f, 31f);
            var panelRightDropImage = panelRightDrop.gameObject.AddComponent<Image>();
            ConfigureRoundedImage(panelRightDropImage, HomeButtonBackgroundColor);

            var panelRightCorner = CreateDesignCircle("PanelRightCorner", topPanel, 388f, 189f, 36f, HomeButtonBackgroundColor);
            panelRightCorner.SetAsLastSibling();
            var panelLeftCorner = CreateDesignCircle("PanelLeftCorner", topPanel, 16f, 189f, 32f, HomeButtonBackgroundColor);
            panelLeftCorner.SetAsLastSibling();

            var header = CreateDesignRect("Header", topPanel, 0f, 0f, 404f, 55f);
            var headerImage = header.gameObject.AddComponent<Image>();
            ConfigurePanelImage(headerImage, StatusBarBackgroundColor);

            var headerLabel = CreateText("HeaderLabel", header, statusBarLabel, 62, StellaColorTokens.Get(ColorToken.BaseBackground), TextAnchor.MiddleCenter);
            AddTextShadow(headerLabel, new Color(0f, 0f, 0f, 0.24f));
            header.SetAsLastSibling();

            BuildStatusColumns(topPanel);
            BuildCenterIcon(topPanel);
        }

        private void BuildStatusColumns(RectTransform topPanel)
        {
            BuildStatusColumn(topPanel, leftStats, false);
            BuildStatusColumn(topPanel, rightStats, true);
        }

        private void BuildStatusColumn(RectTransform parent, List<StatRow> rows, bool alignRight)
        {
            var column = CreateRect(alignRight ? "RightStats" : "LeftStats", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var labelStartX = alignRight ? RightStatusLabelStartX : LeftStatusLabelStartX;
            var valueStartX = alignRight ? RightStatusValueStartX : LeftStatusValueStartX;

            for (var i = 0; i < Mathf.Min(3, rows.Count); i++)
            {
                var row = rows[i];
                var y = StatusRowStartY + i * StatusRowSpacing;

                var labelRect = CreateDesignRect(
                    $"{(alignRight ? "R" : "L")}Label{i}",
                    column,
                    labelStartX,
                    y,
                    StatusLabelWidth,
                    StatusRowHeight);
                var labelImage = labelRect.gameObject.AddComponent<Image>();
                ConfigureRoundedImage(labelImage, Color.black);
                CreateText("LabelText", labelRect, row.label, 16, StellaColorTokens.Get(ColorToken.BaseBackground), TextAnchor.MiddleCenter);

                var valueRect = CreateDesignRect(
                    $"{(alignRight ? "R" : "L")}Value{i}",
                    column,
                    valueStartX,
                    y,
                    StatusValueWidth,
                    StatusRowHeight);
                var valueImage = valueRect.gameObject.AddComponent<Image>();
                ConfigureRoundedImage(valueImage, Color.white);
                CreateText("ValueText", valueRect, row.value, 32, StellaColorTokens.Get(ColorToken.TextShadow), TextAnchor.MiddleCenter);
            }
        }

        private void BuildCenterIcon(RectTransform topPanel)
        {
            var ring = CreateDesignCircle("CenterIconRing", topPanel, 202f, 109f, 82f, HomeCircleBorderColor);
            var plate = CreateDesignCircle("CenterIconPlate", topPanel, 202f, 109f, 68f, HomeButtonBackgroundColor);
            plate.SetSiblingIndex(ring.GetSiblingIndex() + 1);

            var iconLabel = CreateText("CenterIconLabel", plate, centerIconFallbackLabel, 46, StellaColorTokens.Get(ColorToken.BaseBackground), TextAnchor.MiddleCenter);
            AddTextShadow(iconLabel, StellaColorTokens.Get(ColorToken.TextShadow));
        }

        private void BuildMainArea(RectTransform root)
        {
            var mainArea = CreateRect("MainArea", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            const float chatButtonCenterX = 47f;
            const float chatButtonCenterY = 247f;

            var chatButton = CreateDesignCircle("ChatButton", mainArea, chatButtonCenterX, chatButtonCenterY, ChatButtonDiameter, ChatButtonBackgroundColor);
            var chatText = CreateText("ChatDots", chatButton, "...", 52, StellaColorTokens.Get(ColorToken.TextShadow), TextAnchor.MiddleCenter);
            AddTextShadow(chatText, new Color(0f, 0f, 0f, 0.2f));

            var badge = CreateDesignCircle("Badge", mainArea, chatButtonCenterX + 24f, chatButtonCenterY - 24f, ChatBadgeDiameter, StellaColorTokens.Get(ColorToken.Attention));
            var badgeText = CreateText("BadgeText", badge, "1", 22, Color.white, TextAnchor.MiddleCenter);
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
            SetDesignCircleAtPath(bottom, "GuildBackdrop", GuildBackdropCenterX, GuildBackdropCenterY, 172f);
            SetDesignCircleAtPath(bottom, "WorldBackdrop", WorldBackdropCenterX, WorldBackdropCenterY, 172f);

            SetDesignCircleAtPath(bottom, "GuildButton", GuildButtonCenterX, GuildButtonCenterY, MainButtonDiameter);
            SetDesignCircleAtPath(bottom, "WorldButton", WorldButtonCenterX, WorldButtonCenterY, MainButtonDiameter);

            SetDesignCircleAtPath(bottom, "PostButton", SideActionButtonCenterX, PostButtonCenterY, SideActionButtonDiameter);
            SetDesignCircleAtPath(bottom, "CargoButton", SideActionButtonCenterX, CargoButtonCenterY, SideActionButtonDiameter);
            SetDesignRectAtPath(bottom, "PostLabel", GetSideActionLabelX(), GetSideActionLabelY(PostButtonCenterY), SideActionLabelWidth, SideActionLabelHeight);
            SetDesignRectAtPath(bottom, "CargoLabel", GetSideActionLabelX(), GetSideActionLabelY(CargoButtonCenterY), SideActionLabelWidth, SideActionLabelHeight);
        }

        private void ApplyTopLocalizationAndLayout(Transform root)
        {
            var topPanel = root.Find("TopPanel");
            if (topPanel == null)
            {
                return;
            }

            SetTextAtPath(topPanel, "Header/HeaderLabel", statusBarLabel);
            SetTextAtPath(topPanel, "CenterIconPlate/CenterIconLabel", centerIconFallbackLabel);
            ApplyStatusColumnText(topPanel.Find("LeftStats"), leftStats, false);
            ApplyStatusColumnText(topPanel.Find("RightStats"), rightStats, true);
        }

        private void ApplyStatusColumnText(Transform column, List<StatRow> rows, bool alignRight)
        {
            if (column == null || rows == null)
            {
                return;
            }

            var prefix = alignRight ? "R" : "L";
            var rowCount = Mathf.Min(3, rows.Count);
            for (var i = 0; i < rowCount; i++)
            {
                SetTextAtPath(column, $"{prefix}Label{i}/LabelText", rows[i].label);
                SetTextAtPath(column, $"{prefix}Value{i}/ValueText", rows[i].value);
            }
        }

        private void ApplyVisualAssets(Transform root)
        {
            var topPanel = root.Find("TopPanel");
            if (topPanel != null)
            {
                ApplyCircleColor(topPanel, "CenterIconRing", HomeCircleBorderColor);
                var hasCenterIcon = ApplyMaskedCircleIcon(topPanel, "CenterIconPlate", "Icon", HomeCenterIconFileName, homeCenterIconSprite, HomeCenterIconPadding);
                var centerIconLabel = topPanel.Find("CenterIconPlate/CenterIconLabel");
                if (centerIconLabel != null)
                {
                    centerIconLabel.gameObject.SetActive(!hasCenterIcon);
                }
            }

            var mainArea = root.Find("MainArea");
            if (mainArea != null)
            {
                ApplyCircleButtonBase(mainArea, "ChatButton", ChatButtonBackgroundColor);
                SetElementActive(mainArea, "ChatButton/Border", false);
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

        private bool ApplyMaskedCircleIcon(
            Transform parent,
            string plateName,
            string overlayName,
            string fileName,
            Sprite directSprite,
            Vector2 padding)
        {
            var plate = parent.Find(plateName) as RectTransform;
            if (plate == null)
            {
                return false;
            }

            var sprite = ResolveButtonSprite(fileName, directSprite);
            if (sprite == null)
            {
                return false;
            }

            var plateImage = plate.GetComponent<Image>();
            if (plateImage == null)
            {
                return false;
            }

            plateImage.sprite = GetCircleSprite();
            plateImage.type = Image.Type.Simple;
            plateImage.preserveAspect = false;

            var mask = plate.GetComponent<Mask>();
            if (mask == null)
            {
                mask = plate.gameObject.AddComponent<Mask>();
            }

            mask.showMaskGraphic = true;

            var overlay = plate.Find(overlayName) as RectTransform;
            if (overlay == null)
            {
                overlay = CreateRect(
                    overlayName,
                    plate,
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
            overlayImage.preserveAspect = false;
            overlayImage.color = Color.white;
            overlayImage.raycastTarget = false;

            var aspectRatioFitter = overlay.GetComponent<AspectRatioFitter>();
            if (aspectRatioFitter == null)
            {
                aspectRatioFitter = overlay.gameObject.AddComponent<AspectRatioFitter>();
            }

            aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspectRatioFitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
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

        private void ApplyCircleColor(Transform parent, string circleName, Color color)
        {
            var circle = parent.Find(circleName);
            if (circle == null)
            {
                return;
            }

            var image = circle.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.color = color;
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

        private void SetTextAtPath(Transform parent, string relativePath, string value)
        {
            if (parent == null || string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var textTransform = parent.Find(relativePath);
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
            var bottom = CreateRect("BottomArea", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var guildBackdrop = CreateDesignCircle("GuildBackdrop", bottom, GuildBackdropCenterX, GuildBackdropCenterY, 172f, HomeButtonBackgroundColor);
            guildBackdrop.SetAsFirstSibling();
            var worldBackdrop = CreateDesignCircle("WorldBackdrop", bottom, WorldBackdropCenterX, WorldBackdropCenterY, 172f, HomeButtonBackgroundColor);
            worldBackdrop.SetAsFirstSibling();

            var leftMain = CreateDesignCircle("GuildButton", bottom, GuildButtonCenterX, GuildButtonCenterY, MainButtonDiameter, HomeButtonBackgroundColor);
            AddCircleBorder(leftMain, StellaColorTokens.Get(ColorToken.TextShadow), 8f);

            var rightMain = CreateDesignCircle("WorldButton", bottom, WorldButtonCenterX, WorldButtonCenterY, MainButtonDiameter, HomeButtonBackgroundColor);
            AddCircleBorder(rightMain, StellaColorTokens.Get(ColorToken.TextShadow), 8f);

            var postButton = CreateDesignCircle("PostButton", bottom, SideActionButtonCenterX, PostButtonCenterY, SideActionButtonDiameter, HomeButtonBackgroundColor);
            AddCircleBorder(postButton, StellaColorTokens.Get(ColorToken.TextShadow), 6f);
            var postLabelText = CreateBottomLabel(
                "PostLabel",
                bottom,
                postButtonLabel,
                GetSideActionLabelX(),
                GetSideActionLabelY(PostButtonCenterY),
                SideActionLabelWidth,
                SideActionLabelHeight,
                SideActionLabelFontSize);
            AddTextStroke(postLabelText);

            var cargoButton = CreateDesignCircle("CargoButton", bottom, SideActionButtonCenterX, CargoButtonCenterY, SideActionButtonDiameter, HomeButtonBackgroundColor);
            AddCircleBorder(cargoButton, StellaColorTokens.Get(ColorToken.TextShadow), 6f);
            var cargoLabelText = CreateBottomLabel(
                "CargoLabel",
                bottom,
                cargoButtonLabel,
                GetSideActionLabelX(),
                GetSideActionLabelY(CargoButtonCenterY),
                SideActionLabelWidth,
                SideActionLabelHeight,
                SideActionLabelFontSize);
            AddTextStroke(cargoLabelText);
        }

        private static float GetSideActionLabelX()
        {
            return SideActionButtonCenterX - (SideActionLabelWidth * 0.5f);
        }

        private static float GetSideActionLabelY(float buttonCenterY)
        {
            return buttonCenterY + SideActionLabelYOffset;
        }

        private Text CreateBottomLabel(string name, RectTransform parent, string value, float x, float y, float width, float height, int fontSize = 62)
        {
            var labelRect = CreateDesignRect(name, parent, x, y, width, height);

            var text = CreateText("Text", labelRect, value, fontSize, Color.white, TextAnchor.MiddleCenter);
            AddTextShadow(text, StellaColorTokens.Get(ColorToken.TextShadow));
            return text;
        }

        private RectTransform CreateDesignRect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ApplyDesignRect(rect, x, y, width, height);
            return rect;
        }

        private RectTransform CreateDesignCircle(string name, Transform parent, float centerX, float centerY, float diameter, Color color)
        {
            var rect = CreateDesignRect(name, parent, centerX - diameter * 0.5f, centerY - diameter * 0.5f, diameter, diameter);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = GetCircleSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private void SetDesignCircleAtPath(Transform root, string path, float centerX, float centerY, float diameter)
        {
            var rect = root.Find(path) as RectTransform;
            if (rect == null)
            {
                return;
            }

            ApplyDesignRect(rect, centerX - diameter * 0.5f, centerY - diameter * 0.5f, diameter, diameter);
        }

        private void SetDesignRectAtPath(Transform root, string path, float x, float y, float width, float height)
        {
            var rect = root.Find(path) as RectTransform;
            if (rect == null)
            {
                return;
            }

            ApplyDesignRect(rect, x, y, width, height);
        }

        private static void ApplyDesignRect(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(x / DesignWidth, 1f - ((y + height) / DesignHeight));
            rect.anchorMax = new Vector2((x + width) / DesignWidth, 1f - (y / DesignHeight));
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
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
            text.fontSize = Mathf.Max(12, Mathf.RoundToInt(fontSize * HomeTextScale));
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
