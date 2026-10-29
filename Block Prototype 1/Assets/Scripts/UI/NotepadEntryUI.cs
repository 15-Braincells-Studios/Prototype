using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PreventTheMurder
{
    public class NotepadEntryUI : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] TMP_Text summaryText;
        [SerializeField] TMP_Text tagText;
        [SerializeField] Color highlightColor = new Color(1f, 0.82f, 0.4f, 1f);

        Color baseColor;
        Coroutine highlight;

        void Awake() => baseColor = background.color;

        public void Set(string summary, string tag)
        {
            summaryText.text = summary;
            tagText.text = tag;
        }

        public void Highlight(float seconds)
        {
            if (highlight != null) StopCoroutine(highlight);
            highlight = StartCoroutine(Fade(seconds));
        }

        IEnumerator Fade(float seconds)
        {
            for (float t = 0f; t < 1f;)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / seconds);
                background.color = Color.Lerp(highlightColor, baseColor, t * t);
                yield return null;
            }
            highlight = null;
        }
    }
}
