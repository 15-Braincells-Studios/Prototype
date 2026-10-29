using TMPro;
using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>The win / lose popup with the time, the correct solution (on a loss) and a Restart button.</summary>
    public class EndPopup : MonoBehaviour
    {
        TextMeshProUGUI title, body;

        public static EndPopup Create(RectTransform parent)
        {
            var dim = UIKit.Image("End Screen", parent, new Color(0f, 0f, 0f, 0.7f), true);
            dim.rectTransform.Stretch();
            var view = dim.gameObject.AddComponent<EndPopup>();

            var card = UIKit.Image("Card", dim.transform, UIKit.Panel, true);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 0));
            UIKit.Column(card, 22, 44);
            card.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            view.title = UIKit.Label(card.transform, "", 52, UIKit.Text, TextAlignmentOptions.Center, FontStyles.Bold);
            view.body = UIKit.Label(card.transform, "", 26, UIKit.Text, TextAlignmentOptions.Top);

            var buttons = UIKit.Rect("Buttons", card.transform);
            UIKit.Row(buttons, 16, 0);
            UIKit.Size(buttons, 60);
            UIKit.Button(buttons, "Restart", () => GameFlow.Instance.Restart(), UIKit.Accent, UIKit.Ink, 28, 58, 240);
            UIKit.Button(buttons, "Quit", Application.Quit, UIKit.PanelLight, UIKit.Text, 28, 58, 160);

            dim.gameObject.SetActive(false);
            return view;
        }

        public void Show(CaseResult r)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            UIRoot.Instance.Hud.ClearToast();
            string time = $"Time: <b>{GameTimer.Format(r.time)}</b>";
            string report = ReportQuality(r);

            if (r.won)
            {
                title.text = "You won!";
                title.color = UIKit.Good;
                body.text = "You prevented the murder.\n\nThe Chef hid the deadly green herbs on the Prince's plate, " +
                            "but your antidote neutralised them, and you named the Chef and the Prince to the Butler.\n\n" + report + "\n\n" + time;
                return;
            }

            title.text = "The murder takes place after all...";
            title.color = UIKit.Bad;
            string what = r.plateRight
                ? "The antidote was on the right plate, but you named the wrong people. The Chef walks free and poisons the Prince the next evening."
                : "The Prince eats from the plate with the green herbs and collapses at the table.";
            body.text = what + "\n\n" +
                        "<b>Your report</b>\n" +
                        $"Antidote on: {CaseFile.HerbName(r.plate)} herbs  {Mark(r.plateRight)}\n" +
                        $"Suspect: {CaseFile.NameOf(r.suspect)}  {Mark(r.suspectRight)}\n" +
                        $"Victim: {CaseFile.NameOf(r.victim)}  {Mark(r.victimRight)}\n\n" +
                        report + "\n\n" +
                        $"<b><color=#{UIKit.Hex(UIKit.Accent)}>The solution</color></b>\n" +
                        $"Suspect: {CaseFile.NameOf(CaseFile.SolutionSuspect)}\n" +
                        $"Victim: {CaseFile.NameOf(CaseFile.SolutionVictim)}\n" +
                        $"Poisoned plate: the one with the {CaseFile.HerbName(CaseFile.PoisonedPlate)} herbs\n" +
                        "Strongest evidence: the guard's testimony and the herbs in the closet drawer, backed by the herb book or the cook's testimony\n\n" + time;
        }

        /// <summary>The evidence grade, graded apart from whether the accusation was right.</summary>
        static string ReportQuality(CaseResult r)
        {
            var (color, why) = r.strength switch
            {
                CaseStrength.Strong => (UIKit.Good,
                    "Your evidence ties the Chef to the poison: he gathered the herbs himself, which isn't his job, and the same herbs were hidden in the kitchen closet."),
                CaseStrength.Plausible => (UIKit.Accent,
                    "Your evidence makes the Chef look suspicious, but it doesn't establish that he is responsible."),
                _ => (UIKit.Bad, "Your evidence doesn't connect the poisoning to anyone in particular."),
            };
            string pieces = r.evidenceCount == 1 ? "1 piece" : $"{r.evidenceCount} pieces";
            string extra = r.irrelevant >= 2 ? " Too much irrelevant evidence weakened your report." : "";
            return $"<b>Report quality: <color=#{UIKit.Hex(color)}>{CaseFile.StrengthName(r.strength)}</color></b>\n" +
                   $"{why}{extra}\n" +
                   $"<size=85%>{pieces} handed in, {r.supporting} of them relevant.</size>";
        }

        static string Mark(bool right) =>
            right ? $"<color=#{UIKit.Hex(UIKit.Good)}>right</color>" : $"<color=#{UIKit.Hex(UIKit.Bad)}>wrong</color>";
    }
}
