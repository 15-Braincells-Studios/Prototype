using TMPro;
using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>Reveals a TextMeshPro text a few characters at a time.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class Typewriter : MonoBehaviour
    {
        [SerializeField] float charactersPerSecond = 60f;

        TMP_Text text;
        float shown;
        int total;

        public bool IsTyping { get; private set; }

        public void Play(string line)
        {
            if (!text) text = GetComponent<TMP_Text>();
            text.text = line;
            text.maxVisibleCharacters = 0;
            shown = 0f;
            total = line.Length;
            IsTyping = true;
        }

        public void Finish()
        {
            IsTyping = false;
            if (text) text.maxVisibleCharacters = 99999;
        }

        void Update()
        {
            if (!IsTyping) return;
            shown += Time.unscaledDeltaTime * charactersPerSecond;
            text.maxVisibleCharacters = (int)shown;
            if (shown >= total) Finish();
        }

        /// <summary>The small "E next / E close" hint shown once a line has finished typing.</summary>
        public static string Hint(bool more) =>
            $"<color=#{UIKit.Hex(UIKit.Accent)}><b>E</b></color>  {(more ? "next" : "close")}";
    }
}
