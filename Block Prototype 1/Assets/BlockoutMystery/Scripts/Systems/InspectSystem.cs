using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

namespace BlockoutMystery
{
    /// <summary>
    /// Holds an object up in front of the camera. Hold the mouse and drag to rotate it, click to use things on it
    /// (the hidden key), Escape puts it back exactly where it was. The object is drawn by an overlay camera on top of
    /// the world, with a dark backdrop behind it, so nearby walls (first person) can never cut into it.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class InspectSystem : MonoBehaviour
    {
        /// <summary>Built-in "Water" layer, unused by this prototype: only the overlay camera draws it on top.</summary>
        const int InspectLayer = 4;
        public static InspectSystem Instance { get; private set; }

        [SerializeField] Camera viewCamera;
        [Tooltip("Distance of the dim backdrop from the camera. Must be further than the inspected object.")]
        [SerializeField] float backdropDistance = 1.3f;
        [Tooltip("Lights the inspected object; only on while inspecting.")]
        [SerializeField] Light inspectLight;
        [Tooltip("Degrees per pixel of mouse drag.")]
        [SerializeField] float rotateSpeed = 0.4f;
        [Tooltip("A press that moves less than this many pixels counts as a click.")]
        [SerializeField] float clickTolerance = 8f;

        readonly List<(GameObject go, int layer)> swapped = new List<(GameObject, int)>();
        Camera overlay;
        GameObject backdrop;
        Transform pivot;
        bool restoreOrthographic;
        float baseLightIntensity;
        Transform originalParent;
        Vector3 originalPosition;
        Quaternion originalRotation;
        bool pressing;
        float pressTravel;

        public Inspectable Current { get; private set; }
        public Camera ViewCamera => viewCamera;

        public void Configure(Camera cam, Light light)
        {
            viewCamera = cam;
            inspectLight = light;
        }

        void Awake()
        {
            Instance = this;
            if (!viewCamera) viewCamera = Camera.main;
            if (inspectLight)
            {
                inspectLight.enabled = false;
                baseLightIntensity = inspectLight.intensity;
            }
            CreateOverlayCamera();
        }

        /// <summary>
        /// A camera stacked on the main one that clears depth and draws only the inspected object and the backdrop.
        /// </summary>
        void CreateOverlayCamera()
        {
            overlay = new GameObject("Inspect Camera").AddComponent<Camera>();
            overlay.transform.SetParent(viewCamera.transform, false);
            overlay.cullingMask = 1 << InspectLayer;
            overlay.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            viewCamera.GetUniversalAdditionalCameraData().cameraStack.Add(overlay);
            viewCamera.cullingMask &= ~(1 << InspectLayer);
            overlay.enabled = false;

            // The dim backdrop: a world-space panel on the same layer, further away than the object.
            backdrop = new GameObject("Inspect Backdrop", typeof(RectTransform)) { layer = InspectLayer };
            backdrop.transform.SetParent(overlay.transform, false);
            backdrop.transform.localPosition = new Vector3(0f, 0f, backdropDistance);
            backdrop.transform.localScale = Vector3.one * 0.01f;
            backdrop.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var dim = UIKit.Image("Dim", backdrop.transform, new Color(0.04f, 0.05f, 0.07f, 0.97f)); // UI blends in linear space: 0.9 still shows a close, bright wall
            dim.gameObject.layer = InspectLayer;
            dim.rectTransform.Stretch();
            backdrop.SetActive(false);
        }

        /// <summary>Sizes the backdrop to fill the view at its distance (with a margin).</summary>
        void FitBackdrop()
        {
            float height = 2f * backdropDistance * Mathf.Tan(overlay.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.2f;
            ((RectTransform)backdrop.transform).sizeDelta = new Vector2(height * overlay.aspect, height) * 100f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Begin(Inspectable target)
        {
            if (Current) End();
            Current = target;

            var t = target.transform;
            originalParent = t.parent;
            originalPosition = t.localPosition;
            originalRotation = t.localRotation;
            Vector3 localCenter = t.InverseTransformPoint(WorldBounds(t).center);

            pivot = new GameObject("Inspect Pivot").transform;
            pivot.SetParent(viewCamera.transform, false);
            pivot.localPosition = new Vector3(0f, 0f, target.InspectDistance);

            // Rotate around the object's visual centre, not its base.
            t.SetParent(pivot, false);
            t.localRotation = Quaternion.Euler(target.InspectEuler);
            t.localPosition = -(t.localRotation * Vector3.Scale(localCenter, t.localScale));

            // The isometric camera is orthographic; hold things up with a normal perspective instead. It also hangs high
            // above the level, out of reach of the room lights, so the inspect light works harder there.
            restoreOrthographic = viewCamera.orthographic;
            if (restoreOrthographic)
            {
                viewCamera.orthographic = false;
                viewCamera.fieldOfView = 60f;
            }

            overlay.orthographic = false;
            overlay.fieldOfView = viewCamera.fieldOfView;
            overlay.nearClipPlane = viewCamera.nearClipPlane;
            overlay.enabled = true;
            FitBackdrop();
            backdrop.SetActive(true);
            foreach (var child in t.GetComponentsInChildren<Transform>(true))
            {
                swapped.Add((child.gameObject, child.gameObject.layer));
                child.gameObject.layer = InspectLayer;
            }

            pressing = false;
            if (inspectLight)
            {
                inspectLight.intensity = baseLightIntensity * (restoreOrthographic ? 2.5f : 1f);
                inspectLight.enabled = true;
            }
            GameFlow.Instance.SetMode(GameMode.Inspect);
            target.OnInspectBegin();
            RefreshHint();
        }

        /// <summary>Puts the object back where it came from.</summary>
        public void End()
        {
            if (!Current) return;
            var t = Current.transform;
            t.SetParent(originalParent, false);
            t.localPosition = originalPosition;
            t.localRotation = originalRotation;
            if (pivot) Destroy(pivot.gameObject);
            foreach (var (go, layer) in swapped)
                if (go) go.layer = layer;
            swapped.Clear();
            overlay.enabled = false;
            backdrop.SetActive(false);

            var was = Current;
            Current = null;
            pressing = false;
            if (inspectLight) inspectLight.enabled = false;
            if (restoreOrthographic) viewCamera.orthographic = true;
            restoreOrthographic = false;
            UIRoot.Instance.Hud.HideInspect();
            was.OnInspectEnd();
            if (GameFlow.Instance.Mode == GameMode.Inspect) GameFlow.Instance.SetMode(GameMode.Explore);
        }

        public void RefreshHint()
        {
            if (Current) UIRoot.Instance.Hud.ShowInspect(Current.DisplayName, Current.InspectHint);
        }

        void Update()
        {
            var flow = GameFlow.Instance;
            if (!Current || flow.Mode != GameMode.Inspect || flow.NotebookOpen)
            {
                pressing = false;
                return;
            }

            if (GameInput.PointerDown && !PointerOverUI())
            {
                pressing = true;
                pressTravel = 0f;
            }

            if (pressing && GameInput.PointerHeld)
            {
                Vector2 d = GameInput.PointerDelta;
                pressTravel += d.magnitude;
                Rotate(d * rotateSpeed);
            }

            if (pressing && GameInput.PointerUp)
            {
                pressing = false;
                if (pressTravel < clickTolerance) Click(GameInput.PointerPosition);
            }
        }

        /// <summary>Turns the object: x spins it left/right, y tips it towards/away from the camera (degrees).</summary>
        public void Rotate(Vector2 degrees)
        {
            if (!pivot) return;
            var cam = viewCamera.transform;
            pivot.Rotate(cam.up, -degrees.x, Space.World);
            pivot.Rotate(cam.right, degrees.y, Space.World);
        }

        /// <summary>Clicks whatever part of the inspected object is under the pointer. True if it reacted.</summary>
        public bool Click(Vector2 screenPosition)
        {
            var hit = HitCurrent(screenPosition);
            if (!hit) return false;
            var clickable = hit.GetComponentInParent<IInspectClickable>();
            if (clickable == null) return false;
            clickable.OnInspectClick();
            return true;
        }

        /// <summary>Is this screen point over the inspected object (used to drop the antidote on a plate)?</summary>
        public bool IsOverCurrent(Vector2 screenPosition)
        {
            if (!Current) return false;
            if (HitCurrent(screenPosition)) return true;

            // Be forgiving: anywhere inside the object's on-screen rectangle counts.
            var b = WorldBounds(Current.transform);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(i % 2 == 0 ? b.min.x : b.max.x, (i / 2) % 2 == 0 ? b.min.y : b.max.y, i / 4 == 0 ? b.min.z : b.max.z);
                Vector2 s = viewCamera.WorldToScreenPoint(corner);
                min = Vector2.Min(min, s);
                max = Vector2.Max(max, s);
            }
            return screenPosition.x >= min.x && screenPosition.x <= max.x && screenPosition.y >= min.y && screenPosition.y <= max.y;
        }

        Collider HitCurrent(Vector2 screenPosition)
        {
            if (!Current) return null;
            Physics.SyncTransforms();
            var ray = viewCamera.ScreenPointToRay(screenPosition);
            Collider best = null;
            float bestDistance = float.MaxValue;
            foreach (var h in Physics.RaycastAll(ray, 3f, ~0, QueryTriggerInteraction.Collide))
            {
                if (h.distance < bestDistance && h.collider.transform.IsChildOf(Current.transform))
                {
                    best = h.collider;
                    bestDistance = h.distance;
                }
            }
            return best;
        }

        static bool PointerOverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        static Bounds WorldBounds(Transform t)
        {
            var renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(t.position, Vector3.one * 0.2f);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
