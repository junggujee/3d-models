using UnityEngine;
using UnityEngine.UI;

namespace MoominAction
{
    /// <summary>
    /// 바닷길 문(마을 남쪽 성벽 안쪽 ↔ 선착장). 문 앞에서 "F  …" → 화면이 어두워졌다 밝아지며 건너편 문 앞으로(펫도 함께, 카메라는 뒤에서).
    /// 맵이 성벽 + 산맥으로 막혀 있어 걸어서는 바다에 못 감 → 이 문으로 오감. 웨이브 전투 중에는 못 씀.
    /// 머리 위 이름표(가까이 가면 보임). Seaside 가 Configure.
    /// </summary>
    public class SeaGate : MonoBehaviour
    {
        Vector3 front, dest, look; string prompt, label; bool toHarbor; Transform player, sign; CanvasGroup signGroup;
        /// <summary>다른 곳(성 안 등)에 쓸 때: 도착 알림 · 지나갈 수 있는지 · 못 지나갈 때 알림 · 지난 뒤 할 일.</summary>
        public string arriveText, blockedText; public System.Func<bool> canUse; public System.Action onArrive; public float signHeight = 4.4f;

        public void Configure(Vector3 frontPoint, Vector3 destination, Vector3 lookAt, string promptText, string labelText, bool goesToHarbor)
        {
            front = frontPoint; dest = destination; look = lookAt; prompt = promptText; label = labelText; toHarbor = goesToHarbor; BuildSign();
        }

        void Update()
        {
            if (UIState.Paused || SceneFader.Busy) return;
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return; player = p.transform; }
            Vector3 d = player.position - front; d.y = 0; float dist = d.magnitude;
            if (dist < 2.8f && Mathf.Abs(player.position.y - front.y) < 2.5f) Interaction.Offer(this, dist, prompt, Use);
            if (signGroup != null)
            {
                signGroup.alpha = Mathf.MoveTowards(signGroup.alpha, dist < 18f ? 1f : 0f, Time.deltaTime * 2f);
                var cam = Camera.main; if (cam != null && signGroup.alpha > 0f) sign.rotation = Quaternion.LookRotation(sign.position - cam.transform.position);
            }
        }

        void Use()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.InWave) { Toast.Show(arriveText != null ? "전투 중에는 지나갈 수 없어요" : "전투 중에는 바닷길을 지날 수 없어요"); return; }
            if (canUse != null && !canUse()) { if (!string.IsNullOrEmpty(blockedText)) Toast.Show(blockedText); return; }
            if (player == null) return; var t = player;
            SceneFader.Cover(() =>
            {
                var cc = t.GetComponent<CharacterController>(); if (cc != null) cc.enabled = false;
                Vector3 f = look - dest; f.y = 0; t.SetPositionAndRotation(dest + Vector3.up * 0.05f, f.sqrMagnitude > 0.01f ? Quaternion.LookRotation(f) : t.rotation);
                if (cc != null) cc.enabled = true; Physics.SyncTransforms();
                if (t.TryGetComponent<PlayerController>(out var pc)) pc.ResetFall();
                var pet = PetSeal.Current; if (pet != null && pet.command == PetCommand.Follow) pet.WarpNear(t.position - t.forward * 1.5f);
                var cam = Camera.main; if (cam != null && cam.TryGetComponent<ThirdPersonCamera>(out var tpc)) tpc.SnapBehind();
                Toast.Show(arriveText != null ? arriveText : toHarbor ? "남쪽 선착장 — 짭조름한 바닷바람이 분다" : "조랭하프 마을로 돌아왔다");
                onArrive?.Invoke();
            });
        }

        void BuildSign()
        {
            if (sign != null || string.IsNullOrEmpty(label)) return;
            var go = new GameObject("GateSign", typeof(RectTransform)); go.transform.SetParent(transform, false); go.transform.position = front + Vector3.up * signHeight;
            var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace; var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(600, 120); rt.localScale = Vector3.one * 0.008f;
            signGroup = go.AddComponent<CanvasGroup>(); signGroup.alpha = 0f;
            var tgo = new GameObject("Text", typeof(RectTransform)); tgo.transform.SetParent(go.transform, false); ((RectTransform)tgo.transform).sizeDelta = new Vector2(600, 120);
            var tx = tgo.AddComponent<Text>(); tx.font = UI.UITheme.HeadingFont; tx.fontSize = 60; tx.alignment = TextAnchor.MiddleCenter; tx.color = UI.UITheme.GoldBright; tx.raycastTarget = false;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow; tx.verticalOverflow = VerticalWrapMode.Overflow; tx.text = "~ " + label + " ~";
            var o = tgo.AddComponent<Outline>(); o.effectColor = new Color(0.2f, 0.11f, 0.04f, 0.9f); o.effectDistance = new Vector2(3, -3);
            sign = go.transform;
        }
    }

    /// <summary>화면 아래 짧은 알림(HUD 가 구독). 어디서든 Toast.Show("…").</summary>
    public static class Toast
    {
        public static event System.Action<string> Shown;
        public static void Show(string message) { if (!string.IsNullOrEmpty(message)) Shown?.Invoke(message); }
    }
}
