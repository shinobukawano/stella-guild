using StellaGuild.Design;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StellaGuild.UI.Home
{
    [DisallowMultipleComponent]
    public sealed class HomeBaseBackground3D : MonoBehaviour
    {
        private const int CurrentSerializedDataVersion = 3;
        private const int MinRenderTextureWidth = 1440;
        private const int MinRenderTextureHeight = 2560;
        private const int MaxRenderTextureWidth = 3072;
        private const int MaxRenderTextureHeight = 4096;

        [SerializeField] private RawImage targetRawImage;
        [SerializeField] private Vector2Int textureSize = new(1440, 2560);
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool renderEveryFrame = true;

        [Header("Camera")]
        [SerializeField] private float cameraHeight = 32f;
        [SerializeField] private float cameraDistance = 24f;
        [SerializeField] private float cameraFieldOfView = 36f;
        [SerializeField] private Vector3 cameraLookAt = new(0f, 1.2f, 7f);

        [Header("Animation")]
        [SerializeField] private bool animateBackground = true;
        [SerializeField] private float cameraOrbitSpeedDegrees = 6f;
        [SerializeField] private float cameraOrbitDistance = 1.8f;
        [SerializeField] private float cameraBobAmplitude = 0.8f;
        [SerializeField] private float cameraBobSpeed = 0.85f;
        [SerializeField] private float lookAtBobAmplitude = 0.45f;
        [SerializeField] private float lookAtBobSpeed = 1.1f;
        [SerializeField] private float hqSpinSpeed = 20f;
        [SerializeField] private float beaconBobAmplitude = 0.2f;
        [SerializeField] private float beaconBobSpeed = 2.1f;

        [Header("Interaction")]
        [SerializeField] private bool enableTouchInteraction = true;
        [SerializeField] private bool enableMouseInEditor = true;
        [SerializeField] private float dragPanSpeed = 0.08f;
        [SerializeField] private float pinchPanSpeed = 0.05f;
        [SerializeField] private float pinchZoomSpeed = 0.08f;
        [SerializeField] private float minCameraDistance = 18f;
        [SerializeField] private float maxCameraDistance = 52f;
        [SerializeField] private float maxPanX = 28f;
        [SerializeField] private float minPanZ = -14f;
        [SerializeField] private float maxPanZ = 34f;
        [SerializeField] private int serializedDataVersion = CurrentSerializedDataVersion;

        private RenderTexture _renderTexture;
        private GameObject _worldRoot;
        private Camera _worldCamera;
        private Light _worldLight;
        private Light _fillLight;
        private Material _groundMaterial;
        private Material _buildingMaterial;
        private Material _accentMaterial;
        private Material _roadMaterial;
        private Material _waterMaterial;
        private Transform _hqTopTransform;
        private Quaternion _hqTopBaseRotation;
        private Transform _beaconLeftTransform;
        private Transform _beaconRightTransform;
        private Vector3 _beaconLeftBasePosition;
        private Vector3 _beaconRightBasePosition;
        private Vector3 _cameraBaseLocalPosition;
        private Vector3 _panOffset;
        private float _zoomOffset;
        private bool _isDragging;
        private bool _isPinching;
        private Vector2 _lastDragPointerPosition;
        private Vector2 _lastPinchCenter;
        private float _lastPinchDistance;

        private void OnEnable()
        {
            MigrateSerializedDataIfNeeded();

            if (autoInitialize)
            {
                Initialize();
            }
        }

        private void OnValidate()
        {
            MigrateSerializedDataIfNeeded();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void LateUpdate()
        {
            var interacted = false;

            if (enableTouchInteraction)
            {
                interacted = HandleInteractionInput();
            }

            UpdateWorldTransforms(Time.unscaledTime);

            if (!renderEveryFrame && !animateBackground && !interacted)
            {
                return;
            }

            RenderNow();
        }

        [ContextMenu("Initialize Background")]
        public void Initialize()
        {
            if (targetRawImage == null)
            {
                targetRawImage = GetComponent<RawImage>();
            }

            if (targetRawImage == null)
            {
                Debug.LogWarning("HomeBaseBackground3D requires a RawImage target.", this);
                return;
            }

            Cleanup();
            BuildMaterials();
            BuildWorld();
            BuildCameraAndLight();
        }

        private void BuildMaterials()
        {
            _groundMaterial = CreateMaterial(new Color(0.95f, 0.88f, 0.74f, 1f), 0.02f, 0.08f);
            _buildingMaterial = CreateMaterial(new Color(0.36f, 0.26f, 0.17f, 1f), 0.04f, 0.22f);
            _accentMaterial = CreateMaterial(new Color(0.95f, 0.61f, 0.23f, 1f), 0.08f, 0.34f);
            _roadMaterial = CreateMaterial(new Color(0.88f, 0.82f, 0.71f, 1f), 0f, 0.05f);
            _waterMaterial = CreateMaterial(new Color(0.20f, 0.54f, 0.64f, 1f), 0f, 0.2f);
        }

        private void BuildWorld()
        {
            _worldRoot = new GameObject("HomeBaseBackdropWorld");
            _worldRoot.transform.SetParent(transform, false);
            _worldRoot.transform.localPosition = Vector3.zero;
            _worldRoot.transform.localRotation = Quaternion.identity;
            _worldRoot.transform.localScale = Vector3.one;

            CreatePrimitive(PrimitiveType.Plane, "Ground", new Vector3(0f, 0f, 6f), new Vector3(5f, 1f, 5f), _groundMaterial);

            CreateRoad(new Vector3(0f, 0.02f, 6f), new Vector3(2f, 0.04f, 35f));
            CreateRoad(new Vector3(-6f, 0.02f, 6f), new Vector3(2f, 0.04f, 32f));
            CreateRoad(new Vector3(6f, 0.02f, 6f), new Vector3(2f, 0.04f, 32f));
            CreateRoad(new Vector3(0f, 0.02f, 12f), new Vector3(22f, 0.04f, 2f));
            CreateRoad(new Vector3(0f, 0.02f, 0f), new Vector3(20f, 0.04f, 2f));

            CreatePrimitive(PrimitiveType.Cylinder, "BasePad", new Vector3(0f, 0.45f, 7f), new Vector3(4f, 0.9f, 4f), _accentMaterial);
            CreatePrimitive(PrimitiveType.Cylinder, "HQCore", new Vector3(0f, 2.1f, 7f), new Vector3(1.8f, 3.2f, 1.8f), _buildingMaterial);
            var hqTop = CreatePrimitive(PrimitiveType.Cube, "HQTop", new Vector3(0f, 4.9f, 7f), new Vector3(2.6f, 0.4f, 2.6f), _accentMaterial);
            _hqTopTransform = hqTop.transform;
            _hqTopBaseRotation = _hqTopTransform.localRotation;

            BuildBuildingDistrict();
            BuildWaterAndProps();
        }

        private void BuildBuildingDistrict()
        {
            var index = 0;
            for (var x = -3; x <= 3; x++)
            {
                for (var z = -1; z <= 5; z++)
                {
                    if (Mathf.Abs(x) <= 1 && Mathf.Abs(z - 2) <= 1)
                    {
                        continue;
                    }

                    var height = 0.8f + ((index * 37 + x * 13 + z * 17) % 5) * 0.55f;
                    var size = 1.2f + ((index * 11 + z) % 3) * 0.25f;
                    var position = new Vector3(x * 3.2f, height * 0.5f, z * 3f + 3f);
                    CreatePrimitive(PrimitiveType.Cube, $"Building_{index}", position, new Vector3(size, height, size), _buildingMaterial);

                    if (index % 5 == 0)
                    {
                        CreatePrimitive(
                            PrimitiveType.Cylinder,
                            $"Tower_{index}",
                            new Vector3(position.x + 0.6f, height + 0.5f, position.z - 0.6f),
                            new Vector3(0.38f, 1f, 0.38f),
                            _accentMaterial);
                    }

                    index++;
                }
            }
        }

        private void BuildWaterAndProps()
        {
            CreatePrimitive(PrimitiveType.Cube, "River", new Vector3(9f, 0.01f, 8f), new Vector3(4.2f, 0.02f, 30f), _waterMaterial);
            CreatePrimitive(PrimitiveType.Cube, "RiverEdge", new Vector3(6.8f, 0.02f, 8f), new Vector3(0.25f, 0.05f, 30f), _roadMaterial);
            CreatePrimitive(PrimitiveType.Cube, "RiverEdge2", new Vector3(11.2f, 0.02f, 8f), new Vector3(0.25f, 0.05f, 30f), _roadMaterial);

            var beaconLeft = CreatePrimitive(PrimitiveType.Sphere, "BeaconLeft", new Vector3(-4f, 1.2f, 10f), new Vector3(0.7f, 0.7f, 0.7f), _accentMaterial);
            var beaconRight = CreatePrimitive(PrimitiveType.Sphere, "BeaconRight", new Vector3(4f, 1.2f, 10f), new Vector3(0.7f, 0.7f, 0.7f), _accentMaterial);

            _beaconLeftTransform = beaconLeft.transform;
            _beaconRightTransform = beaconRight.transform;
            _beaconLeftBasePosition = _beaconLeftTransform.localPosition;
            _beaconRightBasePosition = _beaconRightTransform.localPosition;
        }

        private void BuildCameraAndLight()
        {
            var renderWidth = Mathf.Clamp(textureSize.x, MinRenderTextureWidth, MaxRenderTextureWidth);
            var renderHeight = Mathf.Clamp(textureSize.y, MinRenderTextureHeight, MaxRenderTextureHeight);

            _renderTexture = new RenderTexture(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = "HomeBaseBackdropRT",
                antiAliasing = 4,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0
            };
            _renderTexture.Create();

            var cameraObject = new GameObject("BackdropCamera");
            cameraObject.transform.SetParent(_worldRoot.transform, false);
            _cameraBaseLocalPosition = new Vector3(0f, cameraHeight, -cameraDistance);
            cameraObject.transform.localPosition = _cameraBaseLocalPosition;
            cameraObject.transform.LookAt(_worldRoot.transform.TransformPoint(cameraLookAt));

            _worldCamera = cameraObject.AddComponent<Camera>();
            _worldCamera.clearFlags = CameraClearFlags.SolidColor;
            _worldCamera.backgroundColor = new Color(0.76f, 0.84f, 0.9f, 1f);
            _worldCamera.fieldOfView = cameraFieldOfView;
            _worldCamera.nearClipPlane = 0.1f;
            _worldCamera.farClipPlane = 250f;
            _worldCamera.allowHDR = true;
            _worldCamera.allowMSAA = true;
            _worldCamera.targetTexture = _renderTexture;

            var lightObject = new GameObject("BackdropLight");
            lightObject.transform.SetParent(_worldRoot.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(55f, -38f, 0f);

            _worldLight = lightObject.AddComponent<Light>();
            _worldLight.type = LightType.Directional;
            _worldLight.intensity = 1.28f;
            _worldLight.color = new Color(1f, 0.93f, 0.82f, 1f);

            var fillLightObject = new GameObject("BackdropFillLight");
            fillLightObject.transform.SetParent(_worldRoot.transform, false);
            fillLightObject.transform.rotation = Quaternion.Euler(32f, 146f, 0f);

            _fillLight = fillLightObject.AddComponent<Light>();
            _fillLight.type = LightType.Directional;
            _fillLight.intensity = 0.48f;
            _fillLight.color = new Color(0.68f, 0.77f, 0.98f, 1f);

            targetRawImage.texture = _renderTexture;
            targetRawImage.color = Color.white;
            targetRawImage.raycastTarget = false;
            UpdateWorldTransforms(Time.unscaledTime);
            RenderNow();
        }

        private void UpdateWorldTransforms(float time)
        {
            if (_worldRoot == null)
            {
                return;
            }

            if (_worldCamera != null)
            {
                var distance = Mathf.Clamp(cameraDistance + _zoomOffset, minCameraDistance, maxCameraDistance);
                _cameraBaseLocalPosition = new Vector3(0f, cameraHeight, -distance);

                var cameraOffset = Vector3.zero;
                var lookAtOffset = Vector3.zero;
                if (animateBackground)
                {
                    var orbitRadians = time * cameraOrbitSpeedDegrees * Mathf.Deg2Rad;
                    cameraOffset = new Vector3(
                        Mathf.Sin(orbitRadians) * cameraOrbitDistance,
                        Mathf.Sin(time * cameraBobSpeed) * cameraBobAmplitude,
                        Mathf.Cos(orbitRadians) * cameraOrbitDistance * 0.5f);

                    lookAtOffset = new Vector3(0f, Mathf.Sin(time * lookAtBobSpeed) * lookAtBobAmplitude, 0f);
                }

                var pan = new Vector3(
                    Mathf.Clamp(_panOffset.x, -maxPanX, maxPanX),
                    0f,
                    Mathf.Clamp(_panOffset.z, minPanZ, maxPanZ));

                _worldCamera.transform.localPosition = _cameraBaseLocalPosition + pan + cameraOffset;
                _worldCamera.transform.LookAt(_worldRoot.transform.TransformPoint(cameraLookAt + pan + lookAtOffset));
            }

            if (_hqTopTransform != null)
            {
                _hqTopTransform.localRotation = animateBackground
                    ? _hqTopBaseRotation * Quaternion.Euler(0f, time * hqSpinSpeed, 0f)
                    : _hqTopBaseRotation;
            }

            if (_beaconLeftTransform != null)
            {
                _beaconLeftTransform.localPosition = animateBackground
                    ? _beaconLeftBasePosition + new Vector3(0f, Mathf.Sin(time * beaconBobSpeed) * beaconBobAmplitude, 0f)
                    : _beaconLeftBasePosition;
            }

            if (_beaconRightTransform != null)
            {
                _beaconRightTransform.localPosition = animateBackground
                    ? _beaconRightBasePosition + new Vector3(0f, Mathf.Sin(time * beaconBobSpeed + Mathf.PI * 0.5f) * beaconBobAmplitude, 0f)
                    : _beaconRightBasePosition;
            }
        }

        private void MigrateSerializedDataIfNeeded()
        {
            if (serializedDataVersion >= CurrentSerializedDataVersion)
            {
                return;
            }

            if (serializedDataVersion < 2)
            {
                // Existing scene instances created before interaction/animation fields were added.
                renderEveryFrame = true;
                animateBackground = true;
                cameraOrbitSpeedDegrees = 6f;
                cameraOrbitDistance = 1.8f;
                cameraBobAmplitude = 0.8f;
                cameraBobSpeed = 0.85f;
                lookAtBobAmplitude = 0.45f;
                lookAtBobSpeed = 1.1f;
                hqSpinSpeed = 20f;
                beaconBobAmplitude = 0.2f;
                beaconBobSpeed = 2.1f;

                enableTouchInteraction = true;
                enableMouseInEditor = true;
                dragPanSpeed = 0.08f;
                pinchPanSpeed = 0.05f;
                pinchZoomSpeed = 0.08f;
                minCameraDistance = 18f;
                maxCameraDistance = 52f;
                maxPanX = 28f;
                minPanZ = -14f;
                maxPanZ = 34f;
            }

            if (serializedDataVersion < 3)
            {
                // Increase detail and contrast for the demo base background.
                textureSize = new Vector2Int(1440, 2560);
                cameraHeight = 32f;
                cameraDistance = 24f;
                cameraFieldOfView = 36f;
                cameraLookAt = new Vector3(0f, 1.2f, 7f);
                minCameraDistance = 16f;
                maxCameraDistance = 42f;
            }

            serializedDataVersion = CurrentSerializedDataVersion;
        }

        private bool HandleInteractionInput()
        {
            if (_worldCamera == null || targetRawImage == null)
            {
                return false;
            }

#if ENABLE_INPUT_SYSTEM
            var touchChanged = HandleTouchInputSystem();
            if (HasActiveInputSystemTouches() || _isPinching)
            {
                return touchChanged;
            }

            _isPinching = false;
            return HandleMouseInputSystem();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                return HandleTouchInputLegacy();
            }

            _isPinching = false;
            return HandleMouseInputLegacy();
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private bool HandleTouchInputSystem()
        {
            var activeTouchCount = GetActiveInputSystemTouches(
                out var firstPosition,
                out var firstPhase,
                out var secondPosition,
                out var secondPhase);

            if (activeTouchCount >= 2)
            {
                var center = (firstPosition + secondPosition) * 0.5f;

                if (!IsScreenPointOnMap(center))
                {
                    _isPinching = false;
                    _isDragging = false;
                    return false;
                }

                var distance = Vector2.Distance(firstPosition, secondPosition);
                if (!_isPinching ||
                    firstPhase == UnityEngine.InputSystem.TouchPhase.Began ||
                    secondPhase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    _isPinching = true;
                    _isDragging = false;
                    _lastPinchCenter = center;
                    _lastPinchDistance = distance;
                    return false;
                }

                var centerDelta = center - _lastPinchCenter;
                var distanceDelta = distance - _lastPinchDistance;

                _lastPinchCenter = center;
                _lastPinchDistance = distance;

                var changed = ApplyPanFromScreenDelta(centerDelta, pinchPanSpeed);
                changed |= ApplyPinchZoom(distanceDelta);
                return changed;
            }

            _isPinching = false;

            if (activeTouchCount == 1)
            {
                if (firstPhase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    _isDragging = IsScreenPointOnMap(firstPosition);
                    _lastDragPointerPosition = firstPosition;
                    return false;
                }

                if (!_isDragging)
                {
                    return false;
                }

                if (firstPhase == UnityEngine.InputSystem.TouchPhase.Moved ||
                    firstPhase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    var delta = firstPosition - _lastDragPointerPosition;
                    _lastDragPointerPosition = firstPosition;
                    return ApplyPanFromScreenDelta(delta, dragPanSpeed);
                }

                if (firstPhase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    firstPhase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    _isDragging = false;
                }

                return false;
            }

            _isDragging = false;
            return false;
        }

        private bool HandleMouseInputSystem()
        {
            if (!enableMouseInEditor)
            {
                return false;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            var changed = false;
            var mousePosition = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                _isDragging = IsScreenPointOnMap(mousePosition);
                _lastDragPointerPosition = mousePosition;
            }

            if (_isDragging && mouse.leftButton.isPressed)
            {
                var delta = mousePosition - _lastDragPointerPosition;
                _lastDragPointerPosition = mousePosition;
                changed |= ApplyPanFromScreenDelta(delta, dragPanSpeed);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                _isDragging = false;
            }

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.001f && IsScreenPointOnMap(mousePosition))
            {
                var normalizedScroll = scroll / 120f;
                changed |= ApplyPinchZoom(normalizedScroll * 24f);
            }

            return changed;
        }

        private int GetActiveInputSystemTouches(
            out Vector2 firstPosition,
            out UnityEngine.InputSystem.TouchPhase firstPhase,
            out Vector2 secondPosition,
            out UnityEngine.InputSystem.TouchPhase secondPhase)
        {
            firstPosition = Vector2.zero;
            secondPosition = Vector2.zero;
            firstPhase = UnityEngine.InputSystem.TouchPhase.None;
            secondPhase = UnityEngine.InputSystem.TouchPhase.None;

            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                {
                    continue;
                }

                if (count == 0)
                {
                    firstPosition = touch.position.ReadValue();
                    firstPhase = touch.phase.ReadValue();
                }
                else if (count == 1)
                {
                    secondPosition = touch.position.ReadValue();
                    secondPhase = touch.phase.ReadValue();
                }

                count++;
            }

            return count;
        }

        private static bool HasActiveInputSystemTouches()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            foreach (var touch in touchscreen.touches)
            {
                if (touch.press.isPressed)
                {
                    return true;
                }
            }

            return false;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        private bool HandleTouchInputLegacy()
        {
            if (Input.touchCount >= 2)
            {
                var first = Input.GetTouch(0);
                var second = Input.GetTouch(1);
                var center = (first.position + second.position) * 0.5f;

                if (!IsScreenPointOnMap(center))
                {
                    _isPinching = false;
                    _isDragging = false;
                    return false;
                }

                var distance = Vector2.Distance(first.position, second.position);
                if (!_isPinching || first.phase == TouchPhase.Began || second.phase == TouchPhase.Began)
                {
                    _isPinching = true;
                    _isDragging = false;
                    _lastPinchCenter = center;
                    _lastPinchDistance = distance;
                    return false;
                }

                var centerDelta = center - _lastPinchCenter;
                var distanceDelta = distance - _lastPinchDistance;

                _lastPinchCenter = center;
                _lastPinchDistance = distance;

                var changed = ApplyPanFromScreenDelta(centerDelta, pinchPanSpeed);
                changed |= ApplyPinchZoom(distanceDelta);
                return changed;
            }

            _isPinching = false;
            var touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                _isDragging = IsScreenPointOnMap(touch.position);
                _lastDragPointerPosition = touch.position;
                return false;
            }

            if (!_isDragging)
            {
                return false;
            }

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                var delta = touch.position - _lastDragPointerPosition;
                _lastDragPointerPosition = touch.position;
                return ApplyPanFromScreenDelta(delta, dragPanSpeed);
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                _isDragging = false;
            }

            return false;
        }

        private bool HandleMouseInputLegacy()
        {
            if (!enableMouseInEditor)
            {
                return false;
            }

            var changed = false;
            var mousePosition = (Vector2)Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = IsScreenPointOnMap(mousePosition);
                _lastDragPointerPosition = mousePosition;
            }

            if (_isDragging && Input.GetMouseButton(0))
            {
                var delta = mousePosition - _lastDragPointerPosition;
                _lastDragPointerPosition = mousePosition;
                changed |= ApplyPanFromScreenDelta(delta, dragPanSpeed);
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f && IsScreenPointOnMap(mousePosition))
            {
                changed |= ApplyPinchZoom(scroll * 24f);
            }

            return changed;
        }
