using System.Collections.Generic;
using UnityEngine;

namespace AssassinMystery
{
    /// <summary>
    /// A character in the scene. Identity and dialogue come from its <see cref="SuspectData"/>.
    /// Idles with a slow breath, turns to face the player while talking.
    /// </summary>
    public class NPCDialogue : Interactable
    {
        static readonly List<NPCDialogue> npcs = new List<NPCDialogue>();

        [SerializeField] SuspectData profile;
        [Tooltip("Visual child, animated for idle and cutscenes.")]
        [SerializeField] Transform model;

        Quaternion restRotation;
        Transform lookTarget;
        float breathOffset;

        public SuspectData Profile => profile;
        public Transform Model => model;
        public bool IsTalking { get; private set; }
        /// <summary>While true, the sequence owns this character's transform and model.</summary>
        public bool CutsceneControlled { get; set; }

        public override string Prompt => profile ? $"Talk to {profile.displayName} ({profile.role})" : "Talk";
        public override bool CanInteract => profile && !CutsceneControlled;

        public void Configure(SuspectData data, Transform modelRoot, float height)
        {
            profile = data;
            model = modelRoot;
            promptHeight = height;
        }

        public static NPCDialogue Find(SuspectData data)
        {
            foreach (var n in npcs)
                if (n.profile == data) return n;
            return null;
        }

        void Awake()
        {
            if (!profile) Debug.LogError($"[AssassinMystery] NPC '{name}' has no SuspectData.", this);
            if (!model) Debug.LogError($"[AssassinMystery] NPC '{name}' has no model transform.", this);
            restRotation = transform.rotation;
            breathOffset = Random.value * 10f;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            npcs.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            npcs.Remove(this);
        }

        public override void Interact() => DialogueManager.Instance.StartDialogue(this);

        public void BeginTalking(Transform listener)
        {
            IsTalking = true;
            lookTarget = listener;
        }

        public void EndTalking()
        {
            IsTalking = false;
            lookTarget = null;
        }

        void Update()
        {
            if (CutsceneControlled || !model) return;

            float t = Time.time + breathOffset;
            float talkBob = IsTalking ? Mathf.Abs(Mathf.Sin(t * 9f)) * 0.025f : 0f;
            model.localPosition = new Vector3(0f, Mathf.Sin(t * 1.6f) * 0.012f + talkBob, 0f);

            Quaternion wanted = restRotation;
            if (IsTalking && lookTarget)
            {
                Vector3 to = lookTarget.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f) wanted = Quaternion.LookRotation(to);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, 240f * Time.deltaTime);
        }
    }
}
