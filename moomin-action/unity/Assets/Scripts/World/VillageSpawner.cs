using UnityEngine;
using UnityEngine.AI;

namespace MoominAction
{
    /// <summary>
    /// 조랭하프족 마을 사람들(GameManager 가 NavMesh 를 구운 뒤 붙임).
    ///  · 조랭이(퀘스트 「땅콩빵이 먹고 싶은 조랭이」): 시작 위치 가까이 — 처음부터 머리 위 "!" 가 보이게
    ///  · 마을 주민 몽글이·포실이: 마을 광장(MapPlaces "plaza", 없으면 시작 위치 근처)에서 잡담
    /// 같은 프리팹(Joraeng)에 이름·목도리 색·대사만 다르게.
    /// </summary>
    public class VillageSpawner : MonoBehaviour
    {
        public GameObject npcPrefab;

        void Start()
        {
            if (npcPrefab == null) return;
            var player = GameObject.FindGameObjectWithTag("Player"); Vector3 start = player != null ? player.transform.position : Vector3.zero;
            Vector3 fwd = player != null ? player.transform.forward : Vector3.forward; fwd.y = 0; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward; fwd.Normalize();
            var joraeng = Spawn(start + fwd * 6f + Vector3.Cross(Vector3.up, fwd) * 2.5f, start, "조랭이", new Color(0.86f, 0.3f, 0.27f), Color.white);
            if (joraeng != null) { joraeng.questId = Quests.PeanutCraving; joraeng.wanderRadius = 1.2f; joraeng.place = "조랭하프 마을 어귀"; }

            Vector3 plaza = MapPlaces.TryWorldPosition("plaza", out var pz) ? pz : start + fwd * 14f;
            var a = Spawn(plaza + new Vector3(2.5f, 0, 1f), start, "몽글이", new Color(0.33f, 0.52f, 0.82f), new Color(0.97f, 0.97f, 1f));
            if (a != null) a.chatter = new[]
            {
                "troubled|No|요즘 마을 밖에 핑크색 말랑이가 통통 튀어 다녀요. 몸통박치기가 꽤 아프니까 조심하세요!",
                "normal|Talk|그린볼런은 톱니를 날려요. 날아오는 톱니는 같은 원거리 공격으로만 맞받아칠 수 있대요.",
                "happy|Laugh|여행자님 털이 복슬복슬하네요! 우리 조랭하프족만큼은 아니지만요. 헤헤.",
            };
            var b = Spawn(plaza + new Vector3(-2f, 0, -1.5f), start, "포실이", new Color(0.93f, 0.74f, 0.3f), new Color(1f, 0.96f, 0.9f));
            if (b != null) b.chatter = new[]
            {
                "sad|Sad|성 뒤쪽 산은 너무 높아서 우리 조랭하프족은 못 올라가요. 지느러미가 짧거든요…",
                "happy|Laugh|하프물범은 땅콩빵 조각을 정말 좋아해요. 세 번 주면 친구가 된대요!",
                "surprised|Surprised|어머, 방금 조랭이가 배에서 꼬르륵 소리를 냈어요. 마을 어귀에 있을 거예요!",
            };
        }

        Npc Spawn(Vector3 p, Vector3 lookAt, string name, Color scarf, Color fur)
        {
            if (!NavMesh.SamplePosition(p, out var hit, 6f, NavMesh.AllAreas)) { if (!NavMesh.SamplePosition(p, out hit, 20f, NavMesh.AllAreas)) return null; }
            var dir = lookAt - hit.position; dir.y = 0;
            var go = Instantiate(npcPrefab, hit.position, dir.sqrMagnitude > 0.01f ? Quaternion.LookRotation(dir) : Quaternion.identity); go.name = "NPC_" + name;
            if (!go.TryGetComponent<Npc>(out var npc)) npc = go.AddComponent<Npc>();
            npc.npcName = name; npc.scarfColor = scarf; npc.furTint = fur; npc.ApplyLook(); return npc;
        }
    }
}
