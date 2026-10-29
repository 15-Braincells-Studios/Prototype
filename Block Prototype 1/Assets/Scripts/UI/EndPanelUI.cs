using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PreventTheMurder
{
    /// <summary>Win or lose screen with a Restart button.</summary>
    public class EndPanelUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button restartButton;

        void Awake()
        {
            root.SetActive(false);
            restartButton.onClick.AddListener(() => GameManager.Instance.Restart());
        }

        public void Show(string title, string body)
        {
            titleText.text = title;
            bodyText.text = body;
            root.SetActive(true);
        }
    }
}
