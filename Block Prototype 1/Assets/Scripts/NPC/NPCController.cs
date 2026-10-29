using TMPro;
using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>
    /// A talkable character. Identity and dialogue come from the scenario (matched by characterId),
    /// so the same scene NPC can play a different part in another story.
    /// </summary>
    public class NPCController : MonoBehaviour, IInteractable
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] string characterId;
        [SerializeField] NPCWaypointMover mover;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Renderer bodyRenderer;
        [SerializeField] float turnSpeed = 360f;

        Transform faceTarget;

        public string CharacterId => characterId;
        public CharacterData Character { get; private set; }
        public DialogueData Dialogue { get; private set; }
        public string DisplayName => Character ? Character.displayName : name;
        public bool InConversation => faceTarget != null;

        public void Bind(CharacterData character, DialogueData dialogue)
        {
            Character = character;
            Dialogue = dialogue;
            if (nameLabel) nameLabel.text = character.displayName;
            if (bodyRenderer)
            {
                var block = new MaterialPropertyBlock();
                bodyRenderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, character.color);
                bodyRenderer.SetPropertyBlock(block);
            }
        }

        public string GetPrompt() => Dialogue ? $"[{InputReader.InteractKeyLabel}] Talk to {DisplayName}" : null;

        public void Interact(PlayerInteractor player) => DialogueManager.Instance.StartDialogue(this, player.transform);

        public void BeginConversation(Transform player)
        {
            faceTarget = player;
            if (mover) mover.SetHeld(true);
        }

        public void EndConversation()
        {
            faceTarget = null;
            if (mover) mover.SetHeld(false);
        }

        void Update()
        {
            if (!faceTarget) return;
            Vector3 toPlayer = faceTarget.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f) return;
            Quaternion facing = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * Time.deltaTime);
        }
    }
}
