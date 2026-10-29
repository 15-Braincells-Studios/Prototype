using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockoutMystery
{
    /// <summary>
    /// Visual-novel conversation: the world is covered, a large live portrait of the speaker stands in the middle and
    /// the line sits in a big text box with a name tab. The portrait is the blockout NPC itself, rendered by a
    /// separate camera that only sees the speaker (moved to the TransparentFX layer while they talk).
    /// </summary>
    public class VisualNovelView : MonoBehaviour, IDialoguePresenter
    {
        const int PortraitLayer = 1; // TransparentFX: unused by the level, so the portrait camera sees only the speaker

        [SerializeField] float cameraDistance = 2.4f;
        [SerializeField] float cameraHeight = 1.4f;
        [SerializeField] float fieldOfView = 40f;

        readonly List<(GameObject go, int layer)> swapped = new List<(GameObject, int)>();
        TextMeshProUGUI nameLabel, body, hint;
        Typewriter typer;
        RawImage portrait;
        Camera portraitCamera;
        RenderTexture texture;
        Npc current;
        bool more;

        public bool IsTyping => typer.IsTyping;

        public static VisualNovelView Create(RectTransform parent)
        {
            // The world stays visible: only a text box at the bottom, with the speaker standing on it at the left.
            var screen = UIKit.Rect("Visual Novel", parent).Stretch();
            var view = screen.gameObject.AddComponent<VisualNovelView>();

            // Drawn before the box, so the box hides where the portrait is cut off at the waist.
            var frame = UIKit.Rect("Portrait Frame", screen);
            frame.Anchors(0.08f, 0.24f, 0.4f, 0.8f);
            view.portrait = UIKit.Rect("Portrait", frame).gameObject.AddComponent<RawImage>();
            view.portrait.raycastTarget = false;
            view.portrait.rectTransform.pivot = new Vector2(0.5f, 0f);
            var aspect = view.portrait.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 0.75f;

            var box = UIKit.Image("Text Box", screen, UIKit.Panel, true);
            box.rectTransform.Anchors(0.06f, 0.03f, 0.94f, 0.28f);
            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = UIKit.Accent;
            outline.effectDistance = new Vector2(2f, -2f);

            UIKit.Pill("Name", box.transform, UIKit.Accent, out view.nameLabel, 32, UIKit.Ink, 26, 8)
                .Place(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(36f, -6f));
            view.nameLabel.fontStyle = FontStyles.Bold;

            view.body = UIKit.Label(box.transform, "", 34, UIKit.Text);
            view.body.rectTransform.Stretch(44, 48, 44, 48);
            view.typer = view.body.gameObject.AddComponent<Typewriter>();
            view.hint = UIKit.Label(box.transform, "", 22, UIKit.TextDim, TextAlignmentOptions.BottomRight);
            view.hint.rectTransform.Stretch(44, 0, 32, 16);

            screen.gameObject.SetActive(false);
            return view;
        }

        public List<string> Paginate(string line) => new List<string> { line };

        public void Show(Npc who, string text, bool morePages)
        {
            more = morePages;
            gameObject.SetActive(true);
            if (who != current)
            {
                RestoreLayers();
                current = who;
                if (who) MoveToPortraitLayer(who);
            }

            EnsureCamera();
            if (who)
            {
                var t = who.transform;
                portraitCamera.transform.position = t.position + t.forward * cameraDistance + t.right * 0.45f + Vector3.up * cameraHeight;
                portraitCamera.transform.LookAt(t.position + Vector3.up * (cameraHeight - 0.05f));
            }
            portraitCamera.enabled = who;
            portrait.enabled = who;

            nameLabel.text = who ? who.DisplayName : "";
            hint.text = "";
            typer.Play(text);
        }

        public void Finish() => typer.Finish();

        public void Hide()
        {
            typer.Finish();
            RestoreLayers();
            if (portraitCamera) portraitCamera.enabled = false;
            gameObject.SetActive(false);
        }

        void Update()
        {
            string h = typer.IsTyping ? "" : Typewriter.Hint(more);
            if (hint.text != h) hint.text = h;
            // A little breathing, so the portrait doesn't feel like a photo.
            portrait.rectTransform.localScale = new Vector3(1f, 1f + Mathf.Sin(Time.unscaledTime * 1.8f) * 0.006f, 1f);
        }

        void OnDestroy()
        {
            RestoreLayers();
            if (texture) texture.Release();
        }

        void EnsureCamera()
        {
            if (portraitCamera) return;
            texture = new RenderTexture(768, 1024, 24, RenderTextureFormat.ARGB32) { name = "Portrait" };
            portraitCamera = new GameObject("Portrait Camera").AddComponent<Camera>();
            portraitCamera.clearFlags = CameraClearFlags.SolidColor;
            portraitCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            portraitCamera.cullingMask = 1 << PortraitLayer;
            portraitCamera.fieldOfView = fieldOfView;
            portraitCamera.nearClipPlane = 0.1f;
            portraitCamera.farClipPlane = 10f;
            portraitCamera.targetTexture = texture;
            portraitCamera.enabled = false;
            portrait.texture = texture;
        }

        void MoveToPortraitLayer(Npc who)
        {
            foreach (var r in who.GetComponentsInChildren<Renderer>())
            {
                if (r.GetComponent<TMP_Text>()) continue; // keep the floating name tag out of the portrait
                swapped.Add((r.gameObject, r.gameObject.layer));
                r.gameObject.layer = PortraitLayer;
            }
        }

        void RestoreLayers()
        {
            foreach (var (go, layer) in swapped)
                if (go) go.layer = layer;
            swapped.Clear();
            current = null;
        }
    }
}
