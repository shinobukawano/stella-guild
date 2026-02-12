#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using StellaGuild.Design;
using StellaGuild.Flow;
using StellaGuild.UI;
using StellaGuild.UI.Home;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StellaGuild.EditorTools
{
    public static class StellaProjectSetupMenu
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string StageScenePath = "Assets/Scenes/Stage01.unity";
        private const string LogoFolderPath = "Assets/Stella/UI/Logos";
        private const string PreferredLogoPath = "Assets/Stella/UI/Logos/stella-guild-logo.png";
        private const float StartupLogoHorizontalPadding = 120f;

        [MenuItem("Stella/Setup/Create Bootstrap + Stage01")]
        public static void CreateBootstrapAndStage()
        {
            EnsureFolder("Assets/Scenes");

            if (!CanCreateOrOverwriteScenes())
            {
                return;
            }

            CreateStageScene();
            CreateBootstrapScene();
            ApplyBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(BootstrapScenePath);
            EditorUtility.DisplayDialog(
                "Stella Setup Completed",
                "Bootstrap and Stage01 scenes were created and added to Build Settings.",
                "OK");
        }

        [MenuItem("Stella/Setup/Apply Startup Logo To Bootstrap")]
        public static void ApplyStartupLogoToBootstrap()
        {
            if (!File.Exists(BootstrapScenePath))
            {
                EditorUtility.DisplayDialog(
                    "Bootstrap Scene Not Found",
                    "Please create the bootstrap scene first from Stella > Setup > Create Bootstrap + Stage01.",
                    "OK");
                return;
            }

            var logoSprite = LoadStartupLogoSprite();
            if (logoSprite == null)
            {
                EditorUtility.DisplayDialog(
                    "Logo Sprite Not Found",
                    "No Sprite was found in Assets/Stella/UI/Logos/. Import the logo as Sprite (2D and UI) and retry.",
                    "OK");
                return;
            }

            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            var startupLogoObject = GameObject.Find("StartupLogo");
            var appObject = GameObject.Find("App");

            if (startupLogoObject == null || appObject == null)
            {
                EditorUtility.DisplayDialog(
                    "Scene Structure Missing",
                    "StartupLogo or App object is missing. Re-run Stella > Setup > Create Bootstrap + Stage01.",
                    "OK");
                return;
            }

            var startupLogoImage = startupLogoObject.GetComponent<Image>();
            if (startupLogoImage == null)
            {
                startupLogoImage = startupLogoObject.AddComponent<Image>();
            }

            ConfigureStartupLogoBackground(startupLogoImage);

            var logoImage = GetOrCreateStartupLogoImage(startupLogoObject.transform);
            ApplyStartupLogoToImage(logoImage, logoSprite);
            logoImage.gameObject.SetActive(true);

            var logoLabelTransform = startupLogoObject.transform.Find("LogoLabel");
            if (logoLabelTransform != null)
            {
                logoLabelTransform.gameObject.SetActive(false);
            }

            var startupLogoCanvasGroup = startupLogoObject.GetComponent<CanvasGroup>();
            if (startupLogoCanvasGroup == null)
            {
                startupLogoCanvasGroup = startupLogoObject.AddComponent<CanvasGroup>();
            }

            var startupFlow = appObject.GetComponent<StartupFlowController>();
            if (startupFlow != null)
            {
                var startupFlowSerialized = new SerializedObject(startupFlow);
                startupFlowSerialized.FindProperty("startupLogoCanvasGroup").objectReferenceValue = startupLogoCanvasGroup;
                startupFlowSerialized.FindProperty("skipStartupLogo").boolValue = false;
                startupFlowSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog(
                "Startup Logo Applied",
                "Logo sprite was applied to Bootstrap and startup logo playback was enabled.",
                "OK");
        }

        [MenuItem("Stella/Setup/Apply Home Base Layout To Bootstrap")]
        public static void ApplyHomeBaseLayoutToBootstrap()
        {
            if (!File.Exists(BootstrapScenePath))
            {
                EditorUtility.DisplayDialog(
                    "Bootstrap Scene Not Found",
                    "Please create the bootstrap scene first from Stella > Setup > Create Bootstrap + Stage01.",
                    "OK");
                return;
            }

            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            var homePageObject = GameObject.Find("HomePage");
            var settingsPageObject = GameObject.Find("SettingsPage");
            var titlePageObject = GameObject.Find("TitlePage");
            var uiRootObject = GameObject.Find("UIRoot");
            var appObject = GameObject.Find("App");

            if (homePageObject == null || uiRootObject == null || appObject == null)
            {
                EditorUtility.DisplayDialog(
                    "Scene Structure Missing",
                    "HomePage, UIRoot, or App object is missing. Re-run Stella > Setup > Create Bootstrap + Stage01.",
                    "OK");
                return;
            }

            var homeBaseController = EnsureHomeBaseController(homePageObject);
            var pageRouter = uiRootObject.GetComponent<UIPageRouter>();
            if (pageRouter != null)
            {
                var settingsPage = settingsPageObject != null ? settingsPageObject.GetComponent<UIPage>() : null;
                ConfigureHomePageEntry(pageRouter, homeBaseController, settingsPage);
            }

            var startupFlow = appObject.GetComponent<StartupFlowController>();
            if (startupFlow != null)
            {
                var startupFlowSerialized = new SerializedObject(startupFlow);
                startupFlowSerialized.FindProperty("startDestinationPage").enumValueIndex = (int)UIPageType.Home;
                startupFlowSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            if (titlePageObject != null)
            {
                UnityEngine.Object.DestroyImmediate(titlePageObject);
            }

            homeBaseController.RebuildHomeBaseLayout();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog(
                "Home Base Applied",
                "HomePage layout was updated and bootstrap now starts directly from Home.",
                "OK");
        }

        private static bool CanCreateOrOverwriteScenes()
        {
            var bootstrapExists = File.Exists(BootstrapScenePath);
            var stageExists = File.Exists(StageScenePath);

            if (!bootstrapExists && !stageExists)
            {
                return true;
            }

            return EditorUtility.DisplayDialog(
                "Overwrite Existing Scenes?",
                "Bootstrap.unity or Stage01.unity already exists. Overwrite them with generated scenes?",
                "Overwrite",
                "Cancel");
        }

        private static void CreateStageScene()
        {
            var stageScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Ground";
            floor.transform.localScale = new Vector3(3f, 1f, 3f);

            var playerSpawn = new GameObject("PlayerSpawn");
            playerSpawn.transform.position = new Vector3(0f, 0.5f, -8f);

            var stageMarker = new GameObject("Stage01Marker");
            stageMarker.transform.position = new Vector3(0f, 1f, 0f);

            EditorSceneManager.SaveScene(stageScene, StageScenePath);
        }

        private static void CreateBootstrapScene()
        {
            var bootstrapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateEventSystem();

            var canvas = CreateCanvas();
            var startupLogoCanvasGroup = CreateStartupLogo(canvas.transform, out var hasStartupLogoSprite);

            var homePage = CreatePageRoot<HomeBasePageController>(canvas.transform, "HomePage", new Color32(0xFE, 0xF5, 0xE8, 0xFF));
            var settingsPage = CreatePageRoot<SimplePageController>(canvas.transform, "SettingsPage", new Color32(0xFE, 0xF5, 0xE8, 0xFF));

            CreatePageTitle(settingsPage.transform, "SETTINGS");

            var uiRoot = new GameObject("UIRoot");
            var pageRouter = uiRoot.AddComponent<UIPageRouter>();
            ConfigurePageRouter(pageRouter, homePage.GetComponent<UIPage>(), settingsPage.GetComponent<UIPage>());

            var app = new GameObject("App");
            var startupFlow = app.AddComponent<StartupFlowController>();
            ConfigureStartupFlow(startupFlow, pageRouter, startupLogoCanvasGroup, hasStartupLogoSprite);

            EditorSceneManager.SaveScene(bootstrapScene, BootstrapScenePath);
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            var inputSystemUiModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (inputSystemUiModule != null)
            {
                eventSystemObject.AddComponent(inputSystemUiModule);
                return;
            }

            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static CanvasGroup CreateStartupLogo(Transform canvasTransform, out bool hasStartupLogoSprite)
        {
            var startupLogo = CreateFullStretchUiObject("StartupLogo", canvasTransform);
            var startupLogoImage = startupLogo.AddComponent<Image>();
            ConfigureStartupLogoBackground(startupLogoImage);

            var logoImage = GetOrCreateStartupLogoImage(startupLogo.transform);
            hasStartupLogoSprite = TryApplyStartupLogoSprite(logoImage);
            logoImage.gameObject.SetActive(hasStartupLogoSprite);

            var logoLabel = CreateFullStretchText("LogoLabel", startupLogo.transform, "STELLA GUILD");
            logoLabel.fontSize = 96;
            logoLabel.alignment = TextAnchor.MiddleCenter;
            logoLabel.color = StellaColorTokens.Get(ColorToken.TextShadow);
            logoLabel.gameObject.SetActive(!hasStartupLogoSprite);

            return startupLogo.AddComponent<CanvasGroup>();
        }

        private static GameObject CreatePageRoot<T>(Transform parent, string name, Color32 backgroundColor) where T : UIPage
        {
            var page = CreateFullStretchUiObject(name, parent);
            var background = page.AddComponent<Image>();
            background.color = backgroundColor;
            page.AddComponent<CanvasGroup>();
            page.AddComponent<T>();
            return page;
        }

        private static void CreatePageTitle(Transform pageRoot, string title)
        {
            var titleObject = new GameObject("Title", typeof(RectTransform));
            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.SetParent(pageRoot, false);
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -160f);
            titleRect.sizeDelta = new Vector2(800f, 120f);

            var text = titleObject.AddComponent<Text>();
            text.text = title;
            text.font = GetDefaultFont();
            text.fontSize = 80;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color32(0x28, 0x19, 0x0A, 0xFF);
        }

        private static void ConfigurePageRouter(UIPageRouter pageRouter, UIPage homePage, UIPage settingsPage)
        {
            var routerSerialized = new SerializedObject(pageRouter);
            routerSerialized.FindProperty("initialPage").enumValueIndex = (int)UIPageType.Home;

            var entries = routerSerialized.FindProperty("pageEntries");
            entries.arraySize = 2;
            SetRouterEntry(entries.GetArrayElementAtIndex(0), UIPageType.Home, homePage);
            SetRouterEntry(entries.GetArrayElementAtIndex(1), UIPageType.Settings, settingsPage);

            routerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRouterEntry(SerializedProperty entry, UIPageType type, UIPage page)
        {
            entry.FindPropertyRelative("pageType").enumValueIndex = (int)type;
            entry.FindPropertyRelative("page").objectReferenceValue = page;
        }

        private static void ConfigureStartupFlow(StartupFlowController startupFlow, UIPageRouter pageRouter, CanvasGroup startupLogoCanvasGroup, bool hasStartupLogoSprite)
        {
            var startupFlowSerialized = new SerializedObject(startupFlow);
            startupFlowSerialized.FindProperty("pageRouter").objectReferenceValue = pageRouter;
            startupFlowSerialized.FindProperty("startDestinationPage").enumValueIndex = (int)UIPageType.Home;
            startupFlowSerialized.FindProperty("gameplaySceneName").stringValue = "Stage01";
            startupFlowSerialized.FindProperty("startupLogoCanvasGroup").objectReferenceValue = startupLogoCanvasGroup;
            startupFlowSerialized.FindProperty("startupLogoDurationSeconds").floatValue = 1.6f;
            startupFlowSerialized.FindProperty("startupLogoFadeSeconds").floatValue = 0.35f;
            startupFlowSerialized.FindProperty("skipStartupLogo").boolValue = !hasStartupLogoSprite;
            startupFlowSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static HomeBasePageController EnsureHomeBaseController(GameObject homePageObject)
        {
            var homeBaseController = homePageObject.GetComponent<HomeBasePageController>();
            if (homeBaseController != null)
            {
                return homeBaseController;
            }

            var existingPageControllers = homePageObject.GetComponents<UIPage>();
            foreach (var existingController in existingPageControllers)
            {
                UnityEngine.Object.DestroyImmediate(existingController);
            }

            homeBaseController = homePageObject.AddComponent<HomeBasePageController>();
            return homeBaseController;
        }

        private static void ConfigureHomePageEntry(UIPageRouter pageRouter, UIPage homePage, UIPage settingsPage)
        {
            var routerSerialized = new SerializedObject(pageRouter);
            routerSerialized.FindProperty("initialPage").enumValueIndex = (int)UIPageType.Home;
            var entries = routerSerialized.FindProperty("pageEntries");
            entries.arraySize = settingsPage != null ? 2 : 1;
            SetRouterEntry(entries.GetArrayElementAtIndex(0), UIPageType.Home, homePage);
            if (settingsPage != null)
            {
                SetRouterEntry(entries.GetArrayElementAtIndex(1), UIPageType.Settings, settingsPage);
            }

            routerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text CreateFullStretchText(string name, Transform parent, string value)
        {
            var textObject = CreateFullStretchUiObject(name, parent);
            var text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = GetDefaultFont();
            return text;
        }

        private static GameObject CreateFullStretchUiObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return gameObject;
        }

        private static Font GetDefaultFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static bool TryApplyStartupLogoSprite(Image targetImage)
        {
            var logoSprite = LoadStartupLogoSprite();
            if (logoSprite == null || targetImage == null)
            {
                return false;
            }

            ApplyStartupLogoToImage(targetImage, logoSprite);
            return true;
        }

        private static void ApplyStartupLogoToImage(Image targetImage, Sprite logoSprite)
        {
            ConfigureLogoImageRect(targetImage.rectTransform);
            targetImage.sprite = logoSprite;
            targetImage.color = Color.white;
            targetImage.type = Image.Type.Simple;
            targetImage.preserveAspect = false;
            targetImage.raycastTarget = false;

            var aspectRatioFitter = targetImage.GetComponent<AspectRatioFitter>();
            if (aspectRatioFitter == null)
            {
                aspectRatioFitter = targetImage.gameObject.AddComponent<AspectRatioFitter>();
            }

            aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectRatioFitter.aspectRatio = logoSprite.rect.width / logoSprite.rect.height;
        }

        private static Sprite LoadStartupLogoSprite()
        {
            if (File.Exists(PreferredLogoPath))
            {
                EnsureTextureImportedAsSprite(PreferredLogoPath);
                var preferred = AssetDatabase.LoadAssetAtPath<Sprite>(PreferredLogoPath);
                if (preferred != null)
                {
                    return preferred;
                }
            }

            var spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { LogoFolderPath });
            foreach (var spriteGuid in spriteGuids)
            {
                var spritePath = AssetDatabase.GUIDToAssetPath(spriteGuid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { LogoFolderPath });
            foreach (var textureGuid in textureGuids)
            {
                var texturePath = AssetDatabase.GUIDToAssetPath(textureGuid);
                if (!EnsureTextureImportedAsSprite(texturePath))
                {
                    continue;
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static bool EnsureTextureImportedAsSprite(string assetPath)
        {
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter == null)
            {
                return false;
            }

            var needsReimport = textureImporter.textureType != TextureImporterType.Sprite
                || textureImporter.spriteImportMode != SpriteImportMode.Single
                || textureImporter.mipmapEnabled
                || !textureImporter.alphaIsTransparency;

            if (!needsReimport)
            {
                return true;
            }

            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.mipmapEnabled = false;
            textureImporter.alphaIsTransparency = true;
            textureImporter.SaveAndReimport();
            return true;
        }

        private static void ConfigureStartupLogoBackground(Image backgroundImage)
        {
            if (backgroundImage == null)
            {
                return;
            }

            backgroundImage.sprite = null;
            backgroundImage.color = StellaColorTokens.Get(ColorToken.BaseBackground);
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
            backgroundImage.raycastTarget = false;
        }

        private static Image GetOrCreateStartupLogoImage(Transform startupLogoTransform)
        {
            var existing = startupLogoTransform.Find("LogoImage");
            if (existing != null)
            {
                var existingImage = existing.GetComponent<Image>();
                if (existingImage == null)
                {
                    existingImage = existing.gameObject.AddComponent<Image>();
                }

                ConfigureLogoImageRect(existing.GetComponent<RectTransform>());
                return existingImage;
            }

            var logoImageObject = new GameObject("LogoImage", typeof(RectTransform), typeof(Image));
            var logoImageRect = logoImageObject.GetComponent<RectTransform>();
            logoImageRect.SetParent(startupLogoTransform, false);
            ConfigureLogoImageRect(logoImageRect);

            return logoImageObject.GetComponent<Image>();
        }

        private static void ConfigureLogoImageRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(StartupLogoHorizontalPadding, 0f);
            rectTransform.offsetMax = new Vector2(-StartupLogoHorizontalPadding, 0f);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private static void ApplyBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new(BootstrapScenePath, true),
                new(StageScenePath, true)
            };

            foreach (var existingScene in EditorBuildSettings.scenes)
            {
                if (existingScene.path == BootstrapScenePath || existingScene.path == StageScenePath)
                {
                    continue;
                }

                scenes.Add(existingScene);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string folderPath)
        {
            var normalized = folderPath.Replace("\\", "/");
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            var segments = normalized.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
#endif
