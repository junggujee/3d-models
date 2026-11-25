using System;
using System.Collections.Generic;

namespace MoominAction
{
    /// <summary>
    /// 토벌 퀘스트 정의(순수 C# — status_tests 로 검증): 누가 · 어느 구역의 어떤 몬스터를 몇 마리 · 기본 보상 · 이야기 대사.
    /// 대사 한 줄 = "표정|동작|글". 글 안의 {n} {need} {monster} {zone} {giver} 는 HuntMath.Fill 이 채움.
    /// 구역 자리 = Blender 좌표(zoneBx, zoneBy — 월드맵 데이터와 같은 좌표, MapPlaces.BlenderToWorld 로 게임 좌표).
    /// </summary>
    public class HuntDef
    {
        public string id, title, giver, zoneId, zoneName, monsterKind, monsterName;
        public int need = 5, baseExp = 100, baseGold = 40, minLevel = 1, maxAlive = 4;
        public float zoneBx, zoneBy, zoneRadius = 9f;
        /// <summary>처음 만났을 때 이야기(마지막 줄 뒤에 수락/거절 선택지).</summary>
        public string[] intro = new string[0];
        /// <summary>수락했을 때 2줄 · 거절 · 진행 중(아직) · 힌트 · 다 잡고 왔을 때 · 고마움 · 보상 줄.</summary>
        public string[] accept = new string[0];
        public string decline, waiting, hint, ready, thanks, reward;
        /// <summary>다시 부탁할 때(반복) — 완료 횟수에 따라 돌아가며.</summary>
        public string[] repeats = new string[0];
    }

    /// <summary>토벌 퀘스트 목록(조랭하프족 마을 주민들이 줌, 반복 가능).</summary>
    public static class HuntCatalog
    {
        public const string PinkCube = "hunt_pinkcube", Shadow = "hunt_shadow", Bear = "hunt_bear", Green = "hunt_green";
        static readonly Dictionary<string, HuntDef> defs = new Dictionary<string, HuntDef>();
        public static readonly List<HuntDef> All = new List<HuntDef>();
        public static HuntDef Get(string id) => id != null && defs.TryGetValue(id, out var h) ? h : null;
        static void Add(HuntDef h) { defs[h.id] = h; All.Add(h); }

