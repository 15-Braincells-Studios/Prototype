using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockoutMystery
{
    /// <summary>The open herb book. A / D or the buttons turn the pages; E or Escape closes it. Reading a page with evidence adds it to the journal.</summary>
    public class BookPanel : MonoBehaviour
    {
        TextMeshProUGUI title, body, pageLabel;
        GameObject herbPicture;
        Button previous, next;
        int page;

        public static BookPanel Create(RectTransform parent)
        {
            var panel = UIKit.Image("Book", parent, UIKit.Paper, true);
            panel.rectTransform.Anchors(0.28f, 0.1f, 0.72f, 0.9f);
            UIKit.Column(panel, 18, 44);
            var view = panel.gameObject.AddComponent<BookPanel>();

            view.title = UIKit.Label(panel.transform, "", 44, UIKit.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);

            // A little sprig, so the herb in the drawer and on the plate can be matched.
            var pic = UIKit.Rect("Herb Picture", panel.transform);
            UIKit.Size(pic, 70);
            var herb = CaseFile.ColorOf(HerbColor.Green);
            for (int i = 0; i < 4; i++)
            {
                var leaf = UIKit.Image("Leaf", pic, herb).rectTransform;
                leaf.Place(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30 + i * 34, (i % 2) * 14 - 7), new Vector2(26, 40));
                leaf.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 30 : -25);
            }
            view.herbPicture = pic.gameObject;

            view.body = UIKit.Label(panel.transform, "", 28, UIKit.Ink);
            UIKit.Size(view.body, -1, -1, -1, 1);

            var footer = UIKit.Rect("Footer", panel.transform);
            UIKit.Row(footer, 16, 0);
            UIKit.Size(footer, 54);
            view.previous = UIKit.Button(footer, "< Previous", () => view.Turn(-1), UIKit.PanelLight, UIKit.Text, 24, 50, 190);
            view.pageLabel = UIKit.Label(footer, "", 24, UIKit.InkDim, TextAlignmentOptions.Center);
            UIKit.Size(view.pageLabel, 50, -1, 1);
            view.next = UIKit.Button(footer, "Next >", () => view.Turn(1), UIKit.PanelLight, UIKit.Text, 24, 50, 190);

            UIKit.Label(panel.transform, "<b>A / D</b>  turn page      <b>E / Esc</b>  close", 22, UIKit.InkDim, TextAlignmentOptions.Center);

            panel.gameObject.SetActive(false);
            return view;
        }

        public void Open()
        {
            page = 0;
            gameObject.SetActive(true);
            Render();
            GameFlow.Instance.SetMode(GameMode.Book);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            if (GameFlow.Instance.Mode == GameMode.Book) GameFlow.Instance.SetMode(GameMode.Explore);
        }

        public void Turn(int direction)
        {
            page = Mathf.Clamp(page + direction, 0, CaseFile.BookPages.Length - 1);
            Render();
        }

        void Update()
        {
            if (GameFlow.Instance.NotebookOpen) return;
            if (GameInput.PageNext) Turn(1);
            else if (GameInput.PagePrevious) Turn(-1);
        }

        void Render()
        {
            var p = CaseFile.BookPages[page];
            title.text = p.title;
            body.text = p.body;
            herbPicture.SetActive(p.showsGreenHerb);
            pageLabel.text = $"Page {page + 1} / {CaseFile.BookPages.Length}";
            previous.interactable = page > 0;
            next.interactable = page < CaseFile.BookPages.Length - 1;
            if (p.evidence.HasValue) Notebook.Instance.RecordEvidence(p.evidence.Value);
        }
    }
}
