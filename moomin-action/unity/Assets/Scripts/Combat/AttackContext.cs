using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 공격 하나(= 코인 하나)의 결과(명세 v2 4-2). 코인을 던지는 순간 만들어지고, 같은 공격의 피해·클래시 계산은 모두 이 값을 씀.
    /// hitTargets 로 한 공격에서 같은 대상에게 피해가 중복으로 들어가지 않게 함.
    /// </summary>
    public class AttackContext
    {
        public int power; public bool isHeads; public bool isResolved; public bool isHit;
        public SkillDefinition skill; public SkillExecutor owner; public int coinIndex;
        public bool clashed, cancelled; public float activeUntil;
        /// <summary>마지막으로 맞힌 대상과의 상성(+1 유리, -1 불리).</summary>
        public int affinity;
        public readonly HashSet<CharacterStats> hitTargets = new();

        public AttackContext(int power, bool isHeads) { this.power = power; this.isHeads = isHeads; isResolved = true; }
        /// <summary>히트박스가 열려 있는가(클래시는 양쪽이 모두 열려 있을 때만).</summary>
        public bool HitboxOpen => isResolved && !cancelled && Time.time <= activeUntil;
    }
}
