namespace MoominAction
{
    /// <summary>에너지 수치(순수 로직): 0 ~ Max 사이로 제한, 부족하면 소비 불가.</summary>
    public class EnergyPool
    {
        public int Max = 100;
        public int Current { get; private set; }
        public void Set(int v) => Current = v < 0 ? 0 : v > Max ? Max : v;
        /// <summary>반환: 실제로 늘어난 양(최대치에서 잘린 만큼 제외).</summary>
        public int Gain(int amount) { if (amount <= 0) return 0; int before = Current; Set(Current + amount); return Current - before; }
        public bool CanSpend(int amount) => amount <= 0 || Current >= amount;
        public bool Spend(int amount) { if (amount <= 0) return true; if (Current < amount) return false; Current -= amount; return true; }
    }
}
