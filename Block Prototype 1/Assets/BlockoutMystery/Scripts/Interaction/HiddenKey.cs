using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>The key inside a statuette. Clicking it in the inspect view puts it in the inventory.</summary>
    public class HiddenKey : MonoBehaviour, IInspectClickable
    {
        public void OnInspectClick()
        {
            Inventory.Instance.Add(ItemId.Key);
            gameObject.SetActive(false);
            Notebook.Instance.RecordEvidence(CaseFile.KeyEvidence, announce: false);
            UIRoot.Toast("You found a key! It's in your inventory, and noted in your journal.");
        }
    }
}
