using UnityEngine;

namespace MoominAction.UI
{
    /// <summary>메뉴 화면의 공통 틀. UIRoot 가 열고 닫고 매 프레임 Tick 을 호출한다.</summary>
    public abstract class MenuPage
    {
        public GameObject root; protected UIRoot ui;
        public virtual void Build(UIRoot ui, Transform canvas) { this.ui = ui; }
        public virtual void Open() { root.SetActive(true); }
        public virtual void Close() { root.SetActive(false); }
        public virtual void Tick() { }
    }
}
