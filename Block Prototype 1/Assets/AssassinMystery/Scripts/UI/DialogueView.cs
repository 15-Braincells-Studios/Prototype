using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AssassinMystery
{
    /// <summary>Bottom-of-screen conversation panel: who is speaking, what they said, and what you can ask next.</summary>
    public class DialogueView
    {
        readonly RectTransform root;
        readonly Image portrait;
        readonly Image portraitAccent;
        readonly TextMeshProUGUI nameText;
        readonly TextMeshProUGUI roleText;
        readonly TextMeshProUGUI bodyText;
        readonly RectTransform topicList;
        readonly ScrollRect topicScroll;
        NPCDialogue npc;

        public DialogueView(RectTransform canvas)
        {
            root = UIFactory.Panel("Dialogue", canvas, UIFactory.Parchment, true).rectTransform.Anchors(0.1f, 0.03f, 0.9f, 0.47f);

            portrait = UIFactory.Panel("Portrait", root, Color.grey, true);
            portrait.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -24), new Vector2(72, 72));
            portraitAccent = UIFactory.Panel("Accent", portrait.transform, Color.white);
            portraitAccent.rectTransform.Anchors(0, 0, 1, 0.22f);

            nameText = UIFactory.Text(root, "", 34, UIFactory.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            nameText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(116, -20), new Vector2(760, 44));
            roleText = UIFactory.Text(root, "", 22, UIFactory.InkFaded, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            roleText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(116, -62), new Vector2(760, 30));

            var divider = UIFactory.Panel("Divider", root, UIFactory.InkFaded);
            divider.rectTransform.anchorMin = new Vector2(0, 1);
            divider.rectTransform.anchorMax = new Vector2(0.58f, 1);
            divider.rectTransform.offsetMin = new Vector2(26, -112);
            divider.rectTransform.offsetMax = new Vector2(0, -110);

            var body = UIFactory.Rect("Body", root);
            body.anchorMin = Vector2.zero;
            body.anchorMax = new Vector2(0.58f, 1);
            body.offsetMin = new Vector2(30, 26);
            body.offsetMax = new Vector2(-6, -124);
            bodyText = UIFactory.Text(body, "", 25, UIFactory.Ink);
            bodyText.rectTransform.Stretch();
            bodyText.enableAutoSizing = true;
            bodyText.fontSizeMin = 16;
            bodyText.fontSizeMax = 25;

            var right = UIFactory.Rect("Topics", root);
            right.anchorMin = new Vector2(0.6f, 0);
            right.anchorMax = Vector2.one;
            right.offsetMin = new Vector2(0, 84);
            right.offsetMax = new Vector2(-24, -22);
            var ask = UIFactory.Text(right, "Ask about:", 22, UIFactory.InkFaded, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            ask.rectTransform.anchorMin = new Vector2(0, 1);
            ask.rectTransform.anchorMax = Vector2.one;
            ask.rectTransform.offsetMin = new Vector2(6, -30);
            ask.rectTransform.offsetMax = Vector2.zero;
            var listArea = UIFactory.Rect("List", right).Stretch(0, 34, 0, 0);
            topicList = UIFactory.ScrollList(listArea, 6, out topicScroll);
            topicScroll.GetComponent<RectTransform>().Stretch();

            var bye = UIFactory.Button(root, "Farewell   [Esc]", () => DialogueManager.Instance.EndDialogue(), UIFactory.Leather, UIFactory.Parchment, 22, 50);
            bye.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 22), new Vector2(300, 50));

            root.gameObject.SetActive(false);
        }

        public void Show(NPCDialogue speaker, string greeting)
        {
            npc = speaker;
            var p = speaker.Profile;
            nameText.text = p.displayName;
            roleText.text = p.role;
            portrait.color = p.coatColor;
            portraitAccent.color = p.accentColor;
            bodyText.text = greeting;
            root.gameObject.SetActive(true);
            BuildTopics();
            topicScroll.verticalNormalizedPosition = 1f;
        }

        public void ShowResponse(string text)
        {
            bodyText.text = text;
            BuildTopics();
        }

        public void Hide()
        {
            root.gameObject.SetActive(false);
            npc = null;
        }

        void BuildTopics()
        {
            UIFactory.Clear(topicList);
            if (!npc) return;
            var dm = DialogueManager.Instance;
            foreach (var topic in dm.AvailableTopics(npc))
            {
                var t = topic;
                bool asked = dm.WasAsked(t);
                string label = (t.requiresClue ? $"<color=#{UIFactory.Hex(UIFactory.Blood)}><b>[Evidence]</b></color> " : "") + t.question;
                var bg = asked ? new Color(0.74f, 0.66f, 0.5f) : UIFactory.ParchmentDark;
                var btn = UIFactory.Button(topicList, label, () => dm.Ask(t), bg, asked ? UIFactory.InkFaded : UIFactory.Ink, 21, 48);
                var text = btn.GetComponentInChildren<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.enableAutoSizing = true;
                text.fontSizeMin = 14;
                text.fontSizeMax = 21;
                if (asked) text.fontStyle = FontStyles.Italic;
            }
        }
    }
}
