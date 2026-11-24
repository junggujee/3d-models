using UnityEngine;

namespace MoominAction.UI
{
    /// <summary>메뉴를 열고 닫는 쪽(게임 안 = UIRoot, 타이틀 = TitleScreen). 페이지는 이것만 알면 어디서나 쓸 수 있음.</summary>
    public interface IMenuHost
    {
        void Open(MenuPage page);
        void Back();
    }

    /// <summary>메뉴 화면의 공통 틀. 주인(IMenuHost)이 열고 닫고 매 프레임 Tick 을 호출한다.</summary>
    public abstract class MenuPage
    {
        public GameObject root;
        /// <summary>게임 안 UI(타이틀에서는 null — 게임 전용 페이지만 씀).</summary>
        protected UIRoot ui;
        /// <summary>열고 닫는 쪽(게임 안이면 ui 와 같음).</summary>
        protected IMenuHost host;
        public virtual void Build(UIRoot ui, Transform canvas) { this.ui = ui; if (ui != null) host = ui; }
        /// <summary>타이틀처럼 UIRoot 가 없는 곳에서 만들기.</summary>
        public void BuildFor(IMenuHost h, Transform canvas) { host = h; Build(null, canvas); host = h; }
        public virtual void Open() { root.SetActive(true); }
        public virtual void Close() { root.SetActive(false); }
        public virtual void Tick() { }
        /// <summary>Esc 를 페이지 안에서 처리했으면 true(예: 위에 뜬 상세 창만 닫기) — 아니면 주인이 페이지를 닫음.</summary>
        public virtual bool HandleCancel() => false;
    }
}
