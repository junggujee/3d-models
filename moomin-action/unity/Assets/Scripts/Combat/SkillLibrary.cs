using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 기본 스킬 값(코드). 메뉴 Moomin ▸ "스킬 데이터 에셋 만들기" 가 이 값으로 Assets/Data/Skills/*.asset 을 만들고 플레이어·적에 연결함.
    /// 에셋이 연결돼 있으면 에셋 값을, 없으면 여기 값을 씀.
    /// </summary>
    public static class SkillLibrary
    {
        static Dictionary<string, SkillDefinition> map;
        public static SkillDefinition Get(string id) { Ensure(); return id != null && map.TryGetValue(id, out var s) ? s : null; }
        public static IEnumerable<SkillDefinition> All { get { Ensure(); return map.Values; } }

        static SkillDefinition S(string id, string name, int b, int c, int n, float mult, float cd, int sanity, float interval, float hit, float recovery, string[] anims,
                                 float range, float angle, float kb, float windup = 0f, float lunge = 0f, CoinEffect fx = CoinEffect.None, bool unblockable = false, string desc = "")
        {
            var s = ScriptableObject.CreateInstance<SkillDefinition>(); s.name = id;
            s.skillId = id; s.skillName = name; s.basePower = b; s.coinPower = c; s.coinCount = n; s.damageMultiplier = mult; s.cooldown = cd; s.sanityCost = sanity;
            s.attackInterval = interval; s.hitTime = hit; s.recovery = recovery; s.attackAnimations = anims; s.range = range; s.angle = angle; s.knockback = kb;
            s.windup = windup; s.lunge = lunge; s.coinEffect = fx; s.canBeGuarded = !unblockable; s.description = desc; return s;
        }

        static void Ensure()
        {
            if (map != null) return; map = new Dictionary<string, SkillDefinition>();
            void Look(string id, SkillAttribute a, SkillMotif m) { if (map.TryGetValue(id, out var s)) { s.attribute = a; s.motif = m; } }
            // 상태이상 부여량: 앞면(hp,hc) / 뒷면(tp,tc) — 강도/횟수
            void St(string id, StatusType t, int hp, int hc, int tp = 0, int tc = 0)
            {
                if (!map.TryGetValue(id, out var s)) return; var h = new StatusAmount(hp, hc); var l = new StatusAmount(tp, tc);
                switch (t)
                {
                    case StatusType.Bleed: s.bleedOnHeads = h; s.bleedOnTails = l; break; case StatusType.Burn: s.burnOnHeads = h; s.burnOnTails = l; break;
                    case StatusType.Sinking: s.sinkingOnHeads = h; s.sinkingOnTails = l; break; case StatusType.Tremor: s.tremorOnHeads = h; s.tremorOnTails = l; break;
                    case StatusType.PowerUp: s.powerUpOnHeads = h; s.powerUpOnTails = l; break; case StatusType.PowerDown: s.powerDownOnHeads = h; s.powerDownOnTails = l; break;
                    case StatusType.Paralysis: s.paralysisOnHeads = h; s.paralysisOnTails = l; break;
                }
            }
            void Energy(string id, int cost, int gain, bool burst = false) { if (map.TryGetValue(id, out var s)) { s.energyCost = cost; s.energyGain = gain; s.tremorBurstEnabled = burst; } }
            void Add(SkillDefinition s) => map[s.skillId] = s;
            void Area(string id, float radius) { if (map.TryGetValue(id, out var s)) { s.isAreaAttack = true; s.areaRadius = radius; s.angle = 360f; } }
            string[] A1 = { "Attack1" }, A2 = { "Attack2" }, A23 = { "Attack2", "Attack3" }, A123 = { "Attack1", "Attack2", "Attack3" }, A5 = { "Attack1", "Attack2", "Attack1", "Attack2", "Attack3" }, A33 = { "Attack3", "Attack3" };
            // ── 플레이어: 일반 공격(좌클릭 3연타)
            Add(S("BASIC_1", "베기", 8, 3, 1, 1f, 0f, 0, 0.8f, 0.35f, 0.25f, A1, 1.9f, 110, 4f));
            Add(S("BASIC_2", "되베기", 8, 4, 1, 1f, 0f, 0, 0.8f, 0.35f, 0.25f, A2, 1.9f, 110, 5f));
            Add(S("BASIC_3", "이어 베기", 9, 3, 2, 1f, 0f, 0, 0.45f, 0.35f, 0.35f, A23, 2.2f, 140, 7f));
            // ── 플레이어: 스킬(명세 예시: 연속 베기 기본 10, 코인 +3, 3코인, 쿨 5초)
            Add(S("SKILL_001", "연속 베기", 10, 3, 3, 1.0f, 5f, 0, 0.5f, 0.35f, 0.35f, A123, 2.1f, 120, 5f, desc: "세 번 연속으로 벤다. 베기마다 코인을 하나씩 던진다."));
            Add(S("SKILL_002", "불꽃 찌르기", 14, 6, 2, 1.2f, 8f, 8, 0.55f, 0.4f, 0.4f, A33, 2.8f, 70, 10f, lunge: 4f, fx: CoinEffect.StaggerOnAllHeads,
                  desc: "두 번 깊게 찌른다. 둘 다 앞면이면 적이 크게 휘청인다. 정신력 8 이상일 때 사용, 8 소모."));
            Add(S("SKILL_003", "별빛 난무", 14, 5, 5, 1.3f, 20f, 0, 0.42f, 0.33f, 0.5f, A5, 3.2f, 360, 8f, fx: CoinEffect.SanityOnAllHeads,
                  desc: "강화 스킬 — 에너지 60 소비. 주위를 다섯 번 베어 내며 쌓인 진동을 폭발시킨다. 다섯 번 모두 앞면이면 정신력 +10."));
            // 명세 테스트 스킬: 충격파(광역 · 공통 코인). 슬롯에 넣으려면 CombatManager 의 skill1/skill2/ultimate 에 SKILL_004 에셋 연결
            Add(S("SKILL_004", "충격파", 8, 3, 3, 1.0f, 5f, 0, 0.5f, 0.35f, 0.4f, A123, 4f, 360, 6f, desc: "주위에 충격파를 세 번 터뜨린다. 코인은 매번 한 번만, 범위 안의 모든 적에게 같은 위력."));
            // ── 무기: 베개 방패(방어 코인 14+5 · 합 승리 보호막). 클립 타격 프레임: ShieldBash/Swing 10(0.30초) · ShieldSlam 15(0.47초) · ShieldLeap 22(0.70초)
            string[] SB = { "ShieldBash" }, SW = { "ShieldSwing" }, SS = { "ShieldSlam" };
            Add(S("SHIELD_1", "베개 밀치기", 7, 3, 1, 0.95f, 0f, 0, 0.8f, 0.30f, 0.28f, SB, 1.8f, 100, 6f, lunge: 1.5f, desc: "푹신한 방패로 앞을 밀어낸다."));
            Add(S("SHIELD_2", "옆으로 휘두르기", 7, 3, 1, 0.95f, 0f, 0, 0.8f, 0.30f, 0.28f, SW, 2.0f, 150, 5f, desc: "방패를 옆으로 크게 휘둘러 친다."));
            Add(S("SHIELD_3", "푹신 내려찍기", 9, 3, 1, 1.1f, 0f, 0, 0.8f, 0.47f, 0.35f, SS, 2.2f, 140, 7f, desc: "방패를 머리 위로 들었다가 힘껏 내려찍는다."));
            Add(S("SHIELD_Q", "솜털 돌진", 10, 4, 2, 1.0f, 6f, 0, 0.5f, 0.30f, 0.35f, new[] { "ShieldBash", "ShieldBash" }, 2.2f, 90, 8f, lunge: 6f, desc: "방패를 앞세우고 두 번 들이받는다."));
            Add(S("SHIELD_E", "포근한 충격파", 9, 3, 2, 0.9f, 8f, 0, 0.7f, 0.47f, 0.4f, new[] { "ShieldSlam", "ShieldSlam" }, 3.5f, 360, 6f, desc: "땅을 두 번 내려찍어 주위에 솜털 충격파를 퍼뜨린다. 적중하면 진동."));
            Add(S("SHIELD_R", "거대 베개 낙하", 15, 5, 3, 1.25f, 20f, 0, 0.6f, 0.30f, 0.6f, new[] { "ShieldBash", "ShieldSwing", "ShieldLeap" }, 2.6f, 150, 9f,
                  desc: "강화 스킬 — 에너지 60 소비. 밀치고 휘두른 뒤 높이 뛰어올라 방패째 떨어진다."));
            if (map.TryGetValue("SHIELD_R", out var shr)) shr.hitTimes = new[] { 0.30f, 0.30f, 0.70f };
            // ── 무기: 회전 쌍검(공격·방어 모두 높음 · 결투 고조). 클립 타격 프레임: Dual* 10(0.30초) · DualFinale 26(0.83초)
            Add(S("DUAL_1", "회전 베기", 8, 4, 1, 1f, 0f, 0, 0.75f, 0.30f, 0.25f, new[] { "DualSlash1" }, 2.0f, 120, 4f, desc: "오른쪽 회전 날로 가로 벤다."));
            Add(S("DUAL_2", "역회전 베기", 8, 4, 1, 1f, 0f, 0, 0.75f, 0.30f, 0.25f, new[] { "DualSlash2" }, 2.0f, 120, 4f, desc: "왼쪽 회전 날로 되받아 벤다."));
            Add(S("DUAL_3", "교차 회전참", 9, 4, 2, 1f, 0f, 0, 0.45f, 0.30f, 0.35f, new[] { "DualSlash1", "DualCross" }, 2.2f, 140, 6f, desc: "한 번 더 베고 두 날을 엇갈려 X 자로 가른다."));
            Add(S("DUAL_Q", "회전 돌진", 11, 4, 2, 1.05f, 6f, 0, 0.5f, 0.30f, 0.35f, new[] { "DualDash", "DualDash" }, 2.4f, 90, 6f, lunge: 5f, desc: "돌아가는 두 날을 앞세우고 두 번 파고든다."));
            Add(S("DUAL_E", "회전 난무", 9, 3, 4, 0.9f, 9f, 0, 0.4f, 0.30f, 0.45f, new[] { "DualWhirl", "DualWhirl", "DualWhirl", "DualWhirl" }, 3.0f, 360, 5f, desc: "몸째로 빙글빙글 돌며 주위를 네 번 벤다."));
            Add(S("DUAL_R", "궁극 - 회절원참격", 16, 6, 6, 1.3f, 3f, 0, 0.34f, 0.30f, 0.7f, new[] { "DualGrind", "DualGrind", "DualGrind", "DualGrind", "DualGrind", "DualFinale" }, 2.6f, 140, 10f, lunge: 1f,
                  desc: "결투 고조 최대에서만. 회전하는 두 날로 상대를 다섯 번 갈아 버리고, 뛰어올라 내려찍는다. 파괴불가 코인 — 합에서 져도 끊기지 않는다."));
            if (map.TryGetValue("DUAL_R", out var dur)) { dur.hitTimes = new[] { 0.30f, 0.30f, 0.30f, 0.30f, 0.30f, 0.83f }; dur.unbreakableCoins = true; dur.requiresDuelHeat = true; }
            // ── 무기: 레이저 블레이더(회전 쌍검 상위 — 양손 · 넓은 범위). 클립 타격 프레임: Laser* 10(0.30초) · LaserFinale 26(0.83초)
            Add(S("LASER_1", "광선 내려베기", 9, 4, 1, 1.05f, 0f, 0, 0.75f, 0.30f, 0.25f, new[] { "LaserSlash1" }, 2.9f, 160, 5f, desc: "돌아가는 광선검을 머리 위에서 크게 내려벤다."));
            Add(S("LASER_2", "광선 휩쓸기", 9, 4, 1, 1.05f, 0f, 0, 0.75f, 0.30f, 0.25f, new[] { "LaserSlash2" }, 2.9f, 200, 5f, desc: "광선검을 옆으로 크게 휘둘러 넓게 쓸어 낸다."));
            Add(S("LASER_3", "십자 광참", 10, 4, 2, 1.05f, 0f, 0, 0.45f, 0.30f, 0.35f, new[] { "LaserSlash2", "LaserCross" }, 3.1f, 200, 7f, desc: "휩쓸고 뛰어올라 내려찍어 십자 광선을 남긴다."));
            Add(S("LASER_Q", "광륜 투척", 12, 5, 2, 1.1f, 6f, 0, 0.55f, 0.30f, 0.4f, new[] { "LaserSlash2", "LaserSlash2" }, 11f, 60, 6f, desc: "도는 광선검에서 푸른 빛의 고리를 두 번 날린다. 원거리 — 다른 원거리 공격하고만 공중에서 합한다."));
            if (map.TryGetValue("LASER_Q", out var lq)) { lq.isProjectile = true; lq.projectileKind = ProjectileKind.LaserDisc; lq.projectileSpeed = 15f; lq.projectileRadius = 0.55f; lq.projectileHoming = 40f; }
            Add(S("LASER_E", "광륜 난무", 10, 4, 4, 0.95f, 9f, 0, 0.4f, 0.30f, 0.45f, new[] { "LaserWhirl", "LaserWhirl", "LaserWhirl", "LaserWhirl" }, 4.5f, 360, 6f, desc: "몸째로 돌며 빛의 고리를 네 번 그린다(회전 난무보다 1.5배 넓다)."));
            Add(S("LASER_R", "궁극 - 광륜 회절참", 18, 6, 6, 1.4f, 3f, 0, 0.34f, 0.30f, 0.7f, new[] { "LaserGrind", "LaserGrind", "LaserGrind", "LaserGrind", "LaserGrind", "LaserFinale" }, 3.2f, 180, 11f, lunge: 1f,
                  desc: "결투 고조 최대에서만. 광선검을 최고 속도로 돌려 다섯 번 갈아 버리고, 뛰어올라 빛의 고리째 내려찍는다. 파괴불가 코인."));
            if (map.TryGetValue("LASER_R", out var lsr)) { lsr.hitTimes = new[] { 0.30f, 0.30f, 0.30f, 0.30f, 0.30f, 0.83f }; lsr.unbreakableCoins = true; lsr.requiresDuelHeat = true; }
            // ── 인간형 적: 잠꾸러기 곰(애니메이션 있음 — hitTime 을 클립 타격 프레임에 맞춤: Attack1/2 0.35초, Attack3 0.43초)
            Add(S("HUMAN_JAB", "솜주먹 연타", 6, 3, 3, 0.6f, 2.5f, 0, 0.4f, 0.35f, 0.7f, new[] { "Attack1", "Attack2", "Attack1" }, 1.7f, 90, 3f, windup: 0.45f, lunge: 2f));
            Add(S("HUMAN_SLAM", "베개 내려찍기", 10, 4, 2, 0.8f, 5f, 0, 0.6f, 0.43f, 1.0f, new[] { "Attack3" }, 2.0f, 140, 7f, windup: 0.85f, lunge: 1.5f));
            Add(S("HUMAN_RUSH", "졸음 돌진", 9, 4, 1, 0.8f, 7f, 0, 0.5f, 0.35f, 1.1f, new[] { "Attack1" }, 5.0f, 60, 8f, windup: 0.75f, lunge: 10f));
            // ── 지상괴물형: 고래고래(지상 최대의 끼룩이). 거대해서 사거리가 김. hitTime = 클립 타격 프레임 / 30
            Add(S("WHALE_RAM", "고래박치기", 12, 5, 2, 1.0f, 4f, 0, 0.8f, 0.37f, 0.8f, A1, 3.4f, 80, 10f, windup: 0.7f, lunge: 9f, desc: "머리로 힘껏 들이받는다. 합 가능."));
            Add(S("WHALE_BEAM", "고래빔", 12, 4, 4, 0.9f, 11f, 0, 0.5f, 0.27f, 0.85f, new[] { "Beam", "BeamHold", "BeamHold", "BeamHold" }, 13f, 30, 6f, windup: 0.9f,
                  desc: "기를 모아 입을 크게 벌리고, 앞쪽 일직선을 오래도록 쓸어 버린다(4번 판정). 합 불가 광역."));
            Add(S("WHALE_JUMP", "점프 내려찍기", 14, 4, 1, 1.2f, 7f, 0, 0.8f, 0.8f, 1.0f, new[] { "Jump" }, 4f, 360, 9f, windup: 0.6f, lunge: 11f, desc: "높이 뛰어올라 덮친다. 착지한 자리 주변 모두에게 충격."));
            Add(S("WHALE_FLIPPER", "지느러미 연타", 7, 3, 3, 0.8f, 3f, 0, 0.5f, 0.37f, 0.6f, new[] { "Attack2" }, 3.0f, 110, 4f, windup: 0.45f, lunge: 2f));
            Add(S("WHALE_TAIL", "꼬리 휘두르기", 10, 4, 2, 0.9f, 6f, 0, 0.9f, 0.47f, 0.7f, new[] { "Attack3" }, 3.6f, 360, 8f, windup: 0.55f));
            Add(S("WHALE_SPOUT", "물기둥", 9, 3, 2, 0.8f, 9f, 0, 0.9f, 0.53f, 0.8f, new[] { "Spout" }, 5f, 360, 5f, windup: 0.8f, desc: "숨구멍으로 물기둥을 뿜어 주위를 적신다."));
            // ── 일반 몬스터 ①: 핑크큐브말랑이(약하고 빠름, 통통 튀며 몸통박치기 · 길게 늘어나 찌르기). 타격 = 클립 11프레임(0.37초)
            Add(S("CUBE_BOUNCE", "말랑 박치기", 6, 3, 1, 0.7f, 1.6f, 0, 0.5f, 0.37f, 0.6f, new[] { "Attack1" }, 1.5f, 100, 5f, windup: 0.5f, lunge: 6f, desc: "통통 튀어 올라 몸통으로 철퍽 들이받는다."));
            Add(S("CUBE_STRETCH", "쭈욱 늘이기", 9, 4, 1, 0.8f, 4f, 0, 0.5f, 0.37f, 0.7f, new[] { "Attack2" }, 2.4f, 60, 6f, windup: 0.6f, desc: "몸을 길게 쭈욱 늘여 멀리 있는 상대를 찌른다. 맞으면 힘이 빠진다."));
            Add(S("CUBE_TRIPLE", "통통 연속 박치기", 5, 2, 3, 0.5f, 6f, 0, 0.8f, 0.37f, 0.6f, new[] { "Attack1" }, 1.6f, 100, 4f, windup: 0.6f, lunge: 4f, desc: "세 번 연달아 통통 튀며 들이받는다."));
            // ── 일반 몬스터 ②: 그린볼런(단단하고 느림, 블럭 박치기 · 톱니 던지기 = 원거리 투사체)
            Add(S("BLOCK_RAM", "블럭 박치기", 9, 4, 1, 1.0f, 3.5f, 0, 0.5f, 0.37f, 0.9f, new[] { "Attack1" }, 1.7f, 90, 9f, windup: 0.8f, lunge: 5f, desc: "단단한 몸을 기울여 쿵 들이받는다."));
            Add(S("BLOCK_GEAR", "톱니 던지기", 7, 3, 2, 0.7f, 5f, 0, 0.8f, 0.37f, 0.7f, new[] { "Attack2" }, 9f, 40, 3f, windup: 0.6f,
                  desc: "톱니바퀴를 상대에게 날린다(원거리). 근접 공격으로는 합할 수 없고, 다른 원거리 공격과만 공중에서 합한다. 맞으면 베여 출혈."));
            // ── 펫: 하프물범(위력이 낮음 — 합은 할 수 있지만 대개 지므로 일방 공격이 더 좋음)
            Add(S("SEAL_BUMP", "몽실 박치기", 4, 2, 1, 0.6f, 2.5f, 0, 0.5f, 0.37f, 0.5f, new[] { "Attack1" }, 1.5f, 100, 3f, windup: 0.25f, lunge: 4f, desc: "통통한 몸으로 들이받는다. 주인을 노리는 공격을 가로막아 대신 합하기도 한다."));
            Add(S("SEAL_BUBBLE", "물방울 뿜기", 3, 2, 2, 0.5f, 4f, 0, 0.5f, 0.37f, 0.5f, new[] { "Attack2" }, 7f, 40, 1f, windup: 0.3f, desc: "코에서 물방울을 뿜어 멀리 있는 적을 적신다(원거리). 맞으면 침잠."));
            // ── 적(같은 SkillExecutor 사용). windup = 바닥 경고 시간
            Add(S("ENEMY_BUMP", "몸통 박치기", 7, 3, 2, 0.7f, 1.2f, 0, 0.35f, 0.12f, 0.8f, A1, 1.6f, 100, 4f, windup: 0.65f, lunge: 5f));
            Add(S("ENEMY_SLAM", "내려찍기", 9, 4, 2, 0.8f, 4f, 0, 0.45f, 0.15f, 1.1f, A1, 2.0f, 360, 7f, windup: 1.0f, lunge: 1f));
            Add(S("BOSS_LUNGE", "돌진", 13, 4, 2, 0.8f, 5f, 0, 0.45f, 0.15f, 1.0f, A1, 6f, 70, 8f, windup: 0.8f, lunge: 12f));
            Add(S("BOSS_FLURRY", "난타", 9, 3, 4, 0.6f, 6f, 0, 0.3f, 0.12f, 0.9f, A1, 2.4f, 120, 3f, windup: 0.7f, lunge: 3f));
            Add(S("BOSS_BREAK", "방어 부수기", 18, 6, 1, 1.0f, 8f, 0, 0.5f, 0.15f, 1.2f, A1, 3.2f, 120, 9f, windup: 1.2f, lunge: 3f, unblockable: true));
            // 속성(색)·일러스트 동작
            Look("BASIC_1", SkillAttribute.Yellow, SkillMotif.Slash); Look("BASIC_2", SkillAttribute.Green, SkillMotif.Slash); Look("BASIC_3", SkillAttribute.Blue, SkillMotif.Flurry);
            Look("SKILL_001", SkillAttribute.Red, SkillMotif.Flurry); Look("SKILL_002", SkillAttribute.Orange, SkillMotif.Thrust); Look("SKILL_003", SkillAttribute.Purple, SkillMotif.Slash);
            Look("HUMAN_JAB", SkillAttribute.Yellow, SkillMotif.Flurry); Look("HUMAN_SLAM", SkillAttribute.Blue, SkillMotif.Smash); Look("HUMAN_RUSH", SkillAttribute.Green, SkillMotif.Rush);
            Look("ENEMY_BUMP", SkillAttribute.Black, SkillMotif.Rush); Look("ENEMY_SLAM", SkillAttribute.Purple, SkillMotif.Smash);
            // 적중 시 상태이상(앞면 / 뒷면)
            St("BASIC_1", StatusType.Tremor, 1, 1); St("BASIC_2", StatusType.Sinking, 1, 1); St("BASIC_3", StatusType.Tremor, 2, 1, 1, 1);
            St("SKILL_001", StatusType.Bleed, 2, 1, 1, 1); St("SKILL_002", StatusType.Burn, 3, 2, 2, 1); St("SKILL_003", StatusType.Sinking, 2, 1);
            St("HUMAN_JAB", StatusType.Tremor, 1, 1); St("HUMAN_SLAM", StatusType.Sinking, 2, 2, 1, 1); St("ENEMY_SLAM", StatusType.Tremor, 2, 1);
            St("BOSS_LUNGE", StatusType.Bleed, 3, 2, 1, 1); St("BOSS_FLURRY", StatusType.Burn, 2, 1);
            // 코인 위력: 힘(자신) · 위력 감소 · 마비(상대). 횟수 = 영향받는 코인 수
            St("SKILL_001", StatusType.PowerUp, 1, 2); St("SKILL_002", StatusType.PowerDown, 2, 2); St("BASIC_3", StatusType.PowerUp, 1, 1);
            St("HUMAN_JAB", StatusType.Paralysis, 1, 1); St("ENEMY_BUMP", StatusType.PowerDown, 1, 2); St("BOSS_FLURRY", StatusType.PowerDown, 2, 2); St("BOSS_BREAK", StatusType.Paralysis, 1, 2);
            // 광역(공통 코인): 공격자 중심 반경 안 모든 적
            Area("SHIELD_E", 3.5f); Area("DUAL_E", 3.0f); Area("LASER_E", 4.5f);
            Look("LASER_1", SkillAttribute.Blue, SkillMotif.Slash); Look("LASER_2", SkillAttribute.Blue, SkillMotif.Slash); Look("LASER_3", SkillAttribute.Blue, SkillMotif.Smash);
            Look("LASER_Q", SkillAttribute.Blue, SkillMotif.Throw); Look("LASER_E", SkillAttribute.Purple, SkillMotif.Flurry); Look("LASER_R", SkillAttribute.Blue, SkillMotif.Flurry);
            Look("SHIELD_1", SkillAttribute.Yellow, SkillMotif.Rush); Look("SHIELD_2", SkillAttribute.Yellow, SkillMotif.Slash); Look("SHIELD_3", SkillAttribute.Orange, SkillMotif.Smash);
            Look("SHIELD_Q", SkillAttribute.Yellow, SkillMotif.Rush); Look("SHIELD_E", SkillAttribute.Orange, SkillMotif.Smash); Look("SHIELD_R", SkillAttribute.Orange, SkillMotif.Leap);
            Look("DUAL_1", SkillAttribute.Blue, SkillMotif.Slash); Look("DUAL_2", SkillAttribute.Blue, SkillMotif.Slash); Look("DUAL_3", SkillAttribute.Purple, SkillMotif.Flurry);
            Look("DUAL_Q", SkillAttribute.Blue, SkillMotif.Rush); Look("DUAL_E", SkillAttribute.Purple, SkillMotif.Flurry); Look("DUAL_R", SkillAttribute.Red, SkillMotif.Flurry);
            Area("SKILL_003", 3.2f); Area("SKILL_004", 4f); Area("ENEMY_SLAM", 2f);
            // 고래고래: 빔 = 앞으로 13m·폭 2.4m 직선(합 불가), 점프·꼬리·물기둥 = 원(점프·물기둥 합 불가)
            Area("WHALE_BEAM", 13f); Area("WHALE_JUMP", 4f); Area("WHALE_TAIL", 3.6f); Area("WHALE_SPOUT", 5f);
            if (map.TryGetValue("WHALE_BEAM", out var wb)) { wb.areaShape = AreaShape.Line; wb.areaWidth = 2.4f; wb.canBeClashed = false; wb.vfx = SkillVfx.WhaleBeam; wb.windupAnimation = "BeamCharge"; wb.recoveryAnimation = "BeamEnd"; }   // 기 모으기 0.9초 → 발사 → 2초 가까이 유지 → 마무리
            if (map.TryGetValue("WHALE_JUMP", out var wj)) { wj.canBeClashed = false; wj.vfx = SkillVfx.Shockwave; wj.lungeDelay = 0.27f; wj.lungeTime = 0.5f; }   // 공중에 뜬 동안(8~23프레임) 날아감
            if (map.TryGetValue("WHALE_SPOUT", out var ws)) { ws.canBeClashed = false; ws.vfx = SkillVfx.WaterSpout; }
            if (map.TryGetValue("WHALE_RAM", out var wr)) wr.lungeTime = 0.35f;
            Look("WHALE_RAM", SkillAttribute.Blue, SkillMotif.Rush); Look("WHALE_BEAM", SkillAttribute.Blue, SkillMotif.Beam); Look("WHALE_JUMP", SkillAttribute.Purple, SkillMotif.Leap);
            Look("WHALE_FLIPPER", SkillAttribute.Green, SkillMotif.Flurry); Look("WHALE_TAIL", SkillAttribute.Blue, SkillMotif.Slash); Look("WHALE_SPOUT", SkillAttribute.Blue, SkillMotif.Spout);
            foreach (var id in new[] { "WHALE_RAM", "WHALE_BEAM", "WHALE_JUMP", "WHALE_FLIPPER", "WHALE_TAIL", "WHALE_SPOUT" }) if (map.TryGetValue(id, out var wsk)) wsk.subject = IllustrationSubject.Whale;   // 일러스트 = 고래 실루엣
            St("WHALE_RAM", StatusType.Tremor, 2, 1); St("WHALE_BEAM", StatusType.Sinking, 2, 2, 1, 1); St("WHALE_JUMP", StatusType.Tremor, 3, 1, 1, 1);
            St("WHALE_FLIPPER", StatusType.PowerDown, 1, 1); St("WHALE_SPOUT", StatusType.Sinking, 1, 2);
            // ── 맨두(왕실근위대 엘리트 하프물범, 성문 지기) — 속성 노랑 · 타입 진동 · 무기 파란 야구방망이
            //    클립 타격 프레임: Bat1/Bat2 11(0.37초) · Bat3 13(0.43) · HomeRun 23(0.77) · Charge 14(0.47) · Chomp 20(0.67) · MillionTon 62(2.07) · Attack1 11(0.37)
            string[] BEAT = { "Bat1", "Bat2", "Bat3" }, HRUN = { "HomeRun" }, CHRG = { "Charge" }, CHOMP = { "Chomp" }, MTON = { "MillionTon" };
            Add(S("MANDU_BEAT", "몽둥이 찜질", 11, 5, 3, 1.0f, 7f, 0, 0.62f, 0.37f, 0.5f, BEAT, 2.9f, 120, 6f, windup: 0.5f, lunge: 2.5f,
                  desc: "방망이로 찰싹찰싹 세 번 찜질한다. 합에서 이기면 신이 나서 코인을 세 번 더 쓴다(마지막 타격 반복 — 진동이 쌓인다). \"맞아라맨두!\""));
            Add(S("MANDU_HOMERUN", "홈런!!", 26, 9, 1, 1.35f, 12f, 0, 0.8f, 0.77f, 0.8f, HRUN, 3.2f, 100, 16f, windup: 0.75f, lunge: 5f,
                  desc: "크게 뒤로 당겼다가 온 힘을 실어 풀스윙. 쌓인 진동을 터뜨린다. 한 방이 무겁다. \"홈런이다맨두!!\""));
            Add(S("MANDU_BUMP", "근위대 배치기", 12, 4, 1, 1.0f, 4f, 0, 0.8f, 0.37f, 0.6f, A1, 2.5f, 100, 9f, windup: 0.3f, lunge: 5f,
                  desc: "통통한 배로 들이받는다. 맨두맨두로 막아 낸 뒤 바로 되받아치는 반격 기술. \"물러서라맨두!\""));
            Add(S("MANDU_CHARGE", "맨두돌진", 16, 6, 1, 1.1f, 9f, 0, 0.8f, 0.47f, 0.7f, CHRG, 3.0f, 80, 14f, windup: 0.6f, lunge: 13f,
                  desc: "방망이를 앞세우고 데굴데굴 굴러 돌진한다. 멀리 있어도 단숨에 닿는다."));
            Add(S("MANDU_CHOMP", "냠냠쫍쫍", 20, 7, 1, 1.2f, 16f, 0, 0.8f, 0.67f, 1.2f, CHOMP, 2.6f, 160, 11f, windup: 0.5f, unblockable: true,
                  desc: "갑자기 거대해져서 온몸으로 민미를 폭 감싸 버린다. 한참 오물거리다가 퉤 뱉어내고 원래 크기로 돌아온다. 막을 수 없다."));
            Add(S("MANDU_MILLION", "100만톤 맨두", 42, 14, 1, 1.6f, 45f, 0, 1f, 2.07f, 2.9f, MTON, 7.5f, 360, 20f, windup: 1.0f, unblockable: true,
                  desc: "필살기 — 체력이 25% 아래로 떨어지면 쓴다. 자기 키의 다섯 배로 뛰어올랐다가 민미의 머리 위로 떨어진다. 바닥이 부서지고 쌓인 진동이 한꺼번에 터진다. 빨간 원 밖으로 피하자."));
            Add(S("MANDU_MANDU", "맨두맨두", 14, 5, 1, 1.0f, 6f, 0, 0.8f, 0.3f, 0.4f, A1, 2.2f, 120, 4f,
                  desc: "합 방어 기술 — 방망이를 가로로 세워 받아낸다. 민미의 공격 위력이 맨두의 방어 위력보다 낮으면 그대로 근위대 배치기로 되받아친다."));
            if (map.TryGetValue("MANDU_CHARGE", out var mc)) { mc.lungeDelay = 0.12f; mc.lungeTime = 0.5f; }
            if (map.TryGetValue("MANDU_MILLION", out var mm)) { mm.windupAnimation = "TonCharge"; mm.isAreaAttack = true; mm.areaRadius = 7.5f; mm.vfx = SkillVfx.Shockwave; mm.canBeEvaded = true; }
            if (map.TryGetValue("MANDU_HOMERUN", out var mh)) mh.vfx = SkillVfx.Shockwave;
            foreach (var id in new[] { "MANDU_BEAT", "MANDU_HOMERUN", "MANDU_BUMP", "MANDU_CHARGE", "MANDU_CHOMP", "MANDU_MILLION", "MANDU_MANDU" })
                if (map.TryGetValue(id, out var mk)) { mk.subject = IllustrationSubject.Seal; mk.attribute = SkillAttribute.Yellow; }
            Look("MANDU_BEAT", SkillAttribute.Yellow, SkillMotif.Flurry); Look("MANDU_HOMERUN", SkillAttribute.Yellow, SkillMotif.Smash);
            Look("MANDU_BUMP", SkillAttribute.Yellow, SkillMotif.Rush); Look("MANDU_CHARGE", SkillAttribute.Yellow, SkillMotif.Rush);
            Look("MANDU_CHOMP", SkillAttribute.Yellow, SkillMotif.Bubbles); Look("MANDU_MILLION", SkillAttribute.Yellow, SkillMotif.Leap);
            Look("MANDU_MANDU", SkillAttribute.Yellow, SkillMotif.Thrust);
            // 타입 = 진동: 맞을 때마다 진동이 쌓이고, 홈런·100만톤이 그 진동을 터뜨림
            St("MANDU_BEAT", StatusType.Tremor, 3, 2, 2, 1); St("MANDU_HOMERUN", StatusType.Tremor, 5, 2, 3, 1); St("MANDU_BUMP", StatusType.Tremor, 2, 1);
            St("MANDU_CHARGE", StatusType.Tremor, 3, 2, 2, 1); St("MANDU_CHOMP", StatusType.Tremor, 4, 3, 3, 2); St("MANDU_MILLION", StatusType.Tremor, 8, 4, 6, 3);
            Energy("MANDU_HOMERUN", 0, 6, true); Energy("MANDU_MILLION", 0, 10, true);
            // ── 고래고래 임금님(용궁 보스) 2페이지(각성) 기술 — 사용자 그림: 고래빔(각성) · 박치기 · 회전 탄막 · 딸기 폭탄 · 버블 감옥
            //    탄막·원거리(회전 탄막 · 딸기 폭탄 · 버블 감옥 · 각성 고래빔)는 합 불가 광역: 날아오는 걸 보고 피하기. 박치기만 합 가능(근접)
            Add(S("KING_RAM", "박치기", 14, 5, 2, 1.0f, 5f, 0, 0.85f, 0.37f, 0.9f, new[] { "Attack1", "Attack1" }, 3.8f, 90, 12f, windup: 0.65f, lunge: 13f,
                  desc: "왕관을 앞세워 결투장을 가로질러 두 번 들이받는다. 합 가능."));
            Add(S("KING_ORBIT", "회전 탄막", 8, 3, 3, 0.75f, 9f, 0, 0.6f, 0.47f, 0.8f, new[] { "Attack3" }, 15f, 360, 3f, windup: 0.9f,
                  desc: "몸을 빙글 돌며 빛 구슬 고리를 사방으로 세 번 뿜는다. 고리마다 조금씩 엇갈리니 틈으로 피할 것. 합 불가 광역."));
            Add(S("KING_STRAWBERRY", "딸기 폭탄", 11, 4, 3, 0.9f, 8f, 0, 0.55f, 0.53f, 0.8f, new[] { "Spout" }, 13f, 360, 6f, windup: 0.8f,
                  desc: "숨구멍으로 딸기를 퐁퐁 던진다. 떨어질 자리에 분홍 원이 생기니 원 밖으로! 터지면 딸기잼 화상. 합 불가 광역."));
            Add(S("KING_BUBBLE", "버블 감옥", 9, 3, 2, 0.8f, 11f, 0, 0.9f, 0.27f, 0.8f, new[] { "Beam", "Beam" }, 12f, 360, 2f, windup: 0.9f,
                  desc: "커다란 물방울 세 개를 부채꼴로 천천히 불어 보낸다. 맞으면 물방울에 갇혀 2초 동안 못 움직인다. 합 불가 광역."));
            Add(S("KING_RAINBOW", "고래빔(각성)", 14, 5, 5, 1.0f, 12f, 0, 0.45f, 0.27f, 0.9f, new[] { "Beam", "BeamHold", "BeamHold", "BeamHold", "BeamHold" }, 16f, 30, 7f, windup: 1.1f,
                  desc: "각성했을 때만: 무지갯빛 고래빔을 길고 굵게 쏘아 휩쓴다(5번 판정). 합 불가 광역."));
            if (map.TryGetValue("KING_RAM", out var kr)) { kr.lungeTime = 0.4f; }
            if (map.TryGetValue("KING_ORBIT", out var ko)) { ko.isProjectile = true; ko.canBeClashed = false; ko.projectileKind = ProjectileKind.Orb; ko.projectileSpeed = 6.5f; ko.projectileRadius = 0.32f; ko.projectileHoming = 0f; ko.projectileCount = 14; ko.projectileSpread = 360f; ko.projectileSpin = 12.857f; }
            if (map.TryGetValue("KING_STRAWBERRY", out var ksb)) { ksb.isProjectile = true; ksb.canBeClashed = false; ksb.projectileKind = ProjectileKind.Strawberry; ksb.projectileRadius = 0.35f; ksb.projectileLob = true; ksb.projectileCount = 3; ksb.lobTime = 1.15f; ksb.lobScatter = 3.2f; ksb.areaRadius = 2.3f; }
            if (map.TryGetValue("KING_BUBBLE", out var kbb)) { kbb.isProjectile = true; kbb.canBeClashed = false; kbb.projectileKind = ProjectileKind.Bubble; kbb.projectileSpeed = 5.2f; kbb.projectileRadius = 0.75f; kbb.projectileHoming = 30f; kbb.projectileCount = 3; kbb.projectileSpread = 50f; kbb.trapSeconds = 2f; kbb.windupAnimation = "BeamCharge"; }
            if (map.TryGetValue("KING_RAINBOW", out var krb)) { krb.isAreaAttack = true; krb.areaRadius = 16f; krb.areaShape = AreaShape.Line; krb.areaWidth = 3.0f; krb.canBeClashed = false; krb.vfx = SkillVfx.WhaleBeam; krb.windupAnimation = "BeamCharge"; krb.recoveryAnimation = "BeamEnd"; }
            Look("KING_RAM", SkillAttribute.Blue, SkillMotif.Rush); Look("KING_ORBIT", SkillAttribute.Purple, SkillMotif.Barrage); Look("KING_STRAWBERRY", SkillAttribute.Red, SkillMotif.Bomb);
            Look("KING_BUBBLE", SkillAttribute.Blue, SkillMotif.Bubbles); Look("KING_RAINBOW", SkillAttribute.Purple, SkillMotif.Beam);
            foreach (var id in new[] { "KING_RAM", "KING_ORBIT", "KING_STRAWBERRY", "KING_BUBBLE", "KING_RAINBOW" }) if (map.TryGetValue(id, out var kk)) kk.subject = IllustrationSubject.Whale;
            St("KING_RAM", StatusType.Tremor, 3, 1, 1, 1); St("KING_ORBIT", StatusType.PowerDown, 1, 1); St("KING_STRAWBERRY", StatusType.Burn, 2, 2, 1, 1); St("KING_RAINBOW", StatusType.Sinking, 3, 2, 1, 1);
            Look("SKILL_004", SkillAttribute.Yellow, SkillMotif.Smash);
            // 일반 몬스터: 핑크큐브(빨강, 말랑이 실루엣) · 그린볼런(초록, 블럭 실루엣)
            foreach (var id in new[] { "CUBE_BOUNCE", "CUBE_STRETCH", "CUBE_TRIPLE" }) if (map.TryGetValue(id, out var cs)) cs.subject = IllustrationSubject.Slime;
            foreach (var id in new[] { "BLOCK_RAM", "BLOCK_GEAR" }) if (map.TryGetValue(id, out var bs)) bs.subject = IllustrationSubject.Block;
            Look("CUBE_BOUNCE", SkillAttribute.Red, SkillMotif.Rush); Look("CUBE_STRETCH", SkillAttribute.Red, SkillMotif.Thrust); Look("CUBE_TRIPLE", SkillAttribute.Orange, SkillMotif.Flurry);
            Look("BLOCK_RAM", SkillAttribute.Green, SkillMotif.Smash); Look("BLOCK_GEAR", SkillAttribute.Green, SkillMotif.Throw);
            if (map.TryGetValue("CUBE_BOUNCE", out var cb)) { cb.lungeDelay = 0.13f; cb.lungeTime = 0.23f; }   // 4~11프레임 공중에서 날아감
            if (map.TryGetValue("CUBE_TRIPLE", out var ct)) { ct.lungeDelay = 0.13f; ct.lungeTime = 0.23f; }
            if (map.TryGetValue("BLOCK_RAM", out var br)) { br.lungeDelay = 0.2f; br.lungeTime = 0.17f; }
            if (map.TryGetValue("BLOCK_GEAR", out var bg)) { bg.isProjectile = true; bg.projectileKind = ProjectileKind.Gear; bg.projectileSpeed = 10f; bg.projectileRadius = 0.3f; bg.projectileHoming = 25f; }
            foreach (var id in new[] { "SEAL_BUMP", "SEAL_BUBBLE" }) if (map.TryGetValue(id, out var ss)) ss.subject = IllustrationSubject.Seal;
            Look("SEAL_BUMP", SkillAttribute.Blue, SkillMotif.Rush); Look("SEAL_BUBBLE", SkillAttribute.Blue, SkillMotif.Throw);
            if (map.TryGetValue("SEAL_BUMP", out var sb)) { sb.lungeDelay = 0.15f; sb.lungeTime = 0.2f; }
            if (map.TryGetValue("SEAL_BUBBLE", out var sbb)) { sbb.isProjectile = true; sbb.projectileKind = ProjectileKind.Bubble; sbb.projectileSpeed = 9f; sbb.projectileRadius = 0.25f; sbb.projectileHoming = 40f; }
            St("SEAL_BUBBLE", StatusType.Sinking, 1, 1);
            St("CUBE_STRETCH", StatusType.PowerDown, 1, 1); St("CUBE_TRIPLE", StatusType.Tremor, 1, 1);
            St("BLOCK_RAM", StatusType.Tremor, 2, 1, 1, 1); St("BLOCK_GEAR", StatusType.Bleed, 1, 2, 1, 1);
            // 에너지: (소비, 명중 시 추가 획득, 진동 폭발)
            Energy("BASIC_1", 0, 4); Energy("BASIC_2", 0, 4); Energy("BASIC_3", 0, 4);
            Energy("SKILL_001", 0, 2); Energy("SKILL_002", 0, 2);
            Energy("SKILL_003", 60, 0, burst: true);
            Energy("SHIELD_Q", 0, 2); Energy("SHIELD_E", 0, 2); Energy("SHIELD_R", 60, 0); Energy("DUAL_Q", 0, 2); Energy("DUAL_E", 0, 2);
            St("SHIELD_E", StatusType.Tremor, 2, 1); St("SHIELD_3", StatusType.Tremor, 1, 1); St("DUAL_R", StatusType.Bleed, 2, 1, 1, 1); St("DUAL_E", StatusType.Bleed, 1, 1);
            if (map.TryGetValue("SHIELD_E", out var she)) she.vfx = SkillVfx.Shockwave;
            if (map.TryGetValue("SHIELD_R", out var shr2)) { shr2.vfx = SkillVfx.Shockwave; shr2.vfxLastCoinOnly = true; shr2.lunge = 3f; }
            if (map.TryGetValue("DUAL_R", out var dur2)) { dur2.vfx = SkillVfx.Shockwave; dur2.vfxLastCoinOnly = true; }
            if (map.TryGetValue("LASER_R", out var lsr2)) { lsr2.vfx = SkillVfx.Shockwave; lsr2.vfxLastCoinOnly = true; }
            if (map.TryGetValue("LASER_3", out var ls3)) { ls3.vfx = SkillVfx.Shockwave; ls3.vfxLastCoinOnly = true; }
            Energy("LASER_Q", 0, 2); Energy("LASER_E", 0, 2); St("LASER_E", StatusType.Burn, 1, 1); St("LASER_R", StatusType.Burn, 2, 1, 1, 1);   // 마지막 내려찍기에서 충격파   // 강화 스킬: 에너지 60 소비, 진동 폭발
            Energy("BOSS_BREAK", 40, 0, burst: true); Energy("HUMAN_RUSH", 0, 0, burst: true);
            Look("BOSS_LUNGE", SkillAttribute.Red, SkillMotif.Rush); Look("BOSS_FLURRY", SkillAttribute.Orange, SkillMotif.Flurry); Look("BOSS_BREAK", SkillAttribute.Black, SkillMotif.Smash);
        }
    }
}
