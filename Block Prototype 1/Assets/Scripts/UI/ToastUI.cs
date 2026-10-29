using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>Short messages that fade in and out one after another ("New clue: ...").</summary>
    public class ToastUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text messageText;
        [SerializeField] float showSeconds = 2.2f;
        [SerializeField] float fadeSeconds = 0.25f;

        readonly Queue<string> queue = new Queue<string>();
        Coroutine running;

        void Awake() => group.alpha = 0f;

        public void Show(string message)
        {
            queue.Enqueue(message);
            if (running == null) running = StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            while (queue.Count > 0)
            {
                messageText.text = queue.Dequeue();
                yield return Fade(0f, 1f);
                yield return new WaitForSecondsRealtime(showSeconds);
                yield return Fade(1f, 0f);
            }
            running = null;
        }

        IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < 1f;)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / fadeSeconds);
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
        }
    }
}