        static HuntCatalog()
        {
            Add(new HuntDef
            {
                id = PinkCube, title = "말랑이가 빨래를 깔고 앉았어요", giver = "몽글이",
                zoneId = "zone_jelly", zoneName = "말랑말랑 풀밭", zoneBx = 20f, zoneBy = -45f, zoneRadius = 10f,
                monsterKind = "PinkCube", monsterName = "핑크큐브말랑이", need = 5, baseExp = 150, baseGold = 60, minLevel = 1, maxAlive = 4,
                intro = new[]
                {
                    "surprised|Surprised|여행자님! 큰일, 큰일이에요! 제 목도리가… 제 목도리가요!",
                    "sad|Sad|어제 말랑말랑 풀밭에 빨래를 널어 놨는데, 핑크큐브말랑이들이 통통 튀어 와서 그 위에 철퍽 앉아 버렸어요.",
                    "troubled|No|비켜 달라고 했더니 더 신나서 통통 튀어요… 이제 목도리에서 딸기젤리 냄새가 나요.",
                    "happy|Talk|말랑이 다섯 마리만 혼내 주시면, 나머지는 놀라서 비켜 줄 거예요! 도와주실 거죠?",
                },
                accept = new[]
                {
                    "happy|QuestAccept|와아! 역시 털복숭이 여행자님! 말랑말랑 풀밭은 마을에서 서쪽으로 조금만 가면 있어요.",
                    "normal|Yes|분홍 젤리가 통통 튀는 게 보이면 거기예요! 몸통박치기는 생각보다 아프니까 조심하세요!",
                },
                decline = "sad|Sad|흑… 오늘은 목도리 없이 지내야겠네요. 마음 바뀌면 꼭 와 주세요!",
                waiting = "troubled|No|말랑이들이 아직 빨래 위에서 통통거려요… (지금 {n}/{need}마리)",
                hint = "normal|Talk|{zone}은 서쪽이에요. 몸을 쭈욱 늘이는 공격은 옆으로 피하세요!",
                ready = "surprised|Surprised|어?! 풀밭이 조용해졌어요! 여행자님이 해내셨군요!",
                thanks = "happy|Laugh|목도리가 무사해요! 딸기젤리 냄새는… 음, 이것도 나름 괜찮네요. 헤헤.",
                reward = "normal|Yes|이건 빨래 지켜 준 값이에요. 받아 주세요!",
                repeats = new[]
                {
                    "troubled|No|여행자님… 말랑이들이 또 왔어요. 이번엔 이불 빨래 위에서 통통 튀고 있어요!",
                    "surprised|Surprised|큰일이에요! 말랑이들이 빨래집게를 전부 꿀꺽 삼켜 버렸어요!",
                    "sad|Sad|오늘 말린 양말에서도 젤리 냄새가 나요… 말랑이들 좀 다시 쫓아 주세요!",
                },
            });
            Add(new HuntDef
            {
                id = Shadow, title = "꼬물이는 그림자가 무서워", giver = "꼬물이",
                zoneId = "zone_shadow", zoneName = "어둑어둑 뒷골목", zoneBx = 58f, zoneBy = 0f, zoneRadius = 9f,
                monsterKind = "ShadowBlob", monsterName = "그림자", need = 6, baseExp = 130, baseGold = 50, minLevel = 2, maxAlive = 5,
                intro = new[]
                {
                    "sad|Sad|흐엉… 여, 여행자님… 꼬물이 무서워요…",
                    "troubled|No|동쪽 마을 뒷골목에 시커먼 그림자들이 꿈틀꿈틀 돌아다녀요. 밤마다 우리 집 창문을 똑똑 두드려요!",
                    "sad|Sad|엄마는 그냥 나뭇가지 그림자래요. 근데 나뭇가지는 '꾸르륵' 소리를 안 내잖아요…",
                    "normal|Talk|여행자님은 반짝반짝 칼이 있으니까… 그림자 여섯 마리만 쫓아내 주세요. 그럼 꼬물이 오늘 밤 혼자 잘 수 있어요!",
                },
                accept = new[]
                {
                    "happy|QuestAccept|진짜요?! 여행자님 최고! 어둑어둑 뒷골목은 동쪽, 동쪽 마을 가는 길이에요!",
                    "normal|Yes|꼬물이는 여기서 이불 뒤집어쓰고 응원할게요. 힘내라, 힘내라!",
                },
                decline = "sad|Sad|그, 그럼… 오늘 밤도 불 켜고 자야겠다… 히잉.",
                waiting = "troubled|No|아직 창문 똑똑 소리가 나요… (지금 {n}/{need}마리)",
                hint = "normal|Talk|그림자는 약하지만 몰려다녀요. {zone}은 동쪽이에요!",
                ready = "surprised|Surprised|어? 어어? 창문 소리가 안 나요! 여행자님이 다 쫓아낸 거예요?!",
                thanks = "happy|Laugh|꼬물이 이제 하나도 안 무서워요! …아니, 쬐끔은 무서워요. 그래도 괜찮아요!",
                reward = "normal|Yes|이거 꼬물이 저금통에서 꺼냈어요. 여행자님 주는 거예요!",
                repeats = new[]
                {
                    "sad|Sad|여행자님… 그림자들이 또 왔어요. 이번엔 꼬물이 신발을 숨겼어요!",
                    "troubled|No|그림자들이 꼬물이 그림자랑 바꿔치기하려고 해요! 진짜예요!",
                    "surprised|Surprised|어젯밤에 그림자가 '꾸르륵' 했어요! 두 번이나요!",
                },
            });
            Add(new HuntDef
            {
                id = Bear, title = "낮잠 도둑 잠꾸러기 곰", giver = "포실이",
                zoneId = "zone_nap", zoneName = "꾸벅꾸벅 숲", zoneBx = 6f, zoneBy = 26f, zoneRadius = 10f,
                monsterKind = "SleepyBear", monsterName = "잠꾸러기 곰", need = 3, baseExp = 220, baseGold = 90, minLevel = 3, maxAlive = 3,
                intro = new[]
                {
                    "troubled|No|하아암… 아, 죄송해요 여행자님. 요즘 한숨도 못 자서요…",
                    "normal|Talk|꾸벅꾸벅 숲에 잠꾸러기 곰들이 이사를 왔거든요. 코 고는 소리가 '드르렁 쿠와앙!' 하고 마을까지 들려요.",
                    "sad|Sad|게다가 제 낮잠용 해먹까지 가져가서 자기들끼리 돌아가며 누워 있어요… 제 해먹인데요!",
                    "normal|Talk|곰 세 마리만 혼내서 쫓아 주시면 안 될까요? 다른 곰들도 겁먹고 조용해질 거예요.",
                },
                accept = new[]
                {
                    "happy|QuestAccept|정말요? 고마워요! 꾸벅꾸벅 숲은 마을 북동쪽, 나무가 많은 곳이에요.",
                    "troubled|No|곰들이 졸린 척하다가 갑자기 앞발을 휘두르니까 꼭 조심하세요! 하아암…",
                },
                decline = "sad|Sad|괜찮아요… 귀마개 두 겹이면 버틸 수 있을 거예요. 아마도요…",
                waiting = "troubled|No|드르렁… 아직도 들려요… (지금 {n}/{need}마리)",
                hint = "normal|Talk|{zone}은 북동쪽이에요. 곰은 튼튼하니까 체력을 잘 보면서 싸우세요!",
                ready = "surprised|Surprised|어머… 조용해요. 새소리가 들려요! 여행자님이 해내셨군요!",
                thanks = "happy|Laugh|해먹도 돌아왔어요! 이제 실컷 낮잠… 아, 보상부터 드려야죠. 헤헤.",
                reward = "normal|Yes|푹 잘 수 있게 해 준 고마움이에요. 받아 주세요!",
                repeats = new[]
                {
                    "troubled|No|하아암… 여행자님, 곰들이 또 이사 왔어요. 이번엔 베개까지 챙겨 왔더라고요…",
                    "sad|Sad|곰들이 제 해먹을 또 가져갔어요. 이번엔 꽃무늬 해먹인데…!",
                    "surprised|Surprised|어젯밤 코 고는 소리에 지붕 위 솔방울이 다 떨어졌어요!",
                },
            });
            Add(new HuntDef
            {
                id = Green, title = "톱니가 날아오는 땅콩밭", giver = "똘똘이",
                zoneId = "zone_rocks", zoneName = "와글와글 돌무더기", zoneBx = -34f, zoneBy = -29.5f, zoneRadius = 10f,
                monsterKind = "GreenBlock", monsterName = "그린볼런", need = 4, baseExp = 260, baseGold = 110, minLevel = 4, maxAlive = 3,
                intro = new[]
                {
                    "surprised|Surprised|피, 피해요! …아, 이번 건 그냥 솔방울이네. 휴우.",
                    "troubled|No|안녕하세요 여행자님. 저는 땅콩 농사를 짓는 똘똘이예요. 그런데 요즘 밭에 톱니가 날아와요.",
                    "sad|Sad|와글와글 돌무더기에 사는 그린볼런들이 톱니를 휙휙 던지거든요. 땅콩 새싹이 벌써 반이나 잘렸어요…",
                    "normal|Talk|그린볼런 네 마리만 혼내 주세요. 그 녀석들은 딱딱해서 우리 지느러미로는 어림도 없어요.",
                },
                accept = new[]
                {
                    "happy|QuestAccept|고마워요! 와글와글 돌무더기는 북서쪽, 큰 바위들 쪽이에요.",
                    "normal|Yes|날아오는 톱니는 원거리 공격으로 맞받아치거나 옆으로 피하세요. 그냥 맞으면 아파요!",
                },
                decline = "sad|Sad|그럼 저는 새싹들한테 투구라도 씌워 줘야겠네요… 도토리 껍질로요…",
                waiting = "troubled|No|아직도 톱니가 휙휙 날아와요… (지금 {n}/{need}마리)",
                hint = "normal|Talk|{zone}는 북서쪽이에요. 그린볼런은 느리니까 옆으로 돌아서 치세요!",
                ready = "surprised|Surprised|어? 톱니 소리가 멈췄어요! 여행자님, 혹시…?",
                thanks = "happy|Laugh|새싹들이 다시 쑥쑥 자라요! 올가을엔 땅콩빵을 산더미만큼 구울 수 있겠어요!",
                reward = "normal|Yes|땅콩 팔아서 모은 금화예요. 땅콩빵 나오면 제일 먼저 드릴게요!",
                repeats = new[]
                {
                    "troubled|No|여행자님… 그린볼런들이 또 와글와글 모였어요. 이번엔 허수아비 모자를 톱니로 잘랐어요!",
                    "surprised|Surprised|톱니가 제 꼬리지느러미 바로 옆에 꽂혔어요! 깜짝이야!",
                    "sad|Sad|그린볼런들이 땅콩 새싹으로 블럭 쌓기 놀이를 해요… 제발 도와주세요!",
                },
            });
        }
    }

