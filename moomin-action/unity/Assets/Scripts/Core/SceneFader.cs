using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MoominAction
{
    /// <summary>
    /// 장면 바꾸기 + 검은 화면으로 서서히 어두워졌다 밝아짐(타이틀 ↔ 게임). 처음 부를 때 스스로 만들어지고 장면이 바뀌어도 남음.
    /// 빌드 설정에 없는 장면이면 지금 장면을 다시 엶(타이틀 씬을 아직 만들지 않았을 때).
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        static SceneFader inst; Image black; bool busy;
        public const float OutTime = 0.7f, InTime = 0.9f;
        public static bool Busy => inst != null && inst.busy;

        static SceneFader Get()
        {
            if (inst != null) return inst;
            var go = new GameObject("SceneFader"); DontDestroyOnLoad(go); inst = go.AddComponent<SceneFader>();
            var canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            var img = new GameObject("Black", typeof(RectTransform), typeof(Image)); img.transform.SetParent(go.transform, false);
            var r = (RectTransform)img.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            inst.black = img.GetComponent<Image>(); inst.black.color = new Color(0, 0, 0, 0); inst.black.raycastTarget = false;
            return inst;
        }

        public static void Load(string scene) { var f = Get(); if (f.busy) return; f.StartCoroutine(f.Run(scene)); }

        /// <summary>장면이 열릴 때 검은 화면에서 밝아지기만(타이틀 첫 등장).</summary>
        public static void FadeIn(float seconds = 1.6f) { var f = Get(); if (f.busy) return; f.StartCoroutine(f.In(seconds)); }

        /// <summary>같은 장면 안에서 순간이동할 때: 어두워짐 → mid(옮기기) → 밝아짐(바닷길 문).</summary>
        public static void Cover(System.Action mid, float outTime = 0.45f, float inTime = 0.6f) { var f = Get(); if (f.busy) return; f.StartCoroutine(f.CoverRun(mid, outTime, inTime)); }
        IEnumerator CoverRun(System.Action mid, float outTime, float inTime)
        {
            busy = true; black.raycastTarget = true;
            for (float t = 0; t < outTime; t += Time.unscaledDeltaTime) { black.color = new Color(0, 0, 0, t / outTime); yield return null; }
            black.color = Color.black;
            try { mid?.Invoke(); } catch (System.Exception e) { Debug.LogException(e); }
            yield return null; yield return null;
            yield return In(inTime);
        }

        IEnumerator Run(string scene)
        {
            busy = true; black.raycastTarget = true;
            float a0 = black.color.a;
            for (float t = 0; t < OutTime; t += Time.unscaledDeltaTime) { black.color = new Color(0, 0, 0, Mathf.Lerp(a0, 1, t / OutTime)); yield return null; }
            black.color = Color.black; UIState.Reset();
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else { Debug.LogWarning($"[장면] '{scene}' 이 빌드 설정에 없어 지금 장면을 다시 엽니다. 메뉴 Moomin ▸ 5. 타이틀 씬 만들기 를 실행하세요."); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
            yield return null; yield return null;   // 새 장면의 Start 가 끝날 때까지(NavMesh 굽기 등 첫 프레임이 무거움)
            yield return In(InTime);
        }

        IEnumerator In(float seconds)
        {
            busy = true; black.color = Color.black;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime) { float u = t / seconds; black.color = new Color(0, 0, 0, 1 - u * u * (3 - 2 * u)); yield return null; }
            black.color = new Color(0, 0, 0, 0); black.raycastTarget = false; busy = false;
        }
    }
}
