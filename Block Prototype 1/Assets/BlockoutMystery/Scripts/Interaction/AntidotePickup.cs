using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>The single antidote portion in the drawer. Can only be taken once the drawer is open.</summary>
    public class AntidotePickup : Interactable
    {
        [SerializeField] Drawer drawer;

        public override bool Available => !drawer || drawer.IsOpen;
        public override string Prompt => "Take the antidote";

        public void Configure(Drawer inDrawer) => drawer = inDrawer;

        public override void Interact()
        {
            Inventory.Instance.Add(ItemId.Antidote);
            gameObject.SetActive(false);
            Notebook.Instance.RecordEvidence(CaseFile.AntidoteEvidence, announce: false);
            UIRoot.Toast("You took the silverleaf tonic (the antidote). There is only one portion. Noted in your journal.");
        }
    }
}
