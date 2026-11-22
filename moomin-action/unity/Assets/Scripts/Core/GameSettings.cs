using UnityEngine;

namespace MoominAction
{
    /// <summary>설정값(PlayerPrefs 저장). 설정 메뉴가 바꾸고, 카메라 등이 읽음.</summary>
    public static class GameSettings
    {
        public static float MasterVolume = 0.8f, MouseSensitivity = 1f;
        public static bool InvertY, ShowHud = true, VSync = true, Fullscreen = true, ShowDamageHints = true, CameraShake = true;
        public static int Quality = -1;
        const string Key = "moomin.settings";
        [System.Serializable] class Data { public float vol = 0.8f, sens = 1f; public bool inv, hud = true, vsync = true, full = true; public int q = -1; }

        public static void Load()
        {
            var d = new Data(); try { var j = PlayerPrefs.GetString(Key, ""); if (!string.IsNullOrEmpty(j)) d = JsonUtility.FromJson<Data>(j); } catch { }
            MasterVolume = d.vol; MouseSensitivity = d.sens; InvertY = d.inv; ShowHud = d.hud; VSync = d.vsync; Fullscreen = d.full; Quality = d.q < 0 ? QualitySettings.GetQualityLevel() : d.q; Apply();
        }
        public static void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(new Data { vol = MasterVolume, sens = MouseSensitivity, inv = InvertY, hud = ShowHud, vsync = VSync, full = Fullscreen, q = Quality })); PlayerPrefs.Save(); }
        public static void Reset() { MasterVolume = 0.8f; MouseSensitivity = 1f; InvertY = false; ShowHud = true; VSync = true; Fullscreen = true; Quality = QualitySettings.GetQualityLevel(); Apply(); }
        public static void Apply()
        {
            AudioListener.volume = MasterVolume; QualitySettings.vSyncCount = VSync ? 1 : 0;
            if (Quality >= 0 && Quality < QualitySettings.names.Length) QualitySettings.SetQualityLevel(Quality, true);
            Screen.fullScreen = Fullscreen;
        }
    }
}
