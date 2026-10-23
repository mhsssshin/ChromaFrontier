using System;

namespace ChromaFrontier.Core
{
    /// <summary>
    /// 스킬 카드. 유닛과 달리 전선 규칙 없이 보드 어디에나 쓴다.
    /// 스킬은 턴을 쓰지 않는 보조 행동이다: 한 턴에 한 장까지 쓰고, 그다음 유닛을 놓거나 패 버리기로 턴을 끝낸다.
    /// </summary>
    public enum Skill : byte
    {
        None,
        Warp,      // 공간 왜곡: 타일 2개를 통째로 맞바꾼다.
        Lock,      // 영역 잠금: 3x3 에 일정 턴 동안 아무도 유닛을 못 놓는다.
        Bleach,    // 탈색: 유닛 하나를 무속성(회색)으로 — 융합 촉매가 될 수 없다.
        Mine,      // 잠복 지뢰: 아무 빈 칸에나 숨긴다. 상대가 그 칸에 유닛을 놓으면 폭발해 주변을 흡수.
        Transmute, // 원소 변환: 1단계(또는 회색) 유닛 하나를 카드에 적힌 색으로 바꾼다. 융합 판을 짜는 카드.
        Echo,      // 재점화: 내 1단계 유닛 하나가 그 자리에서 기본 효과를 한 번 더 낸다.
        Beacon,    // 거점: 내 땅에 깃발을 꽂는다. 라운드마다 주변 한 칸을 점유하고, 그 칸을 빼앗기면 사라진다.
        Surge,     // 원소 폭주: 내 유닛(1~2단계·3단계) 주변 3x3 을 차지한다. 유닛은 남는다. (재점화를 대신해 상점에서 판다)
        Siphon,    // 흡수: 상대 1·2단계 유닛 하나를 내 것으로 빼앗는다 (칸도 함께). 융합 재료로 쓰는 카드.
        Barrier,   // 방벽: 내 땅 3x3 에 보호막을 친다 (상대가 못 바꿈).
        Split,     // 분열: 내 1단계 유닛이 옆 빈 칸에 같은 원소로 복제된다. 공명·융합 판을 짜는 카드.
        Quake,     // 지진: 3x3 안의 상대 유닛을 모두 부순다 (촉매를 없앤다).
    }

    /// <summary>덱의 카드 한 장: 1단계 원소 유닛이거나 스킬.</summary>
    public readonly struct Card : IEquatable<Card>
    {
        public readonly Element Element;
        public readonly Skill Skill;

        private Card(Element element, Skill skill)
        {
            Element = element;
            Skill = skill;
        }

        public static Card Unit(Element e) => new(e, Skill.None);
        public static Card Of(Skill s) => new(Element.None, s);

        /// <summary>원소 변환 카드: 바꿀 색을 카드가 정한다.</summary>
        public static Card Transmute(Element to) => new(to, Skill.Transmute);

        public bool IsUnit => Skill == Skill.None;

        public bool Equals(Card o) => Element == o.Element && Skill == o.Skill;
        public override bool Equals(object obj) => obj is Card c && Equals(c);
        public override int GetHashCode() => (int)Element * 16 + (int)Skill;
        public override string ToString() => IsUnit ? Element.ToString() : Skill == Skill.Transmute ? $"Transmute({Element})" : Skill.ToString();
    }
}
