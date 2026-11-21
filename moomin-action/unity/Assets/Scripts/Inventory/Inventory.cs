using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 플레이어 인벤토리: 30칸 슬롯, 겹치기(스택), 금화, 장착 무기, 퀵슬롯 4칸(1~4 키), 사용/장착/버리기, 저장·불러오기.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [Serializable] public class Slot { public string id; public int count; public bool Empty => string.IsNullOrEmpty(id) || count <= 0; public ItemDef Def => ItemDatabase.Get(id); }
        [Serializable] class Save { public int gold; public List<Slot> slots = new(); public string[] quick = new string[4]; public string weapon; }

        public const int Capacity = 30, QuickCount = 4;
        public Slot[] slots = new Slot[Capacity]; public string[] quick = new string[QuickCount]; public string equippedWeapon = "sword_spark"; public int gold;
        public event Action Changed; public event Action<ItemDef, int> Picked; public event Action<string> Message;
        const string PrefKey = "moomin.inventory";
        CharacterStats health; Stamina stamina; SkillExecutor combat; SwordGlow glow;

        void Awake()
        {
            for (int i = 0; i < Capacity; i++) slots[i] = new Slot();
            health = GetComponent<CharacterStats>(); stamina = GetComponent<Stamina>(); combat = GetComponent<SkillExecutor>();
            if (!Load()) NewGame();
        }
        void Start() { glow = GetComponentInChildren<SwordGlow>(); ApplyWeapon(); }
        void OnApplicationQuit() { Persist(); }

        void NewGame()
        {
            gold = 120; Add("sword_spark", 1, silent: true); Add("sword_frost", 1, silent: true); Add("potion_red", 3, silent: true); Add("potion_blue", 2, silent: true); Add("bread", 2, silent: true); Add("key_old", 1, silent: true);
            quick[0] = "potion_red"; quick[1] = "potion_blue"; quick[2] = "bread"; equippedWeapon = "sword_spark";
        }

        public int Count(string id) { int n = 0; foreach (var s in slots) if (!s.Empty && s.id == id) n += s.count; return n; }
        public bool Contains(string id) => Count(id) > 0;

        /// <summary>아이템 추가. 넣지 못한 개수를 반환(가방이 가득 찬 경우).</summary>
        public int Add(string id, int amount = 1, bool silent = false)
        {
            var d = ItemDatabase.Get(id); if (d == null || amount <= 0) return amount; int left = amount;
            if (d.Stackable) foreach (var s in slots) { if (left <= 0) break; if (!s.Empty && s.id == id && s.count < d.maxStack) { int n = Mathf.Min(left, d.maxStack - s.count); s.count += n; left -= n; } }
            foreach (var s in slots) { if (left <= 0) break; if (s.Empty) { int n = d.Stackable ? Mathf.Min(left, d.maxStack) : 1; s.id = id; s.count = n; left -= n; } }
            int added = amount - left; if (added > 0 && !silent) Picked?.Invoke(d, added); if (left > 0) Message?.Invoke("가방이 가득 찼다");
            Changed?.Invoke(); return left;
        }

        public void AddGold(int g) { gold += g; Changed?.Invoke(); Picked?.Invoke(null, g); }

        public bool Remove(string id, int amount = 1)
        {
            if (Count(id) < amount) return false; int left = amount;
            for (int i = Capacity - 1; i >= 0 && left > 0; i--) { var s = slots[i]; if (s.Empty || s.id != id) continue; int n = Mathf.Min(left, s.count); s.count -= n; left -= n; if (s.count <= 0) { s.id = null; s.count = 0; } }
            for (int q = 0; q < QuickCount; q++) if (quick[q] == id && Count(id) == 0) { /* 퀵슬롯은 지정만 유지 — 수량 0이면 흐리게 표시 */ }
            Changed?.Invoke(); return true;
        }

        /// <summary>슬롯 사용: 소모품은 효과 적용, 무기는 장착. 성공 여부 반환.</summary>
        public bool UseSlot(int index) { var s = slots[index]; return s.Empty ? false : UseItem(s.id); }

        public bool UseItem(string id)
        {
            var d = ItemDatabase.Get(id); if (d == null || !Contains(id)) return false;
            switch (d.type)
            {
                case ItemType.Weapon: Equip(id); return true;
                case ItemType.Consumable:
                    if (d.heal > 0 && health != null) { if (health.currentHealth >= health.maxHealth && d.stamina <= 0) { Message?.Invoke("체력이 가득 찼다"); return false; } health.Heal(d.heal); }
                    if (d.stamina > 0 && stamina != null) stamina.Restore(d.stamina);
                    Remove(id, 1); Message?.Invoke(d.name + " 사용"); return true;
                default: Message?.Invoke("지금은 쓸 수 없다"); return false;
            }
        }

        public void Equip(string weaponId)
        {
            var d = ItemDatabase.Get(weaponId); if (d == null || d.type != ItemType.Weapon || !Contains(weaponId)) return;
            equippedWeapon = weaponId; ApplyWeapon(); Message?.Invoke(d.name + " 장착"); Changed?.Invoke();
        }

        void ApplyWeapon()
        {
            var d = ItemDatabase.Get(equippedWeapon); if (d == null) return;
            if (combat == null) combat = GetComponent<SkillExecutor>(); if (glow == null) glow = GetComponentInChildren<SwordGlow>();
            if (combat != null) combat.weaponMultiplier = d.damageMultiplier; if (glow != null) glow.hueOverride = d.hue;
        }

        public bool Drop(int index)
        {
            var s = slots[index]; if (s.Empty) return false; var d = s.Def;
            if (d.type == ItemType.Weapon && s.id == equippedWeapon) { Message?.Invoke("장착 중인 무기는 버릴 수 없다"); return false; }
            if (d.type == ItemType.Key) { Message?.Invoke("중요한 물건이라 버릴 수 없다"); return false; }
            ItemPickup.Spawn(s.id, 1, transform.position + transform.forward * 1.2f + Vector3.up * 0.5f, 0.8f); Remove(s.id, 1); Message?.Invoke(d.name + "을(를) 버렸다"); return true;
        }

        public void AssignQuick(int quickIndex, string id) { var d = ItemDatabase.Get(id); if (d == null || d.type != ItemType.Consumable) { Message?.Invoke("소모품만 퀵슬롯에 둘 수 있다"); return; } for (int i = 0; i < QuickCount; i++) if (quick[i] == id) quick[i] = null; quick[quickIndex] = id; Changed?.Invoke(); }
        public void UseQuick(int i) { if (i < 0 || i >= QuickCount || string.IsNullOrEmpty(quick[i])) return; if (!Contains(quick[i])) { Message?.Invoke("남은 것이 없다"); return; } UseItem(quick[i]); }
        public void Sort() { var list = new List<Slot>(); foreach (var s in slots) if (!s.Empty) list.Add(new Slot { id = s.id, count = s.count }); list.Sort((a, b) => { var da = a.Def; var db = b.Def; int t = da.type.CompareTo(db.type); return t != 0 ? t : string.CompareOrdinal(a.id, b.id); }); for (int i = 0; i < Capacity; i++) slots[i] = i < list.Count ? list[i] : new Slot(); Changed?.Invoke(); }

        // ── 저장/불러오기
        public void Persist() { var sv = new Save { gold = gold, quick = quick, weapon = equippedWeapon }; foreach (var s in slots) sv.slots.Add(new Slot { id = s.id, count = s.count }); PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(sv)); PlayerPrefs.Save(); }
        bool Load()
        {
            try { var j = PlayerPrefs.GetString(PrefKey, ""); if (string.IsNullOrEmpty(j)) return false; var sv = JsonUtility.FromJson<Save>(j); gold = sv.gold; equippedWeapon = sv.weapon ?? "sword_spark"; if (sv.quick != null) for (int i = 0; i < QuickCount && i < sv.quick.Length; i++) quick[i] = sv.quick[i]; for (int i = 0; i < Capacity && i < sv.slots.Count; i++) { slots[i].id = sv.slots[i].id; slots[i].count = sv.slots[i].count; if (ItemDatabase.Get(slots[i].id) == null) { slots[i].id = null; slots[i].count = 0; } } return true; } catch { return false; }
        }
        public static void ClearSave() { PlayerPrefs.DeleteKey(PrefKey); }
    }
}
