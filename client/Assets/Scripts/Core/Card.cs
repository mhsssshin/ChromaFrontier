using System;

namespace ChromaFrontier.Core
{
    /// <summary>스킬 카드. 유닛과 달리 전선 규칙 없이 보드 어디에나 쓴다.</summary>
    public enum Skill : byte
    {
        None,
        Warp,   // 공간 왜곡: 타일 2개를 통째로 맞바꾼다. 턴을 소모하지 않는다.
        Lock,   // 영역 잠금: 3x3 에 일정 턴 동안 아무도 유닛을 못 놓는다.
        Bleach, // 탈색: 유닛 하나를 무속성(회색)으로 — 융합 촉매가 될 수 없다.
        Mine,   // 잠복 지뢰: 아무 빈 칸에나 숨긴다. 상대가 그 칸에 유닛을 놓으면 폭발해 주변을 흡수.
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

        public bool IsUnit => Skill == Skill.None;

        public bool Equals(Card o) => Element == o.Element && Skill == o.Skill;
        public override bool Equals(object obj) => obj is Card c && Equals(c);
        public override int GetHashCode() => (int)Element * 16 + (int)Skill;
        public override string ToString() => IsUnit ? Element.ToString() : Skill.ToString();
    }
}
