using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PreventTheMurder
{
    /// <summary>Bottom-of-screen dialogue box: speaker, line, Continue, and a topic button list.</summary>
    public class DialoguePanelUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text speakerText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button continueButton;
        [SerializeField] RectTransform topicContainer;
        [SerializeField] Button topicButtonTemplate;
        [SerializeField] string askedSuffix = "  (asked)";
        [SerializeField] Color askedTextColor = new Color(0.55f, 0.5f, 0.42f);

        readonly List<Button> spawnedButtons = new List<Button>();
        Action onContinue;

        void Awake()
        {
            root.SetActive(false);
            topicButtonTemplate.gameObject.SetActive(false);
            continueButton.onClick.AddListener(Continue);
        }

        public void Open(string speaker)
        {
            speakerText.text = speaker;
            bodyText.text = string.Empty;
            root.SetActive(true);
        }

        public void Close()
        {
            ClearTopics();
            onContinue = null;
            root.SetActive(false);
        }

        public void ShowLine(string text, Action continueAction)
        {
            ClearTopics();
            bodyText.text = text;
            onContinue = continueAction;
            continueButton.gameObject.SetActive(true);
        }

        public void ShowTopics(IReadOnlyList<(string label, bool asked)> topics, Action<int> onChosen, string farewellLabel, Action onFarewell)
        {
            ClearTopics();
            onContinue = null;
            continueButton.gameObject.SetActive(false);

            for (int i = 0; i < topics.Count; i++)
            {
                int index = i;
                AddButton(topics[i].asked ? topics[i].label + askedSuffix : topics[i].label, topics[i].asked, () => onChosen(index));
            }
            AddButton(farewellLabel, false, onFarewell);
        }

        void AddButton(string label, bool dimmed, Action onClick)
        {
            var button = Instantiate(topicButtonTemplate, topicContainer);
            button.gameObject.SetActive(true);
            var text = button.GetComponentInChildren<TMP_Text>();
            text.text = label;
            if (dimmed) text.color = askedTextColor;
            button.onClick.AddListener(() => onClick());
            spawnedButtons.Add(button);
        }

        void ClearTopics()
        {
            foreach (var button in spawnedButtons)
            {
                button.gameObject.SetActive(false); // Destroy is deferred; hide now so the layout updates this frame
                Destroy(button.gameObject);
            }
            spawnedButtons.Clear();
        }

        void Continue()
        {
            var action = onContinue;
            onContinue = null;
            action?.Invoke();
        }

        void Update()
        {
            if (onContinue != null && InputReader.TryConsumeInteract(true)) Continue();
        }
    }
}
