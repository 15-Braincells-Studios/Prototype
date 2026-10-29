using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockoutMystery
{
    /// <summary>Picked-up items floating at the bottom of the screen. Visible while exploring and inspecting.</summary>
    public class InventoryBar : MonoBehaviour
    {
        RectTransform row;
        CanvasGroup group;
        bool subscribed;

        public static InventoryBar Create(RectTransform parent)
        {
            var img = UIKit.Image("Inventory", parent, new Color(0f, 0f, 0f, 0.35f));
            img.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24));
            UIKit.Row(img, 14, 10);
            var fit = img.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bar = img.gameObject.AddComponent<InventoryBar>();
            bar.row = img.rectTransform;
            bar.group = img.gameObject.AddComponent<CanvasGroup>();
            bar.group.alpha = 0f;
            return bar;
        }

        void Update()
        {
            var inventory = Inventory.Instance;
            if (!subscribed && inventory)
            {
                inventory.Changed += Refresh;
                subscribed = true;
                Refresh();
            }

            var flow = GameFlow.Instance;
            bool show = inventory && inventory.Items.Count > 0 && flow && !flow.NotebookOpen &&
                        (flow.Mode == GameMode.Explore || flow.Mode == GameMode.Inspect);
            group.alpha = show ? 1f : 0f;
            group.blocksRaycasts = show;
        }

        void OnDestroy()
        {
            if (subscribed && Inventory.Instance) Inventory.Instance.Changed -= Refresh;
        }

        void Refresh()
        {
            UIKit.Clear(row);
            foreach (var item in Inventory.Instance.Items) InventorySlot.Create(row, item);
        }
    }

    /// <summary>One floating item. The antidote can be dragged onto a plate in the inspect view.</summary>
    public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        ItemId item;
        RectTransform icon;
        RectTransform ghost;
        float phase;

        public static InventorySlot Create(Transform parent, ItemId item)
        {
            var bg = UIKit.Image("Slot " + item, parent, UIKit.PanelLight, true);
            UIKit.Size(bg, 104, 104);
            var slot = bg.gameObject.AddComponent<InventorySlot>();
            slot.item = item;
            slot.phase = (int)item * 1.7f;
            slot.icon = BuildIcon(bg.transform, item);
            slot.icon.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(64, 64));
            var label = UIKit.Label(bg.transform, Inventory.NameOf(item), 18, UIKit.Text, TMPro.TextAlignmentOptions.Center);
            label.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(100, 24));
            return slot;
        }

        /// <summary>A tiny picture of the item made of rectangles.</summary>
        public static RectTransform BuildIcon(Transform parent, ItemId item)
        {
            var root = UIKit.Rect("Icon " + item, parent);
            root.sizeDelta = new Vector2(64, 64);
            if (item == ItemId.Key)
            {
                var gold = new Color(1f, 0.8f, 0.3f);
                Part(root, gold, new Vector2(6, 0), new Vector2(40, 9));
                Part(root, gold, new Vector2(-18, 0), new Vector2(20, 20));
                Part(root, UIKit.PanelLight, new Vector2(-18, 0), new Vector2(8, 8));
                Part(root, gold, new Vector2(20, -9), new Vector2(6, 11));
                Part(root, gold, new Vector2(11, -8), new Vector2(6, 8));
                root.localRotation = Quaternion.Euler(0, 0, 35f);
            }
            else
            {
                Part(root, new Color(0.35f, 0.75f, 1f), new Vector2(0, -8), new Vector2(30, 38));
                Part(root, new Color(0.7f, 0.9f, 1f), new Vector2(0, 17), new Vector2(14, 12));
                Part(root, new Color(0.55f, 0.36f, 0.2f), new Vector2(0, 26), new Vector2(18, 8));
                Part(root, new Color(1f, 1f, 1f, 0.6f), new Vector2(-8, -6), new Vector2(5, 24));
            }
            return root;
        }

        static void Part(RectTransform parent, Color c, Vector2 pos, Vector2 size) =>
            UIKit.Image("Part", parent, c).rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

        void Update()
        {
            icon.anchoredPosition = new Vector2(0f, 10f + Mathf.Sin(Time.unscaledTime * 2.2f + phase) * 5f);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (item != ItemId.Antidote) return; // the key is used on the door with E
            if (!(InspectSystem.Instance.Current is Plate))
            {
                UIRoot.Toast("Inspect a plate first, then drag the antidote onto it.");
                return;
            }
            ghost = BuildIcon(UIRoot.Instance.Root, item);
            ghost.position = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ghost) ghost.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!ghost) return;
            Destroy(ghost.gameObject);
            ghost = null;

            var plate = InspectSystem.Instance.Current as Plate;
            if (plate && InspectSystem.Instance.IsOverCurrent(eventData.position)) plate.ApplyAntidote();
            else UIRoot.Toast("Drop the antidote onto the plate.");
        }
    }
}
