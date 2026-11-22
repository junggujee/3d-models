using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MoominAction
{
    /// <summary>저장 파일 하나(JSON). 필드를 더할 때는 기본값을 두고 version 을 올릴 것(예전 파일도 읽힘).</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string savedAt, scene = "Main", place = "";
        public double playSeconds;
        // 플레이어
        public int level = 1; public long exp; public float maxHealth = 100f, health = 100f; public int sanity;
        public float[] position; public float yaw;
        // 진행
        public int wave, score, kills, bossKills, clashWins, clashLosses, bestPower;
        public Inventory.State inventory;
    }

    /// <summary>
    /// 저장·불러오기. 슬롯 = 기록 1~3(휴식 메뉴에서 수동) + 자동 저장(웨이브를 끝낼 때마다).
    /// 파일: Application.persistentDataPath/saves/slot1.json … auto.json + 같은 이름 .jpg(작은 화면 사진).
    /// 장면 사이 전달: 타이틀/게임 오버에서 Load(slot) → Pending 에 담고 Main 씬을 엶 → Inventory.Awake·GameManager.Start 가 Pending 을 적용.
    /// 저장 내용: 레벨·경험치·체력·정신력·위치·방향·웨이브·점수·여정 기록(RunStats)·가방·플레이 시간·장소 이름.
    /// </summary>
    public static class SaveSystem
    {
        public const string GameScene = "Main", TitleScene = "Title";
        /// <summary>다음 Main 씬에 적용할 저장(새 게임이면 null).</summary>
        public static SaveData Pending;
        /// <summary>이번 여정의 플레이 시간(초). GameManager 가 일시정지 아닐 때만 더함.</summary>
        public static double PlayTime;
        /// <summary>저장했을 때(슬롯) — HUD 알림.</summary>
        public static event Action<int> Saved;
        public const int ThumbW = 384, ThumbH = 216;

        static string Dir => Path.Combine(Application.persistentDataPath, "saves");
        static string JsonPath(int slot) => Path.Combine(Dir, SaveFormat.FileName(slot) + ".json");
        static string ThumbPath(int slot) => Path.Combine(Dir, SaveFormat.FileName(slot) + ".jpg");

        // ── 읽기 ──────────────────────────────
        static readonly Dictionary<int, SaveData> cache = new();
        public static SaveData Peek(int slot)
        {
            if (cache.TryGetValue(slot, out var c)) return c;
            SaveData d = null; var p = JsonPath(slot);
            try { if (File.Exists(p)) d = JsonUtility.FromJson<SaveData>(File.ReadAllText(p)); }
            catch (Exception e) { Debug.LogWarning($"[저장] {SaveFormat.SlotName(slot)} 파일을 읽지 못함: {e.Message}"); d = null; }
            cache[slot] = d; return d;
        }
        public static bool Exists(int slot) => Peek(slot) != null;
        public static bool AnyExists() { for (int i = 0; i < SaveFormat.SlotCount; i++) if (Exists(i)) return true; return false; }
        public static int LatestSlot()
        {
            var list = new List<(int, string)>();
            for (int i = 0; i < SaveFormat.SlotCount; i++) { var d = Peek(i); if (d != null) list.Add((i, d.savedAt)); }
            return SaveFormat.Latest(list);
        }

        static readonly Dictionary<int, Texture2D> thumbs = new();
        /// <summary>슬롯의 화면 사진(없으면 null).</summary>
        public static Texture2D Thumbnail(int slot)
        {
            if (thumbs.TryGetValue(slot, out var t) && t != null) return t;
            var p = ThumbPath(slot); if (!File.Exists(p)) return null;
            try { t = new Texture2D(2, 2, TextureFormat.RGB24, false); t.LoadImage(File.ReadAllBytes(p)); t.wrapMode = TextureWrapMode.Clamp; thumbs[slot] = t; return t; }
            catch { return null; }
        }
        static void Forget(int slot) { cache.Remove(slot); if (thumbs.TryGetValue(slot, out var t) && t != null) UnityEngine.Object.Destroy(t); thumbs.Remove(slot); }

        // ── 쓰기 ──────────────────────────────
        /// <summary>지금 상태를 저장할 수 있는가(이유). 전투 중(웨이브 진행)·쓰러짐·지도 같은 상황에는 저장 불가.</summary>
        public static bool CanSave(out string reason)
        {
            reason = ""; var gm = GameManager.Instance;
            if (gm == null || gm.player == null) { reason = "저장할 수 없는 곳이다"; return false; }
            if (gm.IsOver || !gm.player.IsAlive) { reason = "쓰러진 뒤에는 저장할 수 없다"; return false; }
            if (gm.InWave) { reason = "전투 중에는 저장할 수 없다"; return false; }
            return true;
        }

        public static bool Save(int slot)
        {
            var d = Capture(); if (d == null) return false;
            try
            {
                Directory.CreateDirectory(Dir);
                string p = JsonPath(slot), tmp = p + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(d, true));   // 임시 파일에 다 쓴 뒤 바꿔 끼움(쓰다가 꺼져도 예전 저장은 남음)
                if (File.Exists(p)) File.Delete(p);
                File.Move(tmp, p);
                var jpg = CaptureThumbnail(); if (jpg != null) File.WriteAllBytes(ThumbPath(slot), jpg);
            }
            catch (Exception e) { Debug.LogError($"[저장] {SaveFormat.SlotName(slot)} 저장 실패: {e.Message}"); return false; }
            Forget(slot); cache[slot] = d; Saved?.Invoke(slot); return true;
        }

        public static void Delete(int slot)
        {
            try { if (File.Exists(JsonPath(slot))) File.Delete(JsonPath(slot)); if (File.Exists(ThumbPath(slot))) File.Delete(ThumbPath(slot)); }
            catch (Exception e) { Debug.LogWarning("[저장] 지우기 실패: " + e.Message); }
            Forget(slot); cache[slot] = null;
        }

        /// <summary>지금 게임 상태 → 저장 데이터.</summary>
        public static SaveData Capture()
        {
            var gm = GameManager.Instance; if (gm == null || gm.player == null) return null;
            var p = gm.player; var tf = p.transform;
            var d = new SaveData
            {
                savedAt = SaveFormat.Stamp(DateTime.UtcNow), scene = GameScene, playSeconds = PlayTime, place = MapPlaces.NearestName(tf.position),
                maxHealth = p.maxHealth - p.equipHealth, health = p.currentHealth,   // 최대 체력은 장비 보너스를 뺀 값(불러올 때 장비가 다시 더함) sanity = p.currentSanity,
                position = new[] { tf.position.x, tf.position.y, tf.position.z }, yaw = tf.eulerAngles.y,
                wave = gm.Wave, score = gm.Score,
                kills = RunStats.Kills, bossKills = RunStats.BossKills, clashWins = RunStats.ClashWins, clashLosses = RunStats.ClashLosses, bestPower = RunStats.BestPower,
            };
            if (p.TryGetComponent<Experience>(out var xp)) { d.level = xp.level; d.exp = xp.exp; }
            if (p.TryGetComponent<Inventory>(out var inv)) d.inventory = inv.Export();
            return d;
        }

        /// <summary>Pending 을 지금 씬에 적용(GameManager.Start 가 부름). 가방은 Inventory.Awake 가 먼저 가져감.</summary>
        public static void ApplyPending(GameManager gm)
        {
            var d = Pending; Pending = null;
            if (d == null) { RunStats.Reset(); return; }
            PlayTime = d.playSeconds;
            RunStats.Kills = d.kills; RunStats.BossKills = d.bossKills; RunStats.ClashWins = d.clashWins; RunStats.ClashLosses = d.clashLosses; RunStats.BestPower = d.bestPower;
            gm.RestoreProgress(d.wave, d.score);
            var p = gm.player; if (p == null) return;
            if (p.TryGetComponent<Experience>(out var xp)) { xp.level = Mathf.Max(1, d.level); xp.exp = Math.Max(0, d.exp); xp.Refresh(); }
            if (d.maxHealth > 0) { p.maxHealth = d.maxHealth; p.equipHealth = 0f; }   // 장비 체력은 Inventory.Start 의 ApplyEquipment 가 다시 더함
            p.currentHealth = d.health > 0 ? d.health : p.maxHealth;   // 장비 체력까지 포함한 값일 수 있음 — Inventory.ApplyEquipment(처음) 가 최대치에 맞춰 자름
            p.currentSanity = Mathf.Clamp(d.sanity, p.minSanity, p.maxSanity);
            if (d.position != null && d.position.Length == 3)
            {
                var pos = new Vector3(d.position[0], d.position[1], d.position[2]);
                bool hadCc = p.TryGetComponent<CharacterController>(out var cc) && cc.enabled;
                if (hadCc) cc.enabled = false;   // CharacterController 는 켜진 채로 옮기면 원래 자리로 돌아감
                p.transform.SetPositionAndRotation(pos + Vector3.up * 0.05f, Quaternion.Euler(0, d.yaw, 0));
                if (hadCc) cc.enabled = true;
            }
        }

        // ── 장면 이동 ──────────────────────────
        /// <summary>새 게임: 저장 없이 Main 씬(처음 위치·레벨 1·처음 가방).</summary>
        public static void StartNewGame() { Pending = null; PlayTime = 0; RunStats.Reset(); SceneFader.Load(GameScene); }

        /// <summary>슬롯을 불러와 Main 씬으로. 슬롯이 비어 있으면 false.</summary>
        public static bool Load(int slot)
        {
            cache.Remove(slot); var d = Peek(slot); if (d == null) return false;
            Pending = d; SceneFader.Load(string.IsNullOrEmpty(d.scene) ? GameScene : d.scene); return true;
        }

        public static void ToTitle() { Pending = null; SceneFader.Load(TitleScene); }

        /// <summary>
        /// 메인 카메라를 작은 텍스처로 한 번 더 그려 JPG 로(화면 위 UI 는 Screen Space Overlay 라 안 찍힘 → 메뉴가 열린 채 저장해도 깨끗한 풍경).
        /// </summary>
        static byte[] CaptureThumbnail()
        {
            var cam = Camera.main; if (cam == null) return null;
            var rt = RenderTexture.GetTemporary(ThumbW, ThumbH, 24); var prevTarget = cam.targetTexture; var prevActive = RenderTexture.active;
            Texture2D tex = null;
            try
            {
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                tex = new Texture2D(ThumbW, ThumbH, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, ThumbW, ThumbH), 0, 0); tex.Apply();
                return tex.EncodeToJPG(82);
            }
            catch (Exception e) { Debug.LogWarning("[저장] 화면 사진 실패: " + e.Message); return null; }
            finally
            {
                cam.targetTexture = prevTarget; RenderTexture.active = prevActive; RenderTexture.ReleaseTemporary(rt);
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
        }
    }
}
