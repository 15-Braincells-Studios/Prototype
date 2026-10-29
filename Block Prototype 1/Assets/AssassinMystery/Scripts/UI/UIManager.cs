using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AssassinMystery
{
    public enum UIPanel { None, Dialogue, Inspect, Notebook, Accusation, Briefing, End }

    /// <summary>
    /// Builds the whole UI in code at startup (no scene wiring to break) and is the single owner of which panel
    /// is open. Only one panel can be open at a time; the pause menu sits on top of whatever is open.
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        public UIPanel ActivePanel { get; private set; } = UIPanel.None;

        RectTransform canvasRect;
        CanvasGroup hudGroup;
        TextMeshProUGUI timerText, timerLabel, promptText, evidenceText, captionText, bannerTitle, bannerBody, hintText;
        RectTransform timerPlate, promptPlate, toastStack, bannerPlate, letterTop, letterBottom;
        GameObject notebookButton;
        Image fade;
        float bannerUntil, hintUntil;

        DialogueView dialogue;
        InspectView inspect;
        NotebookView notebook;
        AccusationView accusation;
        MenuViews menus;

        void Awake()
        {
            Instance = this;
            EnsureEventSystem();
            Build();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            var inv = InvestigationManager.Instance;
            if (inv != null)
            {
                inv.OnClueDiscovered += r => Toast("New clue: " + r.clue.title, "Added to your notebook. Found at " + r.location + ".");
                inv.OnChanged += () =>
                {
                    RefreshEvidenceCount();
                    if (ActivePanel == UIPanel.Notebook) notebook.Refresh();
                };
            }
            if (GameTimer.Instance != null) GameTimer.Instance.OnTick += SetTime;
            hintUntil = Time.unscaledTime + 25f;
            RefreshEvidenceCount();
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        // ---------- construction ----------

        void Build()
        {
            var canvasGo = new GameObject("Mystery UI", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasRect = (RectTransform)canvasGo.transform;

            BuildHud();

            // Letterbox for cutscenes.
            letterTop = UIFactory.Panel("Letterbox Top", canvasRect, Color.black).rectTransform.Anchors(0, 0.88f, 1, 1);
            letterBottom = UIFactory.Panel("Letterbox Bottom", canvasRect, Color.black).rectTransform.Anchors(0, 0, 1, 0.14f);
            captionText = UIFactory.Text(letterBottom, "", 30, UIFactory.Parchment, TextAlignmentOptions.Center, FontStyles.Italic);
            captionText.rectTransform.Stretch(120, 10, 120, 10);
            letterTop.gameObject.SetActive(false);
            letterBottom.gameObject.SetActive(false);

            dialogue = new DialogueView(canvasRect);
            inspect = new InspectView(canvasRect);
            notebook = new NotebookView(canvasRect);
            accusation = new AccusationView(canvasRect);
            menus = new MenuViews(canvasRect);

            // The countdown stays readable above every panel.
            timerPlate.SetParent(canvasRect, true);

            fade = UIFactory.Panel("Fade", canvasRect, new Color(0, 0, 0, 0));
            fade.rectTransform.Stretch();
            fade.raycastTarget = false;
        }

        void BuildHud()
        {
            var hud = UIFactory.Rect("HUD", canvasRect).Stretch();
            hudGroup = hud.gameObject.AddComponent<CanvasGroup>();
            hudGroup.blocksRaycasts = false;

            // Countdown, top centre.
            var plate = UIFactory.Panel("Timer", hud, UIFactory.Dark, true);
            timerPlate = plate.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(300, 104));
            timerLabel = UIFactory.Text(timerPlate, "UNTIL THE VESPER BELL", 16, UIFactory.Gold, TextAlignmentOptions.Top, FontStyles.Bold);
            timerLabel.rectTransform.Stretch(8, 10, 8, 0);
            timerText = UIFactory.Text(timerPlate, "10:00", 56, UIFactory.Parchment, TextAlignmentOptions.Bottom, FontStyles.Bold);
            timerText.rectTransform.Stretch(8, 0, 8, 6);

            // Interaction prompt, follows the object on screen.
            var p = UIFactory.Panel("Prompt", hud, UIFactory.Dark, true);
            promptPlate = p.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(460, 50));
            promptText = UIFactory.Text(promptPlate, "", 24, UIFactory.Parchment, TextAlignmentOptions.Center);
            promptText.rectTransform.Stretch(12, 4, 12, 4);
            promptPlate.gameObject.SetActive(false);

            // Notebook button + evidence count, bottom right.
            var nb = UIFactory.Button(hud, "Notebook  [Tab]", () => { if (GameManager.Instance.State == GameState.Investigating) OpenNotebook(); },
                UIFactory.Leather, UIFactory.Parchment, 24, 56);
            nb.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(260, 56));
            nb.gameObject.AddComponent<Outline>().effectColor = UIFactory.Gold;
            notebookButton = nb.gameObject;
            evidenceText = UIFactory.Text(hud, "", 20, UIFactory.Gold, TextAlignmentOptions.BottomRight);
            evidenceText.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 88), new Vector2(400, 30));

            // Controls hint, bottom left.
            hintText = UIFactory.Text(hud,
                "<b>WASD</b> move   <b>Mouse</b> look   <b>Shift</b> hurry   <b>E</b> talk / search   <b>Tab</b> notebook   <b>Esc</b> pause",
                19, UIFactory.Parchment, TextAlignmentOptions.BottomLeft);
            hintText.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 24), new Vector2(1100, 30));

            // Toasts, top right.
            toastStack = UIFactory.Rect("Toasts", hud).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(440, 600));
            var vlg = UIFactory.Vertical(toastStack, 10);
            vlg.childAlignment = TextAnchor.UpperRight;

            // Centre banner for big messages (failed accusation).
            var b = UIFactory.Panel("Banner", hud, UIFactory.Dark, true);
            bannerPlate = b.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(820, 150));
            bannerTitle = UIFactory.Text(bannerPlate, "", 32, new Color(0.92f, 0.42f, 0.32f),
                TextAlignmentOptions.Top, FontStyles.Bold);
            bannerTitle.rectTransform.Stretch(20, 14, 20, 0);
            bannerBody = UIFactory.Text(bannerPlate, "", 21, UIFactory.Parchment, TextAlignmentOptions.Top);
            bannerBody.rectTransform.Stretch(26, 60, 26, 10);
            bannerPlate.gameObject.SetActive(false);
        }

        // ---------- per frame ----------

        void Update()
        {
            if (bannerPlate.gameObject.activeSelf && Time.unscaledTime > bannerUntil) bannerPlate.gameObject.SetActive(false);
            if (hintText.gameObject.activeSelf && Time.unscaledTime > hintUntil)
            {
                float a = Mathf.MoveTowards(hintText.alpha, 0f, Time.unscaledDeltaTime * 0.5f);
                hintText.alpha = a;
                if (a <= 0f) hintText.gameObject.SetActive(false);
            }

            bool free = ActivePanel == UIPanel.None;
            if (notebookButton.activeSelf != free)
            {
                notebookButton.SetActive(free);
                evidenceText.gameObject.SetActive(free);
            }
            timerPlate.gameObject.SetActive(hudGroup.alpha > 0f);

            var timer = GameTimer.Instance;
            if (timer != null && timer.IsRunning && timer.Remaining <= timer.WarningThreshold)
            {
                float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.08f;
                timerPlate.localScale = new Vector3(pulse, pulse, 1f);
            }
            else timerPlate.localScale = Vector3.one;
        }

        void SetTime(float remaining)
        {
            timerText.text = GameTimer.Format(remaining);
            var t = GameTimer.Instance;
            if (remaining <= t.WarningThreshold)
            {
                timerText.color = new Color(0.95f, 0.3f, 0.22f);
                timerLabel.text = "THE BELL IS NEAR";
            }
            else if (remaining <= t.CautionThreshold)
            {
                timerText.color = new Color(0.98f, 0.7f, 0.3f);
                timerLabel.text = "UNTIL THE VESPER BELL";
            }
            else
            {
                timerText.color = UIFactory.Parchment;
                timerLabel.text = "UNTIL THE VESPER BELL";
            }
        }

        void RefreshEvidenceCount()
        {
            var inv = InvestigationManager.Instance;
            int n = inv != null ? inv.Clues.Count : 0;
            evidenceText.text = n == 0 ? "No evidence yet" : $"{n} clue{(n == 1 ? "" : "s")} in notebook";
        }

        // ---------- HUD API ----------

        public void ShowPrompt(string text, Vector3 worldPosition)
        {
            var cam = Camera.main;
            if (!cam) return;
            Vector3 screen = cam.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f) screen = new Vector3(Screen.width * 0.5f, Screen.height * 0.25f, 1f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            Vector2 half = canvasRect.rect.size * 0.5f;
            local.x = Mathf.Clamp(local.x, -half.x + 240, half.x - 240);
            local.y = Mathf.Clamp(local.y, -half.y + 120, half.y - 220);
            promptPlate.anchoredPosition = local;

            promptText.text = text;
            promptPlate.sizeDelta = new Vector2(Mathf.Min(promptText.GetPreferredValues(text).x + 40f, 760f), 50f);
            promptPlate.gameObject.SetActive(true);
        }

        public void HidePrompt()
        {
            if (promptPlate) promptPlate.gameObject.SetActive(false);
        }

        public void Toast(string title, string body)
        {
            while (toastStack.childCount >= 3) // keep the newest few readable
            {
                var oldest = toastStack.GetChild(0);
                oldest.SetParent(null, false);
                Destroy(oldest.gameObject);
            }
            var plate = UIFactory.Panel("Toast", toastStack, UIFactory.Dark, true);
            UIFactory.Vertical(plate, 4, 14);
            UIFactory.Layout(plate, -1, 440);
            UIFactory.Text(plate.transform, title, 23, UIFactory.Gold, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UIFactory.Text(plate.transform, body, 18, UIFactory.Parchment);
            var group = plate.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(ToastLife(group));
        }

        IEnumerator ToastLife(CanvasGroup g)
        {
            yield return new WaitForSecondsRealtime(5f);
            for (float a = 1f; a > 0f; a -= Time.unscaledDeltaTime * 1.5f)
            {
                if (!g) yield break;
                g.alpha = a;
                yield return null;
            }
            if (g) Destroy(g.gameObject);
        }

        public void ShowBanner(string title, string body)
        {
            bannerTitle.text = title;
            bannerBody.text = body;
            bannerPlate.gameObject.SetActive(true);
            bannerUntil = Time.unscaledTime + 7f;
        }

        // ---------- panels ----------

        void Open(UIPanel panel)
        {
            if (ActivePanel != UIPanel.None && ActivePanel != panel) CloseActivePanel();
            ActivePanel = panel;
            HidePrompt();
        }

        /// <summary>Closes whatever is open, the way Esc does (Accusation goes back to the notebook).</summary>
        public void CloseActivePanel()
        {
            switch (ActivePanel)
            {
                case UIPanel.Dialogue:
                    DialogueManager.Instance.EndDialogue(); // calls HideDialogue
                    break;
                case UIPanel.Inspect:
                    inspect.Hide();
                    ActivePanel = UIPanel.None;
                    break;
                case UIPanel.Notebook:
                    notebook.Hide();
                    ActivePanel = UIPanel.None;
                    break;
                case UIPanel.Accusation:
                    accusation.Hide();
                    ActivePanel = UIPanel.None;
                    OpenNotebook();
                    break;
            }
        }

        public void CloseAllPanels()
        {
            if (ActivePanel == UIPanel.Dialogue) DialogueManager.Instance.EndDialogue();
            inspect.Hide();
            notebook.Hide();
            accusation.Hide();
            ActivePanel = UIPanel.None;
        }

        public void ShowDialogue(NPCDialogue npc, string text)
        {
            Open(UIPanel.Dialogue);
            dialogue.Show(npc, text);
        }

        public void ShowDialogueResponse(string text) => dialogue.ShowResponse(text);

        public void HideDialogue()
        {
            dialogue.Hide();
            if (ActivePanel == UIPanel.Dialogue) ActivePanel = UIPanel.None;
        }

        public void ShowInspection(SearchableObject obj, bool newClue)
        {
            Open(UIPanel.Inspect);
            inspect.Show(obj, newClue);
        }

        public void OpenNotebook()
        {
            Open(UIPanel.Notebook);
            notebook.Show();
        }

        public void OpenAccusation()
        {
            notebook.Hide();
            ActivePanel = UIPanel.None;
            Open(UIPanel.Accusation);
            accusation.Show();
        }

        public void ShowPause(bool on) => menus.ShowPause(on);

        public void ShowBriefing(MysteryData mystery)
        {
            Open(UIPanel.Briefing);
            hudGroup.alpha = 0f;
            menus.ShowBriefing(mystery);
        }

        public void HideBriefing()
        {
            menus.HideBriefing();
            hudGroup.alpha = 1f;
            hintUntil = Time.unscaledTime + 30f;
            hintText.gameObject.SetActive(true);
            hintText.alpha = 1f;
            if (ActivePanel == UIPanel.Briefing) ActivePanel = UIPanel.None;
        }

        public void ShowEnd(bool won, string title, string body)
        {
            CloseAllPanels();
            ActivePanel = UIPanel.End;
            hudGroup.alpha = 0f;
            menus.ShowEnd(won, title, body);
        }

        // ---------- cinematic ----------

        public void SetCinematic(bool on)
        {
            hudGroup.alpha = on ? 0f : 1f;
            letterTop.gameObject.SetActive(on);
            letterBottom.gameObject.SetActive(on);
            HidePrompt();
        }

        public void Caption(string text) => captionText.text = text;

        public IEnumerator FadeTo(float alpha, float duration)
        {
            float start = fade.color.a;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                fade.color = new Color(0, 0, 0, Mathf.Lerp(start, alpha, t));
                yield return null;
            }
            fade.color = new Color(0, 0, 0, alpha);
        }
    }
}
