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

        /// <summary>화면을 보는 팀. PvP 에서 2팀(Side.Ai)이면 내 팀이 늘 파랑으로 보이게 두 팀의 그림·색을 맞바꾼다.</summary>
        public static Side Viewer = Side.Player;

        /// <summary>그림·색을 고를 때 쓰는 쪽: 내 팀은 Player, 상대 팀은 Ai.</summary>
        public static Side Look(Side s) => Viewer == Side.Ai ? s.Opponent() : s;

        /// <summary>내 팀·상대 팀 땅 색 (타일 스킨 색). 점유율 막대 등에 쓴다.</summary>
        public static Color Mine = Player, Foe = Ai;

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.gray;

        public static Color Of(Side s) => Look(s) switch
        {
            Side.Player => Mine,
            Side.Ai => Foe,
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

        /// <summary>조사 '(으)로'를 붙인다: 받침이 있으면 '으로', 없거나 ㄹ 받침이면 '로'.</summary>
        public static string Ro(string word)
        {
            var last = word[word.Length - 1];
            if (last < '가' || last > '힣') return word + "로";
            var jong = (last - '가') % 28;
            return word + (jong == 0 || jong == 8 ? "로" : "으로");
        }

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
            Skill.Transmute => "원소 변환",
            Skill.Echo => "재점화",
            Skill.Beacon => "거점",
            _ => "",
        };

        public static string Name(Card c) => c.IsUnit ? Name(c.Element) : c.Skill == Skill.Transmute ? $"{Name(c.Element)} 변환" : Name(c.Skill);

        /// <summary>카드 아트 칸에 크게 쓰는 한 글자 (Pretendard 에 있는 한글만).</summary>
        public static string Glyph(Card c) => c.IsUnit
            ? Name(c.Element).Substring(0, 1)
            : c.Skill switch { Skill.Warp => "왜", Skill.Lock => "잠", Skill.Bleach => "탈", Skill.Mine => "뢰", Skill.Transmute => "변", Skill.Echo => "점", Skill.Beacon => "깃", _ => "?" };

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
                Skill.Warp => "두 칸 맞바꾸기",
                Skill.Lock => "3x3 배치 금지",
                Skill.Bleach => "유닛 무력화",
                Skill.Mine => "빈 칸 어디든 함정",
                Skill.Transmute => $"유닛을 {Ro(Name(c.Element))}",
                Skill.Echo => "내 유닛 효과 한 번 더",
                Skill.Beacon => "라운드마다 1칸 확장",
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
                Skill.Transmute => Of(c.Element),
                Skill.Echo => new Color(1f, 0.62f, 0.25f),
                Skill.Beacon => new Color(0.95f, 0.78f, 0.3f),
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
                Skill.Transmute => $"{Ro(Name(c.Element))} 바꿀 유닛을 누르세요. 상대 유닛도 바꿀 수 있어요.",
                Skill.Echo => "효과를 한 번 더 낼 내 1단계 유닛을 누르세요.",
                Skill.Beacon => "깃발을 꽂을 내 땅을 누르세요. 라운드마다 주변 한 칸을 차지하고, 빼앗기면 무너져요.",
                _ => "",
            };

        /// <summary>스킬을 쓴 뒤 안내: 스킬은 턴을 쓰지 않는다.</summary>
        public const string AfterSkill = "스킬은 턴을 쓰지 않아요. 이제 유닛을 놓으세요 (스킬은 한 턴에 한 장).";
    }
}
