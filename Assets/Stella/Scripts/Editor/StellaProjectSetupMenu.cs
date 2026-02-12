#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using StellaGuild.Flow;
using StellaGuild.UI;
using UnityEditor;
using UnityEditor.Events;
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
            var startupLogoCanvasGroup = CreateStartupLogo(canvas.transform);

            var titlePage = CreatePageRoot<TitlePageController>(canvas.transform, "TitlePage", new Color32(0xFE, 0xF5, 0xE8, 0xFF));
            var homePage = CreatePageRoot<SimplePageController>(canvas.transform, "HomePage", new Color32(0xFE, 0xF5, 0xE8, 0xFF));
            var settingsPage = CreatePageRoot<SimplePageController>(canvas.transform, "SettingsPage", new Color32(0xFE, 0xF5, 0xE8, 0xFF));

            CreatePageTitle(titlePage.transform, "STELLA GUILD");
            CreatePageTitle(homePage.transform, "HOME");
            CreatePageTitle(settingsPage.transform, "SETTINGS");

            var startButton = CreateButton(titlePage.transform, "StartButton", "Start", new Vector2(0f, -280f), new Vector2(420f, 120f));
            var settingsButton = CreateButton(titlePage.transform, "SettingsButton", "Settings", new Vector2(0f, -430f), new Vector2(300f, 96f));

            var titleController = titlePage.GetComponent<TitlePageController>();
            AssignTitlePageButtons(titleController, startButton, settingsButton);

            var uiRoot = new GameObject("UIRoot");
            var pageRouter = uiRoot.AddComponent<UIPageRouter>();
            ConfigurePageRouter(pageRouter, titlePage.GetComponent<UIPage>(), homePage.GetComponent<UIPage>(), settingsPage.GetComponent<UIPage>());

            var app = new GameObject("App");
            var startupFlow = app.AddComponent<StartupFlowController>();
            ConfigureStartupFlow(startupFlow, pageRouter, startupLogoCanvasGroup);

            BindButtonEvents(startButton, startupFlow.OnStartPressed);
            BindButtonEvents(settingsButton, startupFlow.OnSettingsPressed);

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

        private static CanvasGroup CreateStartupLogo(Transform canvasTransform)
        {
            var startupLogo = CreateFullStretchUiObject("StartupLogo", canvasTransform);
            var startupLogoImage = startupLogo.AddComponent<Image>();
            startupLogoImage.color = new Color32(0x28, 0x19, 0x0A, 0xFF);

            var logoLabel = CreateFullStretchText("LogoLabel", startupLogo.transform, "STELLA GUILD");
            logoLabel.fontSize = 96;
            logoLabel.alignment = TextAnchor.MiddleCenter;
            logoLabel.color = new Color32(0xFE, 0xF5, 0xE8, 0xFF);

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

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color32(0x9F, 0x16, 0x00, 0xFF);

            var labelObject = CreateFullStretchUiObject("Label", buttonObject.transform);
            var labelText = labelObject.AddComponent<Text>();
            labelText.text = label;
            labelText.font = GetDefaultFont();
            labelText.fontSize = 54;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color32(0xFE, 0xF5, 0xE8, 0xFF);

            return buttonObject.GetComponent<Button>();
        }

        private static void AssignTitlePageButtons(TitlePageController titleController, Button startButton, Button settingsButton)
        {
            var titleControllerSerialized = new SerializedObject(titleController);
            titleControllerSerialized.FindProperty("startButton").objectReferenceValue = startButton;
            titleControllerSerialized.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            titleControllerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePageRouter(UIPageRouter pageRouter, UIPage titlePage, UIPage homePage, UIPage settingsPage)
        {
            var routerSerialized = new SerializedObject(pageRouter);
            routerSerialized.FindProperty("initialPage").enumValueIndex = (int)UIPageType.Title;

            var entries = routerSerialized.FindProperty("pageEntries");
            entries.arraySize = 3;
            SetRouterEntry(entries.GetArrayElementAtIndex(0), UIPageType.Title, titlePage);
            SetRouterEntry(entries.GetArrayElementAtIndex(1), UIPageType.Home, homePage);
            SetRouterEntry(entries.GetArrayElementAtIndex(2), UIPageType.Settings, settingsPage);

            routerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRouterEntry(SerializedProperty entry, UIPageType type, UIPage page)
        {
            entry.FindPropertyRelative("pageType").enumValueIndex = (int)type;
            entry.FindPropertyRelative("page").objectReferenceValue = page;
        }

        private static void ConfigureStartupFlow(StartupFlowController startupFlow, UIPageRouter pageRouter, CanvasGroup startupLogoCanvasGroup)
        {
            var startupFlowSerialized = new SerializedObject(startupFlow);
            startupFlowSerialized.FindProperty("pageRouter").objectReferenceValue = pageRouter;
            startupFlowSerialized.FindProperty("startDestinationPage").enumValueIndex = (int)UIPageType.BattleHud;
            startupFlowSerialized.FindProperty("gameplaySceneName").stringValue = "Stage01";
            startupFlowSerialized.FindProperty("startupLogoCanvasGroup").objectReferenceValue = startupLogoCanvasGroup;
            startupFlowSerialized.FindProperty("startupLogoDurationSeconds").floatValue = 1.6f;
            startupFlowSerialized.FindProperty("startupLogoFadeSeconds").floatValue = 0.35f;
            startupFlowSerialized.FindProperty("skipStartupLogo").boolValue = true;
            startupFlowSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindButtonEvents(Button button, UnityEngine.Events.UnityAction call)
        {
            ClearPersistentListeners(button.onClick);
            UnityEventTools.AddPersistentListener(button.onClick, call);
        }

        private static void ClearPersistentListeners(UnityEngine.Events.UnityEvent unityEvent)
        {
            for (var i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, i);
            }
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
