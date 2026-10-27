namespace ChromaFrontier.Core
{
    /// <summary>
    /// 크로마 첨탑 30층. tests/Core.Tests/Tools/PuzzleGen.cs 가 무작위로 만들고 풀이기로 걸러 낸 후보에서 골랐다
    /// (각 층 위 주석은 풀이기가 찾은 해답 하나: 카드@x,y, 물은 →방향, [ ] 안은 일어난 반응). 아래층일수록 작은 판·적은 카드,
    /// 위층일수록 흑색의 추가 턴·스킬·얼음을 섞은 여러 수짜리다. PuzzleTests 가 모든 층이 정확히 Moves 수로 풀리는지 확인한다.
    /// </summary>
    public static class PuzzleCatalog
    {
        public static readonly PuzzleDef[] Floors =
        {
            // Red@1,2[White+]
            new() { Floor = 1, Name = "첫 물들이기", Moves = 1, Hand = "R",
                Hint = "내 2단계 유닛 옆에 빠진 한 색을 놓아 세 색을 모아요. 내 유닛만 쓰면 백색!",
                Map = new[] { ".AAA", "AA.A", "PPAA", "PP.A" },
                Units = new[] { "...y", "....", "G...", "...." } },
            // Red@1,1[Black+!,extra]
            new() { Floor = 2, Name = "검은 흡수", Moves = 1, Hand = "R",
                Hint = "상대의 2단계 유닛도 재료예요. 세 색을 모으면 흑색이 상대 땅을 통째로 삼켜요.",
                Map = new[] { "A.AA", ".AAA", "PPAA", "PPAA" },
                Units = new[] { "....", "..g.", "....", ".R.." } },
            // Blue@1,2→1,0[Black+!,extra]
            new() { Floor = 3, Name = "물로 완성", Moves = 1, Hand = "B",
                Hint = "물 카드로도 세 색을 채울 수 있어요. 상대 유닛에 빠진 색이 무엇인지 보세요.",
                Map = new[] { "AAAA", "AAAA", "PP.A", "PPAA" },
                Units = new[] { "o...", ".o..", "....", "...." } },
            // Red@0,3[Orange]  Red@2,1[Purple!]
            new() { Floor = 4, Name = "불길 두 번", Moves = 2, Hand = "R R",
                Hint = "첫 화염으로 주황을 만들고, 남은 화염으로 상대 유닛을 가로채요.",
                Map = new[] { "..AA", "..AA", "PPAA", "PP.A" },
                Units = new[] { "....", "....", "Y.b.", "...r" } },
            // Red@1,3[res]  Blue@0,1→1,0[res]
            new() { Floor = 5, Name = "공명의 두 줄기", Moves = 2, Hand = "B R",
                Hint = "같은 원소 옆에 놓으면 공명해서 더 넓게 퍼져요. 두 번 모두 공명시켜 보세요.",
                Map = new[] { "AAAA", ".AAA", "...A", "PP.." },
                Units = new[] { "boy.", "....", "....", "R..." } },
            // Red@1,1[Black+!,extra]  Red@2,3[res]
            new() { Floor = 6, Name = "한 수에 두 번", Moves = 1, Hand = "R R",
                Hint = "흑색을 만들면 수를 쓰지 않고 한 번 더 둘 수 있어요.",
                Map = new[] { "AAAA", "..AA", "PP..", "PP.." },
                Units = new[] { ".ob.", "..v.", "....", ".R.." } },
            // Red@1,1[Orange!]  Red@2,1[Orange!]
            new() { Floor = 7, Name = "주황 연쇄", Moves = 2, Hand = "R R",
                Hint = "번개 유닛 옆의 화염은 주황 — 8방향으로 길게 뻗어요.",
                Map = new[] { "..AA", "..AA", "PPAA", "PP.." },
                Units = new[] { "..y.", "..y.", "..y.", "...." } },
            // Blue@2,4→0,-1[Purple]  Blue@2,0→-1,0[Black+!,extra]  Red@2,3[White+]
            new() { Floor = 8, Name = "폭풍에서 흑색으로", Moves = 2, Hand = "R B B",
                Hint = "먼저 판을 넓히고, 흑색으로 얻은 한 수로 백색을 만들어 마무리해요.",
                Map = new[] { "...AA", "..AAA", ".....", "PP...", "PP..." },
                Units = new[] { "...o.", "..v..", ".....", ".Y...", ".R..." } },
            // Blue@1,4→0,-1[res]  Red@3,0[Black+!,extra]  Red@2,4[Purple]
            new() { Floor = 9, Name = "공명 후 흡수", Moves = 2, Hand = "B R R",
                Hint = "공명으로 상대 쪽에 다가간 뒤, 흑색의 추가 턴을 이용해요.",
                Map = new[] { "..AAA", "..AAA", "...AA", "PP..A", "PP..." },
                Units = new[] { "....y", "..og.", ".....", "RB...", "....." } },
            // Blue@1,3→1,0[Green]  Blue@4,3→0,1[Black+!,extra]  Red@1,2[White+]
            new() { Floor = 10, Name = "돌풍의 길", Moves = 2, Hand = "B B R",
                Hint = "돌풍(초록)을 만들어 두면 나중에 세 색을 모으는 재료가 돼요.",
                Map = new[] { "..AAA", "..AAA", "....A", "PPAAA", "PP..." },
                Units = new[] { ".....", ".....", ".....", "...o.", ".Y..." } },
            // Red@2,4[Orange]  Blue@2,3→0,-1[Black+!,extra]  Red@1,0[Orange!]
            new() { Floor = 11, Name = "마그마 협공", Moves = 2, Hand = "B R R",
                Hint = "흑색으로 얻은 한 수로 상대 번개를 가로채 마지막 마그마를 터뜨려요.",
                Map = new[] { "..AAA", "...AA", "..AAA", "P....", "PP..." },
                Units = new[] { "..y..", "...r.", "..v..", ".....", ".Y..." } },
            // Red@2,3[Purple]  Blue@4,3→0,1[Black+!,extra]  Red@0,2[Purple]
            new() { Floor = 12, Name = "두 개의 폭풍", Moves = 2, Hand = "R B R",
                Hint = "폭풍(보라)은 두 칸 거리까지 퍼져요. 처음과 마지막을 폭풍으로.",
                Map = new[] { ".AAAA", "...AA", "....A", "PP...", "PP..." },
                Units = new[] { ".....", "....y", "....o", "BB...", "....." } },
            // Blue@1,4→1,0  Blue@2,3→1,0[Black+!,extra]  Blue@1,3→0,-1[res]
            new() { Floor = 13, Name = "물길 셋", Moves = 2, Hand = "B B B",
                Hint = "첫 물은 길을 닦는 데 쓰세요. 흑색이 남은 상대 땅을 정리해 줘요.",
                Map = new[] { "...AA", "...AA", "..AAA", "...A.", "PP.A." },
                Units = new[] { ".....", ".....", "..o..", ".....", "....." } },
            // Transmute(Yellow)@2,2  Blue@1,2→0,-1[Green!]  Red@2,2[White+]
            new() { Floor = 14, Name = "번개로 바꾸기", Moves = 2, Hand = "B R Ty",
                Hint = "원소 변환으로 재료를 바꾸면 없던 조합이 생겨요.",
                Map = new[] { ".AAAA", "...AA", "..AAA", "PP..A", "PP..." },
                Units = new[] { ".r.r.", ".....", "..b..", ".....", "....." } },
            // Bleach@0,4  Blue@1,4→0,-1[res]  Blue@3,0→0,1[res]
            new() { Floor = 15, Name = "지워야 열린다", Moves = 2, Hand = "B B Bl",
                Hint = "어떤 유닛은 재료로 쓰이면 오히려 손해예요. 탈색으로 반응을 바꿔 보세요.",
                Map = new[] { "....A", "...AA", "..AAA", "..AA.", "PPA.." },
                Units = new[] { "....b", ".....", ".....", "..y..", "Y.b.." } },
            // Blue@0,3→1,0[res]  Echo@0,3  Red@1,0[Black+!,extra]
            new() { Floor = 16, Name = "메아리 물결", Moves = 2, Hand = "B R Ec",
                Hint = "물을 놓은 뒤 재점화로 한 번 더 흘려 보내요. 마무리는 흑색!",
                Map = new[] { ".AAAA", "..AAA", "...AA", "P...A", "PP..." },
                Units = new[] { "..g..", "..r..", ".....", ".....", "B...." } },
            // Blue@1,3→0,-1[res]  Warp@2,1↔3,3  Blue@3,4→0,-1[res]
            new() { Floor = 17, Name = "자리 바꾸기", Moves = 2, Hand = "B B Wp",
                Hint = "공간 왜곡으로 유닛을 옮겨 공명할 자리를 만들어요.",
                Map = new[] { ".AAAA", ".AAAA", ".....", ".....", "PP..." },
                Units = new[] { "...r.", "..b..", ".....", ".....", ".B..." } },
            // Echo@1,4  Red@2,1[Orange!]  Blue@2,2→0,-1[White+]
            new() { Floor = 18, Name = "메아리와 마그마", Moves = 2, Hand = "R B Ec",
                Hint = "재점화로 먼저 땅을 넓혀 두면 놓을 자리가 생겨요.",
                Map = new[] { "AAAAA", ".....", ".....", ".....", "PP..." },
                Units = new[] { "..y..", ".....", ".....", ".....", ".B..." } },
            // Warp@2,2↔1,4  Red@1,2[Orange]  Blue@2,2→0,-1[White+]
            new() { Floor = 19, Name = "왜곡의 묘수", Moves = 2, Hand = "R B Wp",
                Hint = "공간 왜곡으로 유닛을 옮겨 두면, 두 번의 융합이 백색으로 이어져요.",
                Map = new[] { "...AA", "..AAA", "..AA.", "P....", "PP..." },
                Units = new[] { ".....", "...vo", "..r..", ".....", ".Y..." } },
            // Red@0,3[Orange]  Warp@0,3↔3,3  Blue@3,2→0,-1[White+]
            new() { Floor = 20, Name = "옮겨 놓은 마그마", Moves = 2, Hand = "R B B Wp",
                Hint = "주황을 만든 뒤 공간 왜곡으로 그 유닛을 백색을 만들 자리로 보내요.",
                Map = new[] { ".#..AA", "...AAA", "ssAAAA", "P~AAA~", "PP..s." },
                Units = new[] { "......", ".....r", "......", "......", "YB...." } },
            // Red@0,2[Orange]  Blue@3,1→0,-1[Green!]  Red@3,2[White+]
            new() { Floor = 21, Name = "삼원색 계단", Moves = 3, Hand = "R R B Ty",
                Hint = "주황, 초록을 차례로 만들고 마지막 한 색으로 백색을 완성해요.",
                Map = new[] { ".#..AA", "...AAA", "...AAA", "P.#~.A", "PP...." },
                Units = new[] { "....v.", "......", "...yv.", "Y.....", "......" } },
            // Bleach@2,3  Blue@1,3→1,0[res]  Blue@4,1→-1,0[res]  Red@3,0[Black+!,extra]
            new() { Floor = 22, Name = "탈색 후 공명", Moves = 3, Hand = "B B R Bl",
                Hint = "탈색으로 엉뚱한 융합을 막고 공명 두 번, 마무리는 흑색!",
                Map = new[] { ".~.AAA", "s.~AAA", ".#.AA.", "PPAAA.", "PP...." },
                Units = new[] { "....g.", "......", "....b.", "..r.v.", "BB...." } },
            // Echo@1,4  Red@1,1[Purple!]  Blue@4,1→0,1[Black!,extra]  Red@3,1[Orange!]
            new() { Floor = 23, Name = "메아리 연쇄", Moves = 2, Hand = "R R B Ec",
                Hint = "재점화로 놓을 자리를 만든 뒤 상대 유닛을 차례로 가로채요.",
                Map = new[] { ".AAAAA", ".AAAAA", "....AA", "PP#...", "PP#~.#" },
                Units = new[] { ".b.y..", "...y.r", "......", "......", ".B...." } },
            // Transmute(Blue)@1,4  Blue@1,3→1,0[res]  Blue@1,2→0,-1[res]  Blue@1,1→1,0[res]
            new() { Floor = 24, Name = "물의 사다리", Moves = 3, Hand = "B B B Tb",
                Hint = "내 유닛을 물로 바꾸면 물이 줄줄이 공명해요.",
                Map = new[] { ".~.AAA", "..~AA.", "...A..", "P.....", "PP...." },
                Units = new[] { "......", "....y.", "......", "......", ".Y...." } },
            // Warp@2,2↔1,4  Red@3,2[Orange!,slid]  Red@3,2[White+]  Red@0,1
            new() { Floor = 25, Name = "얼음 위의 묘수", Moves = 3, Hand = "R R R Wp",
                Hint = "얼음 칸에서는 한 칸 더 미끄러져요. 공간 왜곡으로 미끄러질 곳을 준비하세요.",
                Map = new[] { "..AAAA", "~.A.AA", ".#.~AA", "P..~.A", "PP...A" },
                Units = new[] { "...b..", "......", ".....y", "......", ".B...." } },
            // Blue@1,4→1,0[res]  Red@3,2[Black+!,extra]  Red@5,1  Transmute(Blue)@5,1  Blue@4,1→-1,0[res]
            new() { Floor = 26, Name = "변환의 마무리", Moves = 3, Hand = "R R B B Tb",
                Hint = "흑색 뒤에 놓은 화염을 물로 바꾸면 마지막 물이 공명해요.",
                Map = new[] { "..AAAA", "...AAA", "~#.As.", "#..AA~", "#..sA.", "PP..#." },
                Units = new[] { "......", "......", "......", "...g..", "......", "BB...." } },
            // Red@2,4  Transmute(Yellow)@2,4  Red@3,4[Orange]  Blue@5,1→0,1[Black+!,extra]  Blue@1,4→0,-1[res]
            new() { Floor = 27, Name = "스스로 만든 재료", Moves = 3, Hand = "R B R B Ty",
                Hint = "내가 놓은 유닛을 원소 변환해 다음 융합의 재료로 삼아요.",
                Map = new[] { ".~.AAA", "~..AAA", ".~#.AA", "...~..", "PP....", "PP...#" },
                Units = new[] { "......", "....o.", "......", "......", "......", ".B...." } },
            // Red@1,3  Blue@2,2→-1,0[res,slid]  Red@3,1[Black!,extra]  Transmute(Blue)@1,3  Blue@1,4→1,0[res]
            new() { Floor = 28, Name = "얼음 미끄럼틀", Moves = 3, Hand = "B B R R Tb",
                Hint = "얼음에 미끄러지는 물로 상대에게 다가가요. 변환은 마지막 공명을 위해 아껴 두세요.",
                Map = new[] { ".AAAAA", "..AAAA", "~.~#AA", ".....A", "PP....", "PP..s#" },
                Units = new[] { ".bb...", "....y.", "......", "......", "......", "......" } },
            // Red@1,4[Purple]  Red@3,3[Orange!]  Warp@3,0↔0,1  Blue@3,2→-1,0[White+]
            new() { Floor = 29, Name = "끌어온 칸", Moves = 3, Hand = "R B R Wp",
                Hint = "공간 왜곡으로 멀리 있는 칸을 내 쪽으로 끌어와요. 마지막은 백색!",
                Map = new[] { "..#AAA", "~.AAAA", "....AA", ".~..AA", "....AA", "PP~.#." },
                Units = new[] { "...v..", "......", "......", "....y.", "......", ".B...." } },
            // Blue@1,5→1,0[Green]  Blue@1,2→1,0[Green!]  Blue@5,2→0,1[Black+!,extra]  Red@2,2[White+]
            new() { Floor = 30, Name = "돌풍의 첨탑", Moves = 3, Hand = "B B R B Tr",
                Hint = "돌풍 두 번으로 판을 흔든 뒤, 흑색의 추가 턴으로 백색을 완성해요.",
                Map = new[] { "...AAA", ".AAA.A", ".AAAAA", "#.A.s.", "PP....", "PP...." },
                Units = new[] { "......", ".g...o", "..y...", "......", "......", "Y....." } },
        };

        public static int Count => Floors.Length;
    }
}
