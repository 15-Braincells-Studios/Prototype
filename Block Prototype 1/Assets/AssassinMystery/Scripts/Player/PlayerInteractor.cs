using UnityEngine;

namespace AssassinMystery
{
    /// <summary>
    /// Picks the nearest interactable in front of the player, shows its prompt and fires it on E.
    /// Only one thing can be interacted with at a time, and nothing while a panel is open.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] float range = 2.3f;
        [Tooltip("Max angle between the camera's view direction and the object.")]
        [SerializeField] float maxAngle = 75f;
        [SerializeField] float maxHeightDifference = 2.2f;

        Interactable current;
        Transform cam;

        void Start()
        {
            cam = Camera.main ? Camera.main.transform : null;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            if (ui == null) return;

            bool canInteract = gm == null || gm.CanControlPlayer;
            current = canInteract ? FindBest() : null;

            if (current) ui.ShowPrompt($"<color=#E8C46A>[E]</color> {current.Prompt}", current.PromptPosition);
            else ui.HidePrompt();

            if (current && GameInput.InteractPressed)
            {
                ui.HidePrompt();
                current.Interact();
            }
        }

        Interactable FindBest()
        {
            Vector3 facing = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : transform.forward;
            Interactable best = null;
            float bestScore = float.MaxValue;

            foreach (var i in Interactable.All)
            {
                if (!i.isActiveAndEnabled || !i.CanInteract) continue;
                Vector3 to = i.InteractionPoint - transform.position;
                if (Mathf.Abs(to.y) > maxHeightDifference) continue;
                to.y = 0f;
                float d = to.magnitude;
                if (d > range + i.ExtraRange) continue;
                float angle = d < 0.5f ? 0f : Vector3.Angle(facing, to);
                if (angle > maxAngle) continue;

                float score = d + angle * 0.025f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best;
        }
    }
}
