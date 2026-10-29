using TMPro;
using UnityEngine;

namespace AssassinMystery
{
    /// <summary>Inspection card for searched furniture: what you see, and the clue if there is one.</summary>
    public class InspectView
    {
        readonly RectTransform root;
        readonly TextMeshProUGUI title, room, description, clueHeader, clueTitle, clueBody;

        public InspectView(RectTransform canvas)
        {
            root = UIFactory.Panel("Inspect", canvas, UIFactory.Parchment, true).rectTransform.Anchors(0.29f, 0.16f, 0.71f, 0.84f);
            var content = UIFactory.Rect("Content", root).Stretch(36, 30, 36, 96);
            UIFactory.Vertical(content, 10);

            room = UIFactory.Text(content, "", 20, UIFactory.InkFaded, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            title = UIFactory.Text(content, "", 38, UIFactory.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UIFactory.Divider(content, UIFactory.InkFaded);
            description = UIFactory.Text(content, "", 24, UIFactory.Ink);
            UIFactory.Divider(content, new Color(0, 0, 0, 0), 10);
            clueHeader = UIFactory.Text(content, "", 22, UIFactory.Blood, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            clueTitle = UIFactory.Text(content, "", 28, UIFactory.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            clueBody = UIFactory.Text(content, "", 23, UIFactory.Ink, TextAlignmentOptions.TopLeft, FontStyles.Italic);

            var close = UIFactory.Button(root, "Close   [E]", () => UIManager.Instance.CloseActivePanel(), UIFactory.Leather, UIFactory.Parchment, 22, 52);
            close.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(260, 52));

            root.gameObject.SetActive(false);
        }

        public void Show(SearchableObject obj, bool newClue)
        {
            room.text = obj.RoomName;
            title.text = obj.ObjectName;
            description.text = obj.Description;

            var clue = obj.Clue;
            if (clue)
            {
                clueHeader.text = newClue ? "CLUE DISCOVERED" : "ALREADY IN YOUR NOTEBOOK";
                clueHeader.color = newClue ? UIFactory.Blood : UIFactory.InkFaded;
                clueTitle.text = clue.title;
                clueBody.text = clue.description;
            }
            else
            {
                clueHeader.text = "";
                clueTitle.text = "";
                clueBody.text = obj.NothingFoundText;
            }
            root.gameObject.SetActive(true);
        }

        public void Hide() => root.gameObject.SetActive(false);
    }
}