    /// <summary>토벌 진행 기록: 진행 중 여부 · 잡은 수 · 완료 횟수(반복). 저장 "id=진행:잡은수:완료수"(예: "hunt_bear=1:2:3").</summary>
    public class HuntLog
    {
        class Entry { public bool active; public int count, clears; }
        readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        readonly List<string> order = new List<string>();
        /// <summary>수락·잡음·완료·포기 때마다(id).</summary>
        public event Action<string> Changed;

        Entry E(string id, bool make)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (entries.TryGetValue(id, out var e)) return e; if (!make) return null;
            e = new Entry(); entries[id] = e; order.Add(id); return e;
        }
        public bool IsActive(string id) { var e = E(id, false); return e != null && e.active; }
        public int Count(string id) { var e = E(id, false); return e != null && e.active ? e.count : 0; }
        public int Clears(string id) { var e = E(id, false); return e != null ? e.clears : 0; }
        public bool Ready(string id, int need) => IsActive(id) && Count(id) >= Math.Max(1, need);

        public bool Accept(string id)
        {
            if (string.IsNullOrEmpty(id) || IsActive(id)) return false;
            var e = E(id, true); e.active = true; e.count = 0; Changed?.Invoke(id); return true;
        }
        /// <summary>한 마리 잡음(진행 중이고 아직 덜 잡았을 때만) → true.</summary>
        public bool AddKill(string id, int need)
        {
            var e = E(id, false); if (e == null || !e.active || e.count >= Math.Max(1, need)) return false;
            e.count++; Changed?.Invoke(id); return true;
        }
        /// <summary>다 잡았으면 완료(완료 수 +1, 다시 받을 수 있게 비움).</summary>
        public bool TurnIn(string id, int need)
        {
            if (!Ready(id, need)) return false; var e = E(id, false);
            e.active = false; e.count = 0; e.clears++; Changed?.Invoke(id); return true;
        }
        public bool Abandon(string id)
        {
            var e = E(id, false); if (e == null || !e.active) return false;
            e.active = false; e.count = 0; Changed?.Invoke(id); return true;
        }
        public List<string> Active() { var l = new List<string>(); foreach (var id in order) if (entries[id].active) l.Add(id); return l; }

