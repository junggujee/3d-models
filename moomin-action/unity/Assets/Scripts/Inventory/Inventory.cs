using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 플레이어 인벤토리: 30칸 슬롯, 겹치기(스택), 금화, 퀵슬롯 4칸(1~4 키), 사용/장착/버리기, 저장·불러오기.
    /// 장비: 무기 1(기본 스파크 검, 빼기 없음) · 방어구 4(머리·가슴·다리·발) · 마법 아티팩트 3. 장비는 가방 칸에 그대로 있고 '장착' 표시만(버리기 불가).
    /// 장비 묶음(프리셋) 1~3 을 바꿔 낄 수 있음. 장비 능력치(EquipStats) 합 → CharacterStats(공격력·코인 최종 위력·최대 체력·피해 감소), 3D 모양 → EquipmentVisuals.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [Serializable] public class Slot { public string id; public int count; public bool Empty => string.IsNullOrEmpty(id) || count <= 0; public ItemDef Def => ItemDatabase.Get(id); }
        /// <summary>저장 슬롯에 들어가는 가방 상태(SaveSystem).</summary>
        [Serializable] public class State
        {
            public int gold; public List<Slot> slots = new(); public string[] quick = new string[4]; public string weapon;
            public string[] armor = new string[ArmorCount], artifacts = new string[ArtifactCount]; public int preset; public List<Loadout> presets = new();
        }
        /// <summary>장비 묶음 하나(프리셋 1~3).</summary>
        [Serializable] public class Loadout
        {
            public string weapon; public string[] armor = new string[ArmorCount], artifacts = new string[ArtifactCount]; public bool used;
            public Loadout Clone() => new Loadout { weapon = weapon, armor = (string[])armor.Clone(), artifacts = (string[])artifacts.Clone(), used = used };
        }
        public const int ArmorCount = 4, ArtifactCount = 3, PresetCount = 3;
        public static readonly EquipSlot[] ArmorSlots = { EquipSlot.Head, EquipSlot.Chest, EquipSlot.Legs, EquipSlot.Feet };

        public const int Capacity = 30, QuickCount = 4;
        public Slot[] slots = new Slot[Capacity]; public string[] quick = new string[QuickCount]; public string equippedWeapon = "sword_spark"; public int gold;
        public string[] armor = new string[ArmorCount], artifacts = new string[ArtifactCount]; public int preset; public Loadout[] presets = new Loadout[PresetCount];
        public event Action Changed; public event Action<ItemDef, int> Picked; public event Action<string> Message;
        const string PrefKey = "moomin.inventory";
        CharacterStats health; Stamina stamina; SkillExecutor combat; SwordGlow glow;

        void Awake()
        {
            for (int i = 0; i < Capacity; i++) slots[i] = new Slot();
            health = GetComponent<CharacterStats>(); stamina = GetComponent<Stamina>(); combat = GetComponent<SkillExecutor>();
            if (SaveSystem.Pending != null && SaveSystem.Pending.inventory != null && SaveSystem.Pending.inventory.slots != null && SaveSystem.Pending.inventory.slots.Count > 0) Import(SaveSystem.Pending.inventory); else NewGame();   // 불러오기면 저장된 가방, 아니면 새 게임
        }
        void Start() { glow = GetComponentInChildren<SwordGlow>(); ApplyWeapon(); ApplyEquipment(initial: true); }

        void NewGame()
        {
            gold = 120; Add("sword_spark", 1, silent: true); Add("sword_frost", 1, silent: true); Add("pillow_shield", 1, silent: true); Add("spin_blades", 1, silent: true); Add("laser_blader", 1, silent: true); Add("potion_red", 3, silent: true); Add("potion_blue", 2, silent: true); Add("bread", 2, silent: true); Add("key_old", 1, silent: true);
            Add("headband_blue", 1, silent: true);   // 첫 장신구(장비 화면에서 머리에 장착)
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
                case ItemType.Armor: case ItemType.Artifact:
                    if (IsEquipped(id)) { UnequipItem(id); return true; }
                    return EquipTo(id, d.Slot, -1);
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
            equippedWeapon = weaponId; ApplyWeapon(); ApplyEquipment(); Message?.Invoke(d.name + " 장착"); Changed?.Invoke();
        }

        void ApplyWeapon()
        {
            var d = ItemDatabase.Get(equippedWeapon); if (d == null) return;
            if (combat == null) combat = GetComponent<SkillExecutor>(); if (glow == null) glow = GetComponentInChildren<SwordGlow>();
            if (combat != null) combat.weaponMultiplier = d.damageMultiplier; if (glow != null) glow.hueOverride = d.hue;
            if (TryGetComponent<CombatManager>(out var cm)) cm.ApplyKit(WeaponKits.Get(string.IsNullOrEmpty(d.kit) ? WeaponKits.Spark : d.kit));   // 무기마다 스킬·방어·고유 효과
        }

        public bool Drop(int index)
        {
            var s = slots[index]; if (s.Empty) return false; var d = s.Def;
            if (d.type == ItemType.Weapon && s.id == equippedWeapon) { Message?.Invoke("장착 중인 무기는 버릴 수 없다"); return false; }
            if (d.Equippable && IsEquipped(s.id)) { Message?.Invoke("장착 중인 장비는 버릴 수 없다"); return false; }
            if (d.type == ItemType.Key) { Message?.Invoke("중요한 물건이라 버릴 수 없다"); return false; }
            ItemPickup.Spawn(s.id, 1, transform.position + transform.forward * 1.2f + Vector3.up * 0.5f, 0.8f); Remove(s.id, 1); Message?.Invoke(PetSeal.Josa(d.name, "을", "를") + " 버렸다"); return true;
        }

        public void AssignQuick(int quickIndex, string id) { var d = ItemDatabase.Get(id); if (d == null || d.type != ItemType.Consumable) { Message?.Invoke("소모품만 퀵슬롯에 둘 수 있다"); return; } for (int i = 0; i < QuickCount; i++) if (quick[i] == id) quick[i] = null; quick[quickIndex] = id; Changed?.Invoke(); }
        public void UseQuick(int i) { if (i < 0 || i >= QuickCount || string.IsNullOrEmpty(quick[i])) return; if (!Contains(quick[i])) { Message?.Invoke("남은 것이 없다"); return; } UseItem(quick[i]); }
        public void Sort() { var list = new List<Slot>(); foreach (var s in slots) if (!s.Empty) list.Add(new Slot { id = s.id, count = s.count }); list.Sort((a, b) => { var da = a.Def; var db = b.Def; int t = da.type.CompareTo(db.type); return t != 0 ? t : string.CompareOrdinal(a.id, b.id); }); for (int i = 0; i < Capacity; i++) slots[i] = i < list.Count ? list[i] : new Slot(); Changed?.Invoke(); }

        // ── 장비 ─────────────────────────────────
        int ArmorIndex(EquipSlot s) => Array.IndexOf(ArmorSlots, s);
        /// <summary>그 칸에 낀 아이템 id(없으면 null). 아티팩트는 index 0~2.</summary>
        public string Equipped(EquipSlot s, int index = 0) => s switch
        {
            EquipSlot.Weapon => equippedWeapon, EquipSlot.Artifact => index >= 0 && index < ArtifactCount ? artifacts[index] : null,
            _ => ArmorIndex(s) >= 0 ? armor[ArmorIndex(s)] : null,
        };
        public bool IsEquipped(string id)
        {
            if (string.IsNullOrEmpty(id)) return false; if (id == equippedWeapon) return true;
            foreach (var a in armor) if (a == id) return true; foreach (var a in artifacts) if (a == id) return true; return false;
        }

        /// <summary>장착. 칸이 아이템과 맞아야 함. 아티팩트 index -1 = 빈 칸 먼저(없으면 첫 칸 교체). 같은 아티팩트가 다른 칸에 있으면 옮김.</summary>
        public bool EquipTo(string id, EquipSlot slot, int index)
        {
            var d = ItemDatabase.Get(id); if (d == null || !d.Equippable || !Contains(id)) return false;
            if (d.Slot != slot) { Message?.Invoke($"{d.name}은(는) {EquipmentMath.SlotName(d.Slot)} 장비다"); return false; }
            if (slot == EquipSlot.Weapon) { Equip(id); return true; }
            if (slot == EquipSlot.Artifact)
            {
                for (int i = 0; i < ArtifactCount; i++) if (artifacts[i] == id) artifacts[i] = null;
                if (index < 0) { index = Array.IndexOf(artifacts, null); if (index < 0) index = 0; }
                artifacts[index] = id;
            }
            else armor[ArmorIndex(slot)] = id;
            ApplyEquipment(); Message?.Invoke(d.name + " 장착"); Changed?.Invoke(); return true;
        }

        /// <summary>칸 비우기(무기는 항상 하나는 끼고 있어 뺄 수 없음).</summary>
        public void Unequip(EquipSlot slot, int index = 0)
        {
            if (slot == EquipSlot.Weapon) { Message?.Invoke("무기는 뺄 수 없다 — 다른 검으로 바꿔 끼자"); return; }
            string id = Equipped(slot, index); if (string.IsNullOrEmpty(id)) return;
            if (slot == EquipSlot.Artifact) artifacts[index] = null; else armor[ArmorIndex(slot)] = null;
            var d = ItemDatabase.Get(id); ApplyEquipment(); Message?.Invoke((d != null ? d.name : "장비") + " 해제"); Changed?.Invoke();
        }
        public void UnequipItem(string id)
        {
            for (int i = 0; i < ArmorCount; i++) if (armor[i] == id) { Unequip(ArmorSlots[i]); return; }
            for (int i = 0; i < ArtifactCount; i++) if (artifacts[i] == id) { Unequip(EquipSlot.Artifact, i); return; }
        }

        /// <summary>지금 낀 장비 능력치 합(무기 배율은 따로 — SkillExecutor.weaponMultiplier).</summary>
        public EquipStats TotalStats
        {
            get
            {
                var t = default(EquipStats);
                foreach (var id in EquippedIds()) { var d = ItemDatabase.Get(id); if (d != null) t += d.stats; }
                return t;
            }
        }
        public IEnumerable<string> EquippedIds()
        {
            if (!string.IsNullOrEmpty(equippedWeapon)) yield return equippedWeapon;
            foreach (var a in armor) if (!string.IsNullOrEmpty(a)) yield return a;
            foreach (var a in artifacts) if (!string.IsNullOrEmpty(a)) yield return a;
        }

        /// <summary>
        /// 장비 능력치 → CharacterStats, 3D 모양 → EquipmentVisuals. initial = 시작할 때 한 번(불러온 최대 체력에 장비 체력을 더하기만, 현재 체력은 최대치에 맞춰 자름).
        /// 게임 중 장비를 바꾸면 늘어난 최대 체력만큼 현재 체력도 함께 늘고, 줄면 최대치에 맞춰 자름.
        /// </summary>
        public void ApplyEquipment(bool initial = false)
        {
            // 가방에 없는 장비(버렸거나 예전 저장)는 빼기
            for (int i = 0; i < ArmorCount; i++) if (!string.IsNullOrEmpty(armor[i]) && !Contains(armor[i])) armor[i] = null;
            for (int i = 0; i < ArtifactCount; i++) if (!string.IsNullOrEmpty(artifacts[i]) && !Contains(artifacts[i])) artifacts[i] = null;
            if (health == null) health = GetComponent<CharacterStats>(); if (health == null) return;
            var t = TotalStats; health.equipAttack = t.attack; health.equipFinalPower = t.finalPower; health.equipDamageReduction = Mathf.Clamp(t.damageReduction, 0f, 0.8f);
            health.equipGuard = t.guard; health.equipEvadePower = t.evadePower; health.equipMoveSpeed = t.moveSpeed;
            float delta = t.health - health.equipHealth; health.equipHealth = t.health; health.maxHealth = Mathf.Max(1f, health.maxHealth + delta);
            if (!initial && delta > 0) health.currentHealth += delta;
            health.currentHealth = Mathf.Min(health.currentHealth, health.maxHealth);
            EquipmentVisuals.Sync(this);
        }

        /// <summary>장비 묶음 바꾸기(1~3): 지금 장비를 현재 묶음에 기억하고, 고른 묶음을 낌(처음 쓰는 묶음은 지금 장비를 그대로 복사).</summary>
        public void SwitchPreset(int index)
        {
            if (index < 0 || index >= PresetCount || index == preset) return;
            presets[preset] = Current(); var next = presets[index];
            preset = index;
            if (next != null && next.used)
            {
                if (!string.IsNullOrEmpty(next.weapon) && Contains(next.weapon)) equippedWeapon = next.weapon;
                for (int i = 0; i < ArmorCount; i++) armor[i] = next.armor != null && i < next.armor.Length ? next.armor[i] : null;
                for (int i = 0; i < ArtifactCount; i++) artifacts[i] = next.artifacts != null && i < next.artifacts.Length ? next.artifacts[i] : null;
            }
            ApplyWeapon(); ApplyEquipment(); Message?.Invoke($"장비 묶음 {index + 1}"); Changed?.Invoke();
        }
        Loadout Current() => new Loadout { weapon = equippedWeapon, armor = (string[])armor.Clone(), artifacts = (string[])artifacts.Clone(), used = true };

        // ── 저장/불러오기(저장 슬롯 — SaveSystem 이 Export/Import 를 부름)
        public State Export()
        {
            var sv = new State { gold = gold, quick = (string[])quick.Clone(), weapon = equippedWeapon, armor = (string[])armor.Clone(), artifacts = (string[])artifacts.Clone(), preset = preset };
            foreach (var s in slots) sv.slots.Add(new Slot { id = s.id, count = s.count });
            presets[preset] = Current(); foreach (var p in presets) sv.presets.Add(p != null ? p.Clone() : new Loadout());
            return sv;
        }
        public void Import(State sv)
        {
            if (sv == null) return;
            gold = sv.gold; equippedWeapon = string.IsNullOrEmpty(sv.weapon) ? "sword_spark" : sv.weapon;
            for (int i = 0; i < QuickCount; i++) quick[i] = sv.quick != null && i < sv.quick.Length ? sv.quick[i] : null;
            for (int i = 0; i < Capacity; i++)
            {
                slots[i] = new Slot(); if (sv.slots == null || i >= sv.slots.Count) continue;
                slots[i].id = sv.slots[i].id; slots[i].count = sv.slots[i].count;
                if (ItemDatabase.Get(slots[i].id) == null) { slots[i].id = null; slots[i].count = 0; }
            }
            for (int i = 0; i < ArmorCount; i++) armor[i] = sv.armor != null && i < sv.armor.Length && !string.IsNullOrEmpty(sv.armor[i]) ? sv.armor[i] : null;
            for (int i = 0; i < ArtifactCount; i++) artifacts[i] = sv.artifacts != null && i < sv.artifacts.Length && !string.IsNullOrEmpty(sv.artifacts[i]) ? sv.artifacts[i] : null;
            preset = Mathf.Clamp(sv.preset, 0, PresetCount - 1);
            for (int i = 0; i < PresetCount; i++) presets[i] = sv.presets != null && i < sv.presets.Count && sv.presets[i] != null && sv.presets[i].used ? sv.presets[i].Clone() : null;
            if (glow != null) { ApplyWeapon(); ApplyEquipment(); } Changed?.Invoke();   // 시작 전(Awake)이면 Start 에서 적용
        }
        /// <summary>예전 버전 호환용(이제 가방은 저장 슬롯에 들어감). 아무것도 하지 않음.</summary>
        public void Persist() { }
        public static void ClearSave() { PlayerPrefs.DeleteKey(PrefKey); }
    }
}
