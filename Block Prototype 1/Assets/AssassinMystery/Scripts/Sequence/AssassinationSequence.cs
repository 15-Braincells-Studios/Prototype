using System;
using System.Collections;
using UnityEngine;

namespace AssassinMystery
{
    /// <summary>
    /// The two endings as scripted cutscenes at the head table: the detective knocking the poisoned goblet away,
    /// or the Queen drinking it. Characters are moved directly; the camera cuts between marker transforms.
    /// </summary>
    public class AssassinationSequence : MonoBehaviour
    {
        [Serializable]
        public class Markers
        {
            public Transform assassinStart;
            public Transform assassinStrike;
            public Transform detectiveStart;
            public Transform detectiveIntercept;
            public Transform captainHold;
            public Transform wideShot;
            public Transform closeShot;
            public Transform queenShot;
        }

        [SerializeField] NPCDialogue target;
        [Tooltip("Steps in to restrain the assassin in the good ending. Optional.")]
        [SerializeField] SuspectData guardCaptain;
        [SerializeField] ThirdPersonController player;
        [SerializeField] ThirdPersonCamera playerCamera;
        [SerializeField] Material gobletMaterial;
        [SerializeField] Material wineMaterial;
        [SerializeField] Markers markers = new Markers();

        Transform cam;

        public void Configure(NPCDialogue queen, SuspectData captain, ThirdPersonController detective, ThirdPersonCamera camera,
            Material goblet, Material wine, Markers points)
        {
            target = queen;
            guardCaptain = captain;
            player = detective;
            playerCamera = camera;
            gobletMaterial = goblet;
            wineMaterial = wine;
            markers = points;
        }

        void Awake()
        {
            if (!target) Debug.LogError("[AssassinMystery] AssassinationSequence has no target NPC.", this);
            if (!player) Debug.LogError("[AssassinMystery] AssassinationSequence has no player.", this);
            if (!playerCamera) Debug.LogError("[AssassinMystery] AssassinationSequence has no player camera.", this);
            if (markers == null || !markers.assassinStart || !markers.assassinStrike || !markers.detectiveStart ||
                !markers.detectiveIntercept || !markers.wideShot || !markers.closeShot || !markers.queenShot)
                Debug.LogError("[AssassinMystery] AssassinationSequence is missing cutscene markers.", this);
        }

        public void PlayPrevented(SuspectData assassin, Action onDone) => StartCoroutine(Prevented(assassin, onDone));
        public void PlayFailure(SuspectData assassin, Action onDone) => StartCoroutine(Failure(assassin, onDone));

        IEnumerator Prevented(SuspectData assassinData, Action onDone)
        {
            var ui = UIManager.Instance;
            var assassin = NPCDialogue.Find(assassinData);
            var captain = guardCaptain ? NPCDialogue.Find(guardCaptain) : null;
            if (captain == assassin) captain = null;

            yield return Begin();
            if (!assassin)
            {
                Debug.LogError("[AssassinMystery] The assassin has no NPC in the scene; skipping to the ending.");
                yield return End(onDone);
                yield break;
            }

            Place(assassin.transform, markers.assassinStart);
            assassin.CutsceneControlled = true;
            player.SetCutsceneMode(true);
            Place(player.transform, markers.detectiveStart);
            var goblet = MakeGoblet(assassin.Model);
            Cut(markers.wideShot);
            yield return ui.FadeTo(0f, 0.6f);

            ui.Caption("The vesper bell tolls. The hall falls silent for the Queen's toast.");
            yield return new WaitForSeconds(2.2f);

            ui.Caption($"{assassinData.displayName} steps forward, carrying the Queen's goblet.");
            yield return Walk(assassin.transform, assassin.Model, markers.assassinStrike.position, 1.6f);
            yield return Turn(target.transform, assassin.transform.position, 0.3f);

            Cut(markers.closeShot);
            ui.Caption("\"Your Majesty. The toast of Aldmere.\"");
            yield return new WaitForSeconds(1.6f);

            ui.Caption("<b>Detective:</b> \"Your Majesty, do not drink that!\"");
            yield return Walk(player.transform, player.Model, markers.detectiveIntercept.position, 6f, player);

            // Knock the goblet away.
            goblet.SetParent(null, true);
            Vector3 from = goblet.position;
            Vector3 to = markers.assassinStrike.position + markers.assassinStrike.right * 1.6f + Vector3.up * 0.02f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.55f)
            {
                goblet.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.8f;
                goblet.rotation = Quaternion.Euler(t * 540f, 0f, t * 200f);
                yield return null;
            }
            goblet.position = to;
            goblet.rotation = Quaternion.Euler(90f, 0f, 0f);
            MakeSpill(to);
            ui.Caption("The goblet clatters across the floor. Dark wine hisses on the stones.");
            yield return new WaitForSeconds(1.4f);

