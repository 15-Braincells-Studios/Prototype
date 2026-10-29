using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>Shows a "..." bubble over a character while the player is close enough to talk.</summary>
    public class SpeechBubble : MonoBehaviour
    {
        [SerializeField] GameObject bubble;
        [SerializeField] float showDistance = 4f;
        [Tooltip("Optional. The bubble hides while this NPC is in conversation.")]
        [SerializeField] NPCController npc;

        Transform player;

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject) player = playerObject.transform;
            if (bubble) bubble.SetActive(false);
        }

        void Update()
        {
            if (!player || !bubble) return;
            bool inRange = (player.position - transform.position).sqrMagnitude <= showDistance * showDistance;
            bool show = inRange && (npc == null || !npc.InConversation);
            if (bubble.activeSelf != show) bubble.SetActive(show);
        }
    }
}
