using TMPro;
using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>Mirrors the countdown on a prop in the world (the hourglass in the Great Hall).</summary>
    public class WorldClockDisplay : MonoBehaviour
    {
        [SerializeField] TMP_Text timeText;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color warningColor = new Color(1f, 0.3f, 0.25f);
        [SerializeField] float warningBelowSeconds = 120f;

        void Start()
        {
            var gm = GameManager.Instance;
            if (!gm) return;
            gm.OnTimeChanged += Refresh;
            Refresh(gm.TimeRemaining);
        }

        void OnDestroy()
        {
            if (GameManager.Instance) GameManager.Instance.OnTimeChanged -= Refresh;
        }

        void Refresh(float seconds)
        {
            timeText.text = ClockFormat.Format(seconds);
            timeText.color = seconds < warningBelowSeconds ? warningColor : normalColor;
        }
    }
}