            // The assassin is restrained.
            Vector3 back = assassin.transform.position - assassin.transform.forward * 0.8f;
            yield return Walk(assassin.transform, assassin.Model, back, 2.5f, null, false);
            if (captain)
            {
                captain.CutsceneControlled = true;
                Vector3 hold = markers.captainHold ? markers.captainHold.position : back - assassin.transform.right * 0.9f;
                Place(captain.transform, hold + Vector3.left * 3.5f);
                yield return Walk(captain.transform, captain.Model, hold, 4.5f);
                yield return Turn(captain.transform, assassin.transform.position, 0.2f);
            }
            yield return Kneel(assassin.Model);
            ui.Caption(captain
                ? $"Captain Hale seizes {assassinData.displayName}. The goblet reeks of wolfsbane."
                : $"{assassinData.displayName} is seized. The goblet reeks of wolfsbane.");
            yield return new WaitForSeconds(2.6f);

            Cut(markers.queenShot);
            yield return Turn(target.transform, player.transform.position, 0.4f);
            ui.Caption("Queen Isolde is safe.");
            yield return new WaitForSeconds(2.6f);

            yield return End(onDone);
        }

        IEnumerator Failure(SuspectData assassinData, Action onDone)
        {
            var ui = UIManager.Instance;
            var assassin = NPCDialogue.Find(assassinData);

            yield return Begin();
            player.SetCutsceneMode(true);
            Place(player.transform, markers.detectiveStart);

            Transform goblet;
            if (assassin)
            {
                assassin.CutsceneControlled = true;
                Place(assassin.transform, markers.assassinStrike);
                assassin.transform.rotation = Quaternion.LookRotation(Flat(target.transform.position - assassin.transform.position));
                goblet = MakeGoblet(assassin.Model);
            }
            else goblet = MakeGoblet(target.Model);

            target.CutsceneControlled = true;
            Cut(markers.queenShot);
            yield return ui.FadeTo(0f, 0.6f);

            ui.Caption("The vesper bell tolls. Queen Isolde rises to give the toast.");
            yield return new WaitForSeconds(2.2f);

            // The goblet passes to the Queen.
            Transform queenHand = target.Model;
            Vector3 handLocal = new Vector3(0.3f, 1.15f, 0.3f);
            yield return MoveGoblet(goblet, () => queenHand.TransformPoint(handLocal), 0.8f);
            goblet.SetParent(queenHand, true);
            if (assassin) yield return Walk(assassin.transform, assassin.Model, assassin.transform.position - assassin.transform.forward * 1.2f, 1.5f, null, false);

            ui.Caption("\"To Aldmere!\" The Queen drinks deep.");
            yield return MoveGoblet(goblet, () => queenHand.TransformPoint(new Vector3(0.05f, 1.55f, 0.3f)), 0.6f);
            yield return new WaitForSeconds(1.4f);

            ui.Caption("The cup slips from her fingers...");
            goblet.SetParent(null, true);
            Vector3 g0 = goblet.position;
            Vector3 g1 = target.transform.position + target.transform.forward * 0.7f + Vector3.up * 0.03f;
            Quaternion q0 = target.Model.localRotation;
            Vector3 p0 = target.Model.localPosition;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.3f)
            {
                float e = t * t;
                goblet.position = Vector3.Lerp(g0, g1, Mathf.Min(1f, t * 2f));
                target.Model.localRotation = q0 * Quaternion.Euler(-85f * e, 0f, 8f * e);
                target.Model.localPosition = p0 + new Vector3(0f, 0.15f * e, 0f);
                yield return null;
            }
            MakeSpill(g1);
            ui.Caption("Queen Isolde collapses. The hall erupts in screams.");
            yield return new WaitForSeconds(2.4f);

