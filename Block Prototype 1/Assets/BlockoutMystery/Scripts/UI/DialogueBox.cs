using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockoutMystery
{
    /// <summary>
    /// Standard subtitles: the speaker's name and line in a box near the bottom of the screen. Raised above the
    /// inventory bar when the player keeps control; low and see-through for close-up shots.
    /// </summary>
    public class DialogueBox : MonoBehaviour, IDialoguePresenter
    {
        public enum Placement { Raised, Low }

        Image panel;
        TextMeshProUGUI speaker, body, hint;
        Typewriter typer;
        bool more;

        public bool IsTyping => typer.IsTyping;

        public static DialogueBox Create(RectTransform parent)
        {
            var panel = UIKit.Image("Dialogue", parent, UIKit.Panel);
            var box = panel.gameObject.AddComponent<DialogueBox>();
            box.panel = panel;

            box.speaker = UIKit.Label(panel.transform, "", 30, UIKit.Accent, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            box.speaker.rectTransform.Stretch(32, 18, 32, 0);
            box.body = UIKit.Label(panel.transform, "", 29, UIKit.Text);
            box.body.rectTransform.Stretch(32, 62, 32, 40);
            box.typer = box.body.gameObject.AddComponent<Typewriter>();
            box.hint = UIKit.Label(panel.transform, "", 22, UIKit.TextDim, TextAlignmentOptions.BottomRight);
            box.hint.rectTransform.Stretch(32, 0, 28, 12);

            box.SetPlacement(Placement.Raised);
            panel.gameObject.SetActive(false);
            return box;
        }

        public void SetPlacement(Placement placement)
        {
            if (placement == Placement.Raised)
            {
                panel.rectTransform.Anchors(0.2f, 0.21f, 0.8f, 0.4f); // above the prompt and the inventory bar
                panel.color = UIKit.Panel;
            }
            else
            {
                panel.rectTransform.Anchors(0.15f, 0.03f, 0.85f, 0.21f); // over the bottom letterbox bar
                panel.color = new Color(0f, 0f, 0f, 0.55f);
            }
        }

        public List<string> Paginate(string line) => new List<string> { line };

        public void Show(Npc who, string text, bool morePages)
        {
            gameObject.SetActive(true);
            speaker.text = who ? who.DisplayName : "";
            hint.text = "";
            more = morePages;
            typer.Play(text);
        }

        public void Finish() => typer.Finish();

        public void Hide()
        {
            typer.Finish();
            gameObject.SetActive(false);
        }

        void Update()
        {
            string h = typer.IsTyping ? "" : Typewriter.Hint(more);
            if (hint.text != h) hint.text = h;
        }
    }
}
