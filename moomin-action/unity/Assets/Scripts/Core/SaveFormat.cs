using System;
using System.Collections.Generic;
using System.Globalization;

namespace MoominAction
{
    /// <summary>
    /// 저장 슬롯 표시용 글자 만들기(순수 C# — UnityEngine 없음, tools/status_tests 로 검증).
    /// 슬롯 번호: 0~2 = 기록 1~3(수동), 3 = 자동 저장.
    /// </summary>
    public static class SaveFormat
    {
        public const int ManualSlots = 3, AutoSlot = 3, SlotCount = 4;

        public static string SlotName(int slot) => slot == AutoSlot ? "자동 저장" : "기록 " + (slot + 1);
        public static string FileName(int slot) => slot == AutoSlot ? "auto" : "slot" + (slot + 1);

        /// <summary>플레이 시간: 1시간 미만 "12:05", 이상 "3:12:05", 100시간 이상도 그대로 "120:00:00".</summary>
        public static string PlayTime(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 0) seconds = 0;
            long s = (long)Math.Floor(seconds); long h = s / 3600, m = s / 60 % 60, sec = s % 60;
            return h > 0 ? $"{h}:{m:00}:{sec:00}" : $"{m:00}:{sec:00}";
        }

        /// <summary>저장 시각(ISO 8601, UTC "o" 형식) → 내 컴퓨터 시각 "2026.11.22 14:03". 읽을 수 없으면 "".</summary>
        public static string Date(string iso, TimeZoneInfo zone = null)
        {
            if (!TryParse(iso, out var utc)) return "";
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone ?? TimeZoneInfo.Local);
            return local.ToString("yyyy.MM.dd HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>얼마나 전에 저장했는가: "방금", "5분 전", "3시간 전", "2일 전", 그 뒤로는 날짜.</summary>
        public static string Ago(string iso, DateTime nowUtc, TimeZoneInfo zone = null)
        {
            if (!TryParse(iso, out var utc)) return "";
            var d = nowUtc - utc;
            if (d.TotalSeconds < 60) return "방금";
            if (d.TotalMinutes < 60) return (int)d.TotalMinutes + "분 전";
            if (d.TotalHours < 24) return (int)d.TotalHours + "시간 전";
            if (d.TotalDays < 7) return (int)d.TotalDays + "일 전";
            return Date(iso, zone);
        }

        public static string Stamp(DateTime utc) => utc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

        public static bool TryParse(string iso, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrEmpty(iso)) return false;
            if (!DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t)) return false;
            utc = DateTime.SpecifyKind(t, DateTimeKind.Utc); return true;
        }

        /// <summary>합 승률 "62%"(합을 한 번도 안 했으면 "-").</summary>
        public static string WinRate(int wins, int losses)
        {
            int n = wins + losses; if (n <= 0) return "-";
            return (int)Math.Round(100.0 * wins / n, MidpointRounding.AwayFromZero) + "%";
        }

        /// <summary>가장 최근에 저장된 슬롯(없으면 -1). (슬롯, 저장 시각) 목록에서 고름 — 이어하기·"마지막 저장에서 다시".</summary>
        public static int Latest(IEnumerable<(int slot, string savedAt)> saves)
        {
            int best = -1; DateTime bt = DateTime.MinValue;
            foreach (var (slot, at) in saves)
            {
                if (!TryParse(at, out var t)) continue;
                if (best < 0 || t > bt) { best = slot; bt = t; }
            }
            return best;
        }

        /// <summary>슬롯 한 줄 요약: "Lv.12 · 마을 광장 · 웨이브 7".</summary>
        public static string Summary(int level, string place, int wave)
        {
            var parts = new List<string> { "Lv." + Math.Max(1, level) };
            if (!string.IsNullOrEmpty(place)) parts.Add(place);
            parts.Add(wave > 0 ? "웨이브 " + wave : "여정의 시작");
            return string.Join("  ·  ", parts);
        }
    }
}