            if (assassin)
            {
                Cut(markers.wideShot);
                ui.Caption("In the uproar, one figure slips quietly away...");
                yield return Walk(assassin.transform, assassin.Model, markers.assassinStart.position, 1.8f);
                yield return new WaitForSeconds(0.6f);
            }

            yield return End(onDone);
        }

        // ---------- helpers ----------

        IEnumerator Begin()
        {
            var ui = UIManager.Instance;
            ui.SetCinematic(true);
            ui.Caption("");
            yield return ui.FadeTo(1f, 0.6f);
            playerCamera.enabled = false;
            cam = playerCamera.transform;
        }

        IEnumerator End(Action onDone)
        {
            yield return UIManager.Instance.FadeTo(1f, 0.8f);
            UIManager.Instance.SetCinematic(false);
            onDone?.Invoke();
            yield return UIManager.Instance.FadeTo(0f, 0.6f);
        }

        void Cut(Transform shot)
        {
            if (shot) cam.SetPositionAndRotation(shot.position, shot.rotation);
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 0.0001f ? Vector3.forward : v;
        }

        static void Place(Transform t, Transform marker)
        {
            if (marker) t.SetPositionAndRotation(marker.position, marker.rotation);
        }

        static void Place(Transform t, Vector3 position) => t.position = position;

        IEnumerator Walk(Transform who, Transform model, Vector3 to, float speed, ThirdPersonController walker = null, bool face = true)
        {
            Vector3 from = who.position;
            float dist = Vector3.Distance(from, to);
            if (dist < 0.01f) yield break;
            if (face) who.rotation = Quaternion.LookRotation(Flat(to - from));
            float phase = 0f;
            for (float d = 0f; d < dist; d += speed * Time.deltaTime)
            {
                who.position = Vector3.Lerp(from, to, d / dist);
                phase += Time.deltaTime * speed * 4f;
                if (walker) walker.AnimateWalk(speed);
                else if (model) model.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(phase)) * 0.06f, 0f);
                yield return null;
            }
            who.position = to;
            if (walker) walker.AnimateWalk(0f);
            else if (model) model.localPosition = Vector3.zero;
        }

        static IEnumerator Turn(Transform who, Vector3 lookAt, float duration)
        {
            Quaternion from = who.rotation;
            Quaternion to = Quaternion.LookRotation(Flat(lookAt - who.position));
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                who.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            who.rotation = to;
        }

        static IEnumerator Kneel(Transform model)
        {
            Vector3 p0 = model.localPosition;
            Quaternion q0 = model.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f)
            {
                float e = Mathf.SmoothStep(0f, 1f, t);
                model.localPosition = p0 + new Vector3(0f, -0.45f * e, 0f);
                model.localRotation = q0 * Quaternion.Euler(22f * e, 0f, 0f);
                yield return null;
            }
        }

        static IEnumerator MoveGoblet(Transform goblet, Func<Vector3> to, float duration)
        {
            goblet.SetParent(null, true);
            Vector3 from = goblet.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                goblet.position = Vector3.Lerp(from, to(), Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            goblet.position = to();
        }

        Transform MakeGoblet(Transform holder)
        {
            var root = new GameObject("Poisoned Goblet").transform;
            var cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(cup.GetComponent<Collider>());
            cup.transform.SetParent(root, false);
            cup.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
            cup.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            if (gobletMaterial) cup.GetComponent<Renderer>().sharedMaterial = gobletMaterial;
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(stem.GetComponent<Collider>());
            stem.transform.SetParent(root, false);
            stem.transform.localScale = new Vector3(0.04f, 0.05f, 0.04f);
            if (gobletMaterial) stem.GetComponent<Renderer>().sharedMaterial = gobletMaterial;

            root.SetParent(holder, false);
            root.localPosition = new Vector3(0.32f, 1.05f, 0.3f);
            return root;
        }

        void MakeSpill(Vector3 at)
        {
            var spill = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(spill.GetComponent<Collider>());
            spill.name = "Spilled Wine";
            spill.transform.position = new Vector3(at.x, at.y - 0.01f, at.z);
            spill.transform.localScale = new Vector3(0.7f, 0.005f, 0.5f);
            if (wineMaterial) spill.GetComponent<Renderer>().sharedMaterial = wineMaterial;
        }
    }
}
