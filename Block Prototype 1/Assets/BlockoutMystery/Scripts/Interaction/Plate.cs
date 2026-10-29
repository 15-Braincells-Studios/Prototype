using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>A plate of food on the kitchen island. Drag the antidote from the inventory onto it in the inspect view.</summary>
    public class Plate : Inspectable
    {
        [SerializeField] HerbColor herbs;
        [Tooltip("Shown once the antidote has been poured over this plate.")]
        [SerializeField] GameObject antidoteVisual;

        public HerbColor Herbs => herbs;

        /// <summary>Just "plate": the herbs have to be told apart by looking, not by reading the prompt.</summary>
        public override string Prompt => "Inspect plate";

        public void Configure(HerbColor herbColor, GameObject antidote)
        {
            herbs = herbColor;
            antidoteVisual = antidote;
        }

        public override string InspectHint
        {
            get
            {
                var flow = GameFlow.Instance;
                if (flow.AntidotePlate == herbs) return "The antidote is on this plate   |   E / Esc  put it back";
                if (!flow.AntidoteUsed && Inventory.Instance.Has(ItemId.Antidote))
                    return "Drag the antidote from your inventory onto this plate (one portion only)   |   E / Esc  put it back";
                return base.InspectHint;
            }
        }

        public bool ApplyAntidote()
        {
            if (!GameFlow.Instance.UseAntidote(herbs)) return false;
            if (antidoteVisual) antidoteVisual.SetActive(true);
            UIRoot.Toast($"You poured the antidote over the plate with {CaseFile.HerbName(herbs)} herbs.");
            InspectSystem.Instance.RefreshHint();
            return true;
        }
    }
}
