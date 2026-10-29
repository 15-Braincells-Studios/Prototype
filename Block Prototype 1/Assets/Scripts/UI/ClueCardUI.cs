using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PreventTheMurder
{
    /// <summary>Centered parchment card for found clues and the intro text. Closed with E, Space or the button.</summary>
    public class ClueCardUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button continueButton;

        public bool IsOpen => root.activeSelf;

        void Awake()
        {
            root.SetActive(false);
            continueButton.onClick.AddListener(Hide);
        }

        public void Show(string title, string body)
        {
            titleText.text = title;
            bodyText.text = body;
            if (IsOpen) return;
            root.SetActive(true);
            UIManager.Instance.PushModal();
        }

        public void Hide()
        {
            if (!IsOpen) return;
            root.SetActive(false);
            UIManager.Instance.PopModal();
        }

        void Update()
        {
            if (IsOpen && InputReader.TryConsumeInteract(true)) Hide();
        }
    }
}