#endif

        private bool IsScreenPointOnMap(Vector2 screenPosition)
        {
            if (targetRawImage == null)
            {
                return false;
            }

            var rectTransform = targetRawImage.rectTransform;
            if (rectTransform == null)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, null);
        }

        private bool ApplyPanFromScreenDelta(Vector2 screenDelta, float speed)
        {
            if (_worldCamera == null || screenDelta.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            var cameraTransform = _worldCamera.transform;
            var right = cameraTransform.right;
            var forward = cameraTransform.forward;
            right.y = 0f;
            forward.y = 0f;

            if (right.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            right.Normalize();
            forward.Normalize();

            var distanceScale = Mathf.Clamp((cameraDistance + _zoomOffset) / 30f, 0.7f, 1.7f);
            var worldDelta = (-right * screenDelta.x + -forward * screenDelta.y) * speed * distanceScale;
            _panOffset += new Vector3(worldDelta.x, 0f, worldDelta.z);
            ClampInteractionOffsets();
            return true;
        }

        private bool ApplyPinchZoom(float distanceDelta)
        {
            if (Mathf.Abs(distanceDelta) < 0.0001f)
            {
                return false;
            }

            _zoomOffset -= distanceDelta * pinchZoomSpeed;
            ClampInteractionOffsets();
            return true;
        }

        private void ClampInteractionOffsets()
        {
            _panOffset.x = Mathf.Clamp(_panOffset.x, -maxPanX, maxPanX);
            _panOffset.z = Mathf.Clamp(_panOffset.z, minPanZ, maxPanZ);

            var distance = Mathf.Clamp(cameraDistance + _zoomOffset, minCameraDistance, maxCameraDistance);
            _zoomOffset = distance - cameraDistance;
        }

        private void CreateRoad(Vector3 position, Vector3 scale)
        {
            CreatePrimitive(PrimitiveType.Cube, "Road", position, scale, _roadMaterial);
        }

        private GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(_worldRoot.transform, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;

            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            return primitive;
        }

        private Material CreateMaterial(Color color, float metallic, float smoothness)
        {
            Shader shader = null;
            var hasRenderPipeline = GraphicsSettings.currentRenderPipeline != null;

            if (hasRenderPipeline)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                }

                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Unlit");
                }
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                Debug.LogError("No compatible shader found for HomeBaseBackground3D.", this);
                return null;
            }

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            return material;
        }

        private void Cleanup()
        {
            if (targetRawImage != null && targetRawImage.texture == _renderTexture)
            {
                targetRawImage.texture = null;
            }

            DestroyObject(_worldCamera);
            DestroyObject(_worldLight);
            DestroyObject(_fillLight);
            DestroyObject(_worldRoot);

            DestroyObject(_groundMaterial);
            DestroyObject(_buildingMaterial);
            DestroyObject(_accentMaterial);
            DestroyObject(_roadMaterial);
            DestroyObject(_waterMaterial);

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                DestroyObject(_renderTexture);
                _renderTexture = null;
            }

            _hqTopTransform = null;
            _hqTopBaseRotation = Quaternion.identity;
            _beaconLeftTransform = null;
            _beaconRightTransform = null;
            _beaconLeftBasePosition = Vector3.zero;
            _beaconRightBasePosition = Vector3.zero;
            _cameraBaseLocalPosition = Vector3.zero;
            _fillLight = null;
            _panOffset = Vector3.zero;
            _zoomOffset = 0f;
            _isDragging = false;
            _isPinching = false;
            _lastDragPointerPosition = Vector2.zero;
            _lastPinchCenter = Vector2.zero;
            _lastPinchDistance = 0f;
        }

        private void RenderNow()
        {
            if (_worldCamera == null || _renderTexture == null)
            {
                return;
            }

            _worldCamera.Render();
        }

        private static void DestroyObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