        public string[] Export()
        {
            var l = new List<string>();
            foreach (var id in order) { var e = entries[id]; if (e.active || e.clears > 0) l.Add($"{id}={(e.active ? 1 : 0)}:{e.count}:{e.clears}"); }
            return l.ToArray();
        }
        public void Import(string[] list)
        {
            Clear(); if (list == null) return;
            foreach (var s in list)
            {
                if (string.IsNullOrEmpty(s)) continue; int k = s.IndexOf('='); if (k <= 0) continue;
                string id = s.Substring(0, k).Trim(); var p = s.Substring(k + 1).Split(':'); if (id.Length == 0 || p.Length != 3) continue;
                if (!int.TryParse(p[0], out int a) || !int.TryParse(p[1], out int c) || !int.TryParse(p[2], out int n) || (a != 0 && a != 1) || c < 0 || n < 0) continue;
                var e = E(id, true); e.active = a == 1; e.count = e.active ? c : 0; e.clears = n;
            }
        }
        public void Clear() { entries.Clear(); order.Clear(); }
    }

    /// <summary>토벌 숫자·글자(순수 C#).</summary>
    public static class HuntMath
    {
        /// <summary>첫 완료 보상 배율.</summary>
        public const float FirstClearBonus = 1.5f;
        /// <summary>경험치 = 기본 × (1 + 12%/레벨) × (처음이면 1.5) — 높은 레벨에서 반복해도 쓸모 있게.</summary>
        public static long Exp(int baseExp, int playerLevel, int clears) => (long)Math.Round(baseExp * (1f + 0.12f * Math.Max(0, playerLevel - 1)) * (clears <= 0 ? FirstClearBonus : 1f));
        /// <summary>금화 = 기본 × (1 + 6%/레벨) × (처음이면 1.5).</summary>
        public static int Gold(int baseGold, int playerLevel, int clears) => (int)Math.Round(baseGold * (1f + 0.06f * Math.Max(0, playerLevel - 1)) * (clears <= 0 ? FirstClearBonus : 1f));
        /// <summary>구역 몬스터 레벨 = max(구역 최소, 내 레벨 - 1 + 흔들림(-1~1)), 최소 1.</summary>
        public static int ZoneLevel(int minLevel, int playerLevel, int variance) => Math.Max(1, Math.Max(minLevel, playerLevel - 1 + variance));
        public static string Progress(string monster, int n, int need) => $"{monster} {Math.Max(0, Math.Min(n, need))}/{need}";

        /// <summary>대사 글자 채우기: {n} {need} {monster} {zone} {giver}.</summary>
        public static string Fill(string text, HuntDef h, int n)
        {
            if (string.IsNullOrEmpty(text) || h == null) return text ?? "";
            return text.Replace("{n}", n.ToString()).Replace("{need}", h.need.ToString()).Replace("{monster}", h.monsterName ?? "")
                       .Replace("{zone}", h.zoneName ?? "").Replace("{giver}", h.giver ?? "");
        }
        /// <summary>"표정|동작|글" 나누기(모자라면 normal · Talk).</summary>
        public static (string face, string anim, string text) Split(string line)
        {
            if (string.IsNullOrEmpty(line)) return ("normal", "Talk", "");
            var p = line.Split(new[] { '|' }, 3);
            return p.Length >= 3 ? (p[0], p[1], p[2]) : ("normal", "Talk", p[p.Length - 1]);
        }
        /// <summary>8방위 글자(Blender 좌표: +y = 북, +x = 동). 아주 가까우면 "".</summary>
        public static string Compass(float dx, float dy)
        {
            if (dx * dx + dy * dy < 1f) return "";
            double a = Math.Atan2(dx, dy) * 180.0 / Math.PI; if (a < 0) a += 360.0;
            string[] n = { "북", "북동", "동", "남동", "남", "남서", "서", "북서" };
            return n[(int)Math.Round(a / 45.0) % 8];
        }
    }
}
