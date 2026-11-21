using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    public enum ItemType { Consumable, Weapon, Material, Key }
    public enum IconKind { Potion, Bread, Dust, Shard, Feather, Sword, Key }

    /// <summary>아이템 정의(데이터). 새 아이템은 ItemDatabase 목록에 한 줄 추가하면 됨.</summary>
    public class ItemDef
    {
        public string id, name, description; public ItemType type; public IconKind icon; public Color color; public int maxStack = 99, value = 10;
        public float heal, stamina;                 // 소모품 효과
        public float damageMultiplier = 1f; public float hue = -1f;   // 무기: 공격력 배율, 칼날 색(-1 = 무지개)
        public bool Stackable => type != ItemType.Weapon && type != ItemType.Key;
        public string TypeLabel => type switch { ItemType.Consumable => "소모품", ItemType.Weapon => "무기", ItemType.Material => "재료", _ => "중요 물품" };
    }

    public static class ItemDatabase
    {
        static readonly Dictionary<string, ItemDef> map = new(); static readonly List<ItemDef> all = new();
        static void Add(ItemDef d) { map[d.id] = d; all.Add(d); }
        public static ItemDef Get(string id) => id != null && map.TryGetValue(id, out var d) ? d : null;
        public static IReadOnlyList<ItemDef> All => all;

        static ItemDatabase()
        {
            Add(new ItemDef { id = "potion_red", name = "붉은 약병", type = ItemType.Consumable, icon = IconKind.Potion, color = new Color(0.85f, 0.15f, 0.2f), heal = 50, maxStack = 20, value = 30, description = "따뜻한 열매즙을 졸여 만든 약. 마시면 체력이 크게 회복된다." });
            Add(new ItemDef { id = "potion_blue", name = "푸른 바람병", type = ItemType.Consumable, icon = IconKind.Potion, color = new Color(0.2f, 0.55f, 0.95f), stamina = 100, maxStack = 20, value = 25, description = "마시면 숨이 트이고 몸이 가벼워져 스태미나가 가득 찬다." });
            Add(new ItemDef { id = "bread", name = "달콤한 빵", type = ItemType.Consumable, icon = IconKind.Bread, color = new Color(0.86f, 0.62f, 0.3f), heal = 20, maxStack = 30, value = 8, description = "갓 구운 빵. 폭신하고 달콤해서 기운이 조금 난다." });
            Add(new ItemDef { id = "dust_fire", name = "불꽃 가루", type = ItemType.Material, icon = IconKind.Dust, color = new Color(1f, 0.55f, 0.1f), value = 12, description = "스파크 검에서 튀어 나온 불꽃이 굳은 가루. 대장장이가 좋아한다." });
            Add(new ItemDef { id = "shard_star", name = "별조각", type = ItemType.Material, icon = IconKind.Shard, color = new Color(1f, 0.9f, 0.4f), value = 40, description = "밤하늘에서 떨어진 작은 조각. 검을 담금질하는 데 쓰인다." });
            Add(new ItemDef { id = "feather_silver", name = "은빛 깃털", type = ItemType.Material, icon = IconKind.Feather, color = new Color(0.78f, 0.84f, 0.92f), value = 55, description = "달빛 아래서만 보이는 희귀한 깃털." });
            Add(new ItemDef { id = "key_old", name = "낡은 열쇠", type = ItemType.Key, icon = IconKind.Key, color = new Color(0.75f, 0.6f, 0.25f), maxStack = 1, value = 0, description = "어느 문을 여는 열쇠인지는 아무도 기억하지 못한다." });
            Add(new ItemDef { id = "sword_spark", name = "스타 스파크 검", type = ItemType.Weapon, icon = IconKind.Sword, color = new Color(0.55f, 0.6f, 0.7f), damageMultiplier = 1f, hue = -1f, maxStack = 1, value = 100, description = "무지개빛 날이 흐르는 검. 휘두를 때마다 불꽃이 튄다." });
            Add(new ItemDef { id = "sword_frost", name = "서리 스파크 검", type = ItemType.Weapon, icon = IconKind.Sword, color = new Color(0.4f, 0.85f, 1f), damageMultiplier = 1.25f, hue = 0.52f, maxStack = 1, value = 260, description = "차가운 푸른 불꽃이 어린 검. 공격력 +25%." });
            Add(new ItemDef { id = "sword_ember", name = "불씨 스파크 검", type = ItemType.Weapon, icon = IconKind.Sword, color = new Color(1f, 0.45f, 0.1f), damageMultiplier = 1.5f, hue = 0.05f, maxStack = 1, value = 520, description = "꺼지지 않는 불씨가 깃든 검. 공격력 +50%." });
        }
    }
}
