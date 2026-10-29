using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>The items the player carries (key, antidote). The inventory bar listens to <see cref="Changed"/>.</summary>
    [DefaultExecutionOrder(-90)]
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        readonly List<ItemId> items = new List<ItemId>();

        public IReadOnlyList<ItemId> Items => items;
        public event Action Changed;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool Has(ItemId id) => items.Contains(id);

        public void Add(ItemId id)
        {
            if (items.Contains(id)) return;
            items.Add(id);
            Changed?.Invoke();
        }

        public bool Remove(ItemId id)
        {
            if (!items.Remove(id)) return false;
            Changed?.Invoke();
            return true;
        }

        public static string NameOf(ItemId id) => id == ItemId.Key ? "Key" : "Antidote";
    }
}
