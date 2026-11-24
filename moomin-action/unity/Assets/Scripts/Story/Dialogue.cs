using System;
using System.Collections.Generic;

namespace MoominAction
{
    /// <summary>대사 한 줄: 누가 · 초상화 표정 · NPC 동작 · 글. choices 가 있으면 선택지(대화창 오른쪽 글상자 위 버튼).</summary>
    public class DialogueLine
    {
        public string label, speaker, face = "normal", anim = "Talk", text;
        public DialogueChoice[] choices;
        /// <summary>이 줄이 뜰 때 할 일(퀘스트 수락 등).</summary>
        public Action onShow;
        /// <summary>다음으로 갈 줄 이름(null = 바로 다음 줄). end = 이 줄에서 끝.</summary>
        public string next; public bool end;
    }

    /// <summary>선택지: 글 · 고르면 할 일 · 갈 줄. primary = 강조(수락·건네주기).</summary>
    public class DialogueChoice
    {
        public string text, next; public Func<bool> act; public string failNext; public bool primary;
    }

    /// <summary>대화 한 편: 위 띠 제목(퀘스트·장소) · 왼쪽 초상화 주인 · 줄 목록. 끝나면 onEnd.</summary>
    public class Dialogue
    {
        public string title, speaker, portrait = "joraeng";
        public readonly List<DialogueLine> lines = new List<DialogueLine>();
        public Action onEnd;
        /// <summary>대사에 맞춰 동작할 NPC(없으면 null).</summary>
        public Npc npc;

        public DialogueLine Say(string face, string anim, string text) { var l = new DialogueLine { face = face, anim = anim, text = text }; lines.Add(l); return l; }
        public int IndexOf(string label) { if (string.IsNullOrEmpty(label)) return -1; for (int i = 0; i < lines.Count; i++) if (lines[i].label == label) return i; return -1; }
    }

    public static class DialogueExt
    {
        public static DialogueLine Label(this DialogueLine l, string label) { l.label = label; return l; }
        public static DialogueLine Then(this DialogueLine l, string next) { l.next = next; return l; }
        public static DialogueLine End(this DialogueLine l) { l.end = true; return l; }
        public static DialogueLine OnShow(this DialogueLine l, Action a) { l.onShow = a; return l; }
        public static DialogueLine Choose(this DialogueLine l, params DialogueChoice[] c) { l.choices = c; return l; }
    }
}
