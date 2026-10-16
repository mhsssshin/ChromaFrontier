using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    public static class Palette
    {
        public static readonly Color Player = new(0.27f, 0.56f, 0.98f);
        public static readonly Color Ai = new(0.93f, 0.32f, 0.32f);
        public static readonly Color Neutral = new(0.74f, 0.78f, 0.6f);
        public static readonly Color Background = new(0.36f, 0.62f, 0.86f);

        public static Color Of(Side s) => s switch
        {
            Side.Player => Player,
            Side.Ai => Ai,
            _ => Neutral,
        };

        public static Color Of(Element e) => e switch
        {
            Element.Red => new Color(0.97f, 0.3f, 0.18f),
            Element.Blue => new Color(0.2f, 0.62f, 1f),
            Element.Yellow => new Color(1f, 0.86f, 0.18f),
            Element.Green => new Color(0.3f, 0.86f, 0.38f),
            Element.Purple => new Color(0.66f, 0.36f, 0.92f),
            Element.Orange => new Color(1f, 0.55f, 0.12f),
            Element.White => Color.white,
            Element.Black => new Color(0.14f, 0.1f, 0.2f),
            _ => Color.gray,
        };

        public static string Name(Element e) => e switch
        {
            Element.Red => "화염",
            Element.Blue => "물",
            Element.Yellow => "번개",
            Element.Green => "돌풍",
            Element.Purple => "폭풍",
            Element.Orange => "마그마",
            Element.White => "백색",
            Element.Black => "흑색",
            _ => "무속성",
        };

        /// <summary>같은 원소 공명의 이름.</summary>
        public static string ResonanceName(Element e) => e switch
        {
            Element.Red => "업화",
            Element.Blue => "해일",
            Element.Yellow => "뇌우",
            _ => "",
        };

        public static string Name(Skill s) => s switch
        {
            Skill.Warp => "공간 왜곡",
            Skill.Lock => "영역 잠금",
            Skill.Bleach => "탈색",
            Skill.Mine => "잠복 지뢰",
            _ => "",
        };

        public static string Name(Card c) => c.IsUnit ? Name(c.Element) : Name(c.Skill);

        /// <summary>카드 아트 칸에 크게 쓰는 한 글자 (Pretendard 에 있는 한글만).</summary>
        public static string Glyph(Card c) => c.IsUnit
            ? Name(c.Element).Substring(0, 1)
            : c.Skill switch { Skill.Warp => "왜", Skill.Lock => "잠", Skill.Bleach => "탈", Skill.Mine => "뢰", _ => "?" };

        public static string Hint(Card c) => c.IsUnit
            ? c.Element switch
            {
                Element.Red => "십자 폭발",
                Element.Blue => $"방향 골라 직선 {Balance.BlueLength}칸",
                Element.Yellow => $"무작위 {Balance.YellowBolts}칸",
                _ => "",
            }
            : c.Skill switch
            {
                Skill.Warp => "두 칸 교환·턴 유지",
                Skill.Lock => "3x3 배치 금지",
                Skill.Bleach => "유닛 무력화",
                Skill.Mine => "빈 칸 어디든 함정",
                _ => "",
            };

        public static Color Of(Card c) => c.IsUnit
            ? Of(c.Element)
            : c.Skill switch
            {
                Skill.Warp => new Color(0.52f, 0.4f, 0.86f),
                Skill.Lock => new Color(0.38f, 0.44f, 0.56f),
                Skill.Bleach => new Color(0.66f, 0.66f, 0.7f),
                Skill.Mine => new Color(0.3f, 0.28f, 0.32f),
                _ => Color.gray,
            };

        /// <summary>대상 선택 안내 문구.</summary>
        public static string TargetPrompt(Card c, bool warpSecond) => c.IsUnit
            ? c.Element == Element.Blue
                ? "밝게 빛나는 칸을 누른 뒤, 물이 흐를 방향을 고르세요."
                : "밝게 빛나는 칸(내 영토나 맞닿은 빈 칸)을 누르세요."
            : c.Skill switch
            {
                Skill.Warp => warpSecond ? "맞바꿀 두 번째 칸을 누르세요. (첫 칸을 다시 누르면 취소)" : "맞바꿀 첫 번째 칸을 누르세요. 보드 어디든 됩니다.",
                Skill.Lock => "잠글 3x3 구역의 가운데 칸을 누르세요.",
                Skill.Bleach => "무력화할 유닛을 누르세요.",
                Skill.Mine => "지뢰를 묻을 빈 칸을 누르세요. 상대가 밟으면 주변이 내 땅! (상대에게 안 보임)",
                _ => "",
            };
    }
}
