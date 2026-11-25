using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    public enum ItemType { Consumable, Weapon, Material, Key, Armor, Artifact }
    public enum IconKind { Potion, Bread, Dust, Shard, Feather, Sword, Key, Headband, Artifact, Peanut }

    /// <summary>아이템 정의(데이터). 새 아이템은 ItemDatabase 목록에 한 줄 추가하면 됨.</summary>
    public class ItemDef
    {
        public string id, name, description; public ItemType type; public IconKind icon; public Color color; public int maxStack = 99, value = 10;
        public float heal, stamina;                 // 소모품 효과
        public float damageMultiplier = 1f; public float hue = -1f;   // 무기: 공격력 배율, 칼날 색(-1 = 무지개)
        [Tooltip("무기 묶음(WeaponKits: spark · shield · dual) — 스킬·방어·고유 효과")] public string kit = "";
        [Header("장비")] public EquipSlot slot = EquipSlot.None; [Tooltip("등급 S · A · B · C")] public string grade = ""; public EquipStats stats;
        [Tooltip("몸에 붙는 3D 모양(Resources 경로, 비우면 없음) · 붙는 뼈. 여러 부품이면 \"경로@뼈|경로@뼈\" (예: 로브 몸통@hips|두건@head)")] public string model; public string attachBone = "head";
        public bool Stackable => type != ItemType.Weapon && type != ItemType.Key && type != ItemType.Armor && type != ItemType.Artifact;
        public bool Equippable => type == ItemType.Weapon || type == ItemType.Armor || type == ItemType.Artifact;
        public EquipSlot Slot => type == ItemType.Weapon ? EquipSlot.Weapon : type == ItemType.Artifact ? EquipSlot.Artifact : slot;
        public string TypeLabel => type switch
        {
            ItemType.Consumable => "소모품", ItemType.Weapon => "무기", ItemType.Material => "재료", ItemType.Armor => "방어구 · " + EquipmentMath.SlotName(slot), ItemType.Artifact => "마법 아티팩트", _ => "중요 물품",
        };
        /// <summary>등급 색(S 하늘 · A 초록 · B 파랑 · C 회색).</summary>
        public static Color GradeColor(string g) => g switch { "S" => new Color(0.45f, 0.95f, 1f), "A" => new Color(0.55f, 0.95f, 0.45f), "B" => new Color(0.45f, 0.65f, 1f), _ => new Color(0.75f, 0.75f, 0.75f) };
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
            Add(new ItemDef { id = "peanut_bread", name = "땅콩빵", type = ItemType.Material, icon = IconKind.Peanut, color = new Color(0.86f, 0.66f, 0.38f), maxStack = 10, value = 60,
                description = "땅콩빵 조각 9개를 꾹꾹 뭉쳐 구운 동그란 땅콩빵. 고소한 냄새가 솔솔." });
            Add(new ItemDef { id = "honey", name = "꿀", type = ItemType.Material, icon = IconKind.Potion, color = new Color(1f, 0.75f, 0.2f), maxStack = 10, value = 40,
                description = "금빛으로 반짝이는 끈적한 꿀. 어디선가 퀘스트로 얻을 수 있다." });
            Add(new ItemDef { id = "honey_peanut_bread", name = "꿀바른 땅콩빵", type = ItemType.Key, icon = IconKind.Peanut, color = new Color(1f, 0.78f, 0.3f), maxStack = 1, value = 0,
                description = "땅콩빵에 꿀을 듬뿍 바른 특별한 빵. 성문 지기 맨두가 가장 좋아한다는 소문이 있다맨두." });
            Add(new ItemDef { id = "royal_pearl", name = "임금님의 무지개 진주", type = ItemType.Key, icon = IconKind.Shard, color = new Color(0.9f, 0.82f, 1f), maxStack = 1, value = 0,
                description = "용궁의 고래고래 임금님이 '친구가 된 기념'이라며 준 진주. 빛에 비추면 무지개가 일렁인다. 어부 철썩이에게 보여 주자." });
            Add(new ItemDef { id = "key_old", name = "낡은 열쇠", type = ItemType.Key, icon = IconKind.Key, color = new Color(0.75f, 0.6f, 0.25f), maxStack = 1, value = 0, description = "어느 문을 여는 열쇠인지는 아무도 기억하지 못한다." });
            Add(new ItemDef { id = "sword_spark", name = "스타 스파크 검", type = ItemType.Weapon, grade = "B", icon = IconKind.Sword, color = new Color(0.55f, 0.6f, 0.7f), damageMultiplier = 1f, hue = -1f, maxStack = 1, value = 100, kit = "spark", description = "무지개빛 날이 흐르는 검. 휘두를 때마다 불꽃이 튄다." });
            Add(new ItemDef { id = "sword_frost", name = "서리 스파크 검", type = ItemType.Weapon, grade = "A", icon = IconKind.Sword, color = new Color(0.4f, 0.85f, 1f), damageMultiplier = 1.25f, hue = 0.52f, maxStack = 1, value = 260, kit = "spark", description = "차가운 푸른 불꽃이 어린 검. 공격력 +25%." });
            Add(new ItemDef { id = "pillow_shield", name = "베개 방패", type = ItemType.Weapon, grade = "A", icon = IconKind.Peanut, color = new Color(0.95f, 0.92f, 0.84f), damageMultiplier = 0.95f, hue = -1f, maxStack = 1, value = 300, kit = "shield",
                              stats = new EquipStats { health = 20 }, model = "Weapons/PillowShield@arm.L",
                              description = "땅콩빵을 꿰매 만든 푹신한 방패. 방어 위력(14+5)이 높아 공격을 잘 막고, 합에서 이기면 포근한 보호막이 생긴다." });
            Add(new ItemDef { id = "spin_blades", name = "회전 쌍검", type = ItemType.Weapon, grade = "S", icon = IconKind.Sword, color = new Color(0.6f, 0.75f, 1f), damageMultiplier = 1.2f, hue = -1f, maxStack = 1, value = 420, kit = "dual",
                              stats = new EquipStats { damageReduction = 0.06f }, model = "Weapons/SpinBlade_R@hand.R|Weapons/SpinBlade_L@arm.L",
                              description = "손잡이 끝에서 두 날이 바람개비처럼 도는 쌍검. 공격·방어 모두 높고, 싸울수록 결투 고조로 더 빨리 돈다." });
            Add(new ItemDef { id = "laser_blader", name = "레이저 블레이더", type = ItemType.Weapon, grade = "S", icon = IconKind.Sword, color = new Color(0.35f, 0.75f, 1f), damageMultiplier = 1.3f, hue = -1f, maxStack = 1, value = 680, kit = "laser",
                              stats = new EquipStats { damageReduction = 0.08f, finalPower = 1 }, model = "Weapons/LaserBlader@hand.R",
                              description = "가운데 고리를 양손으로 쥐고 돌리는 푸른 광선검. 회전 쌍검보다 넓게 베고, 지나간 자리에 푸른 광선 잔상과 빛 가루가 남는다." });
            Add(new ItemDef { id = "sword_ember", name = "불씨 스파크 검", type = ItemType.Weapon, grade = "S", icon = IconKind.Sword, color = new Color(1f, 0.45f, 0.1f), damageMultiplier = 1.5f, hue = 0.05f, maxStack = 1, value = 520, description = "꺼지지 않는 불씨가 깃든 검. 공격력 +50%." });
            // ── 방어구·장신구(머리·가슴·다리·발) · 마법 아티팩트 — slot·grade·stats 만 정하면 장비 화면·능력치에 바로 들어감
            Add(new ItemDef { id = "headband_blue", name = "파란 머리띠", type = ItemType.Armor, slot = EquipSlot.Head, grade = "B", icon = IconKind.Headband, color = new Color(0.62f, 0.82f, 0.95f), maxStack = 1, value = 80,
                              stats = new EquipStats { finalPower = 1 }, model = "Equipment/Headband_Blue", attachBone = "head",
                              description = "보송보송한 하늘색 머리띠. 쓰고 있으면 마음이 차분해져 코인을 한 번 더 힘껏 던진다." });
            // ── 집에서 찾은 장비(blender/build_home_gear.py — 3D 모양·아이콘)
            Add(new ItemDef { id = "robe_blanket", name = "이불 로브", type = ItemType.Armor, slot = EquipSlot.Chest, grade = "A", icon = IconKind.Artifact, color = new Color(0.66f, 0.6f, 0.85f), maxStack = 1, value = 150,
                              stats = new EquipStats { health = 25, damageReduction = 0.08f }, model = "Equipment/Robe_Body@hips|Equipment/Robe_Hood@head",
                              description = "누빔 이불을 통째로 두른 로브. 두건까지 폭 덮으면 웬만한 공격은 푹신하게 받아낸다." });
            Add(new ItemDef { id = "shield_tissue", name = "각티슈 방패", type = ItemType.Artifact, grade = "B", icon = IconKind.Artifact, color = new Color(0.95f, 0.6f, 0.68f), maxStack = 1, value = 90,
                              stats = new EquipStats { guard = 0.15f }, model = "Equipment/TissueShield@arm.L",
                              description = "왼팔에 끼우는 꽃무늬 각티슈 상자. 막을 때 피해를 더 줄여 준다. 급할 땐 한 장 뽑아 쓴다." });
            Add(new ItemDef { id = "cap_sleep", name = "솜털 밀집모자", type = ItemType.Armor, slot = EquipSlot.Head, grade = "C", icon = IconKind.Artifact, color = new Color(0.86f, 0.72f, 0.42f), maxStack = 1, value = 60,
                              stats = new EquipStats { evadePower = 2 }, model = "Equipment/StrawHat@head",
                              description = "꼭지 둘레에 솜털 뭉치를 빙 둘러 단 밀짚모자. 가볍고 바람이 잘 통해 구를 때 몸이 날렵해진다." });
            Add(new ItemDef { id = "pants_pajama", name = "수면 바지", type = ItemType.Armor, slot = EquipSlot.Legs, grade = "B", icon = IconKind.Artifact, color = new Color(0.42f, 0.62f, 0.9f), maxStack = 1, value = 80,
                              stats = new EquipStats { health = 15, evadePower = 1 }, model = "Equipment/PajamaShorts@hips",
                              description = "하늘색 체크무늬 잠옷 반바지. 허리 고무줄이 넉넉해서 움직이기 편하다." });
            Add(new ItemDef { id = "slippers_fuzzy", name = "토끼 털 슬리퍼", type = ItemType.Armor, slot = EquipSlot.Feet, grade = "B", icon = IconKind.Artifact, color = new Color(0.97f, 0.96f, 0.95f), maxStack = 1, value = 80,
                              stats = new EquipStats { moveSpeed = 0.1f, evadePower = 1 }, model = "Equipment/Slipper_L@leg.L|Equipment/Slipper_R@leg.R",
                              description = "긴 귀가 쫑긋한 하얀 토끼털 실내화. 깡총거리듯 가볍게 빨리 걷게 된다." });
            // ── 자원
            Add(new ItemDef { id = "peanut_piece", name = "땅콩빵 조각", type = ItemType.Material, icon = IconKind.Peanut, color = new Color(0.95f, 0.88f, 0.66f), maxStack = 99, value = 6,
                              description = "땅콩빵을 부수면 나오는 말랑한 조각. 산꼭대기에 많다. 하프물범이 아주 좋아한다(가까이에서 F 로 건네 길들이기)." });
        }
    }
}
