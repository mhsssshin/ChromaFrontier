using System.Collections.Generic;
using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>처음 만나는 맵 기믹·보스를 설명하는 팁 카드. Pictures 는 그림마다 겹쳐 그릴 텍스처 층.</summary>
    public sealed class TipDef
    {
        public string Id, Title, Body;
        public string[][] Pictures;
    }

    /// <summary>
    /// 튜토리얼과 상황 팁.
    ///   · 튜토리얼: TutorialCatalog 의 레슨을 차례로. 놓을 수 있는 칸은 레슨의 목표 칸으로만 좁히고, AI 는 두지 않는다.
    ///     처음 실행하면 자동으로 시작, 대기실 '배우기'로 다시 볼 수 있다. 끝나면 1-1 로 바로 이어진다.
    ///   · 팁: 스테이지를 시작할 때 처음 보는 챕터 기믹이나 보스가 있으면 설명 카드를 띄운다 (한 번씩만).
    /// </summary>
    public sealed partial class GameBootstrap
    {
        public bool Tutorial { get; private set; }
        public int LessonIndex { get; private set; }
        public bool LessonDone { get; private set; }
        public Lesson Lesson => TutorialCatalog.Lessons[LessonIndex];

        private readonly Queue<TipDef> _tips = new();
        public TipDef ActiveTip => _tips.Count > 0 ? _tips.Peek() : null;

        private static readonly TipDef[] Tips =
        {
            new()
            {
                Id = "ch2", Title = "화산 지대",
                Body = "몇 라운드마다 화산이 터져요.\n나와 상대의 유닛이 똑같이 무작위로 폭발하고\n그 칸은 주인 없는 땅이 돼요. 재료를 오래 쌓아 두지 마세요!",
                Pictures = new[] { new[] { "Tiles/tile_lava" } },
            },
            new()
            {
                Id = "ch3", Title = "얼음성",
                Body = "얼음 칸에 유닛을 놓으면 밀고 나온 방향으로\n한 칸 더 미끄러져요. 카드를 고르면 얼음 칸에\n반투명 화살표로 미끄러질 방향이, 칸을 노리면\n실제로 멈출 칸에 반투명 유닛이 보여요.",
                Pictures = new[] { new[] { "Tiles/tile_grass_a", "Tiles/overlay_ice" } },
            },
            new()
            {
                Id = "ch4", Title = "끊어진 다리",
                Body = "낭떠러지에는 아무것도 놓을 수 없어요.\n좁은 다리를 먼저 차지하면 상대의 길을 막을 수 있어요.",
                Pictures = new[] { new[] { "Tiles/tile_player" }, new[] { "Tiles/tile_ai" } },
            },
            new()
            {
                Id = "ch5", Title = "공허의 탑",
                Body = "컨베이어: 라운드가 끝나면 벨트 위 칸이 한 칸씩 흘러가고 끝에서 떨어져요.\n블랙홀: 라운드마다 주변 8칸을 비워요.\n침묵 지대: 그 위에서는 융합과 공명이 일어나지 않아요.",
                Pictures = new[] { new[] { "Tiles/tile_grass_a", "Tiles/overlay_conveyor" }, new[] { "Tiles/tile_blackhole" }, new[] { "Tiles/tile_grass_b", "Tiles/overlay_silence" } },
            },
            new()
            {
                Id = "ignis", Title = "보스: 이그니스",
                Body = "화염 유닛만 내며 내 땅을 빼앗으러 와요.\n상대가 놓은 화염 옆에 물이나 번개를 놓으면\n그 화염을 재료로 가로챌 수 있어요!",
                Pictures = new[] { new[] { "Units/unit_red" } },
            },
            new()
            {
                Id = "glacia", Title = "보스: 글라시아",
                Body = "물과 보라 위주에 영역 잠금과 탈색으로 길을 막아요.\n잠긴 곳에 매달리지 말고 다른 쪽 전선을 넓혀 보세요.",
                Pictures = new[] { new[] { "Units/unit_blue" } },
            },
            new()
            {
                Id = "homunculus", Title = "보스: 호문클루스",
                Body = "유닛을 아끼다가 내가 놓은 유닛 옆에서 콤보를 훔쳐 가요.\n상대 전선 바로 옆에 재료를 흘리지 않도록 조심하고,\n위험한 내 유닛은 탈색으로 지워도 좋아요.",
                Pictures = new[] { new[] { "Units/unit_black" } },
            },
        };

        public void StartTutorial(int lesson)
        {
            Tutorial = true;
            LessonIndex = Mathf.Clamp(lesson, 0, TutorialCatalog.Count - 1);
            LessonDone = false;
            _tips.Clear();
            var l = Lesson;
            Progress.TutorialLesson = LessonIndex; // 중간에 나가도 이 레슨부터 이어서
            BeginBattle(new StageDef { Index = -1, Name = l.Title, MapNote = "" }, l.CreateGame());
            // 카드는 미리 골라 두지 않는다: 실제 게임처럼 카드를 끌어 놓거나, 카드를 누른 뒤 칸을 눌러야 놓인다.
            // 목표 칸은 카드를 고르기 전부터 빛난다 (BuildHighlight).
            Message = l.Intro;
        }

        public void NextLesson()
        {
            if (LessonIndex + 1 < TutorialCatalog.Count)
            {
                StartTutorial(LessonIndex + 1);
                return;
            }
            Progress.TutorialDone = true;
            Tutorial = false;
            StartStage(0);
        }

        public void SkipTutorial()
        {
            Progress.TutorialDone = true;
            Tutorial = false;
            ShowLobby(Mathf.Min(Progress.Unlocked - 1, StageCatalog.Count - 1));
        }

        public void DismissTip()
        {
            if (_tips.Count == 0) return;
            Progress.MarkTipSeen(_tips.Dequeue().Id);
        }

        /// <summary>이 스테이지에서 처음 보는 챕터 기믹·보스 팁을 줄 세운다.</summary>
        private void QueueTips(StageDef s)
        {
            _tips.Clear();
            void Add(string id)
            {
                if (Progress.TipSeen(id)) return;
                foreach (var t in Tips)
                    if (t.Id == id) _tips.Enqueue(t);
            }
            if (s.Chapter >= 1) Add($"ch{s.Chapter + 1}");
            if (s.IsBoss) Add(s.Persona.Id.TrimEnd('+'));
        }

        /// <summary>튜토리얼에서는 놓을 수 있는 칸을 레슨의 목표로만 좁힌다.</summary>
        private void RestrictToLesson()
        {
            if (!Tutorial || LessonDone) return;
            var keep = new HashSet<Cell>();
            if (_blueAt is { } at && Lesson.WaterDir is { } dir)
                foreach (var c in BlueLine(at, dir)) keep.Add(c);
            else
                keep.Add(Lesson.Target);
            _hl.Legal.IntersectWith(keep);
            _hl.Arrows.Clear();
            if (_blueAt is { } a && Lesson.WaterDir is { } d && BlueLine(a, d).Count > 0) _hl.Arrows[BlueLine(a, d)[0]] = d;
        }

        /// <summary>레슨 판정: 플레이어가 수를 두면 결과가 레슨 목표를 만족하는지 본다.</summary>
        private void CheckLesson(PlayResult r)
        {
            if (!Tutorial || LessonDone || r == null) return;
            if (Lesson.Check(r))
            {
                LessonDone = true;
                Message = Lesson.Outro;
                // 마지막 레슨을 마치면 바로 완료로 기록 → 다음 접속부터 튜토리얼이 안 나온다
                if (LessonIndex + 1 >= TutorialCatalog.Count) Progress.TutorialDone = true;
                else Progress.TutorialLesson = LessonIndex + 1;
            }
            else
            {
                StartTutorial(LessonIndex); // 다른 수를 두면 처음부터 (목표 칸만 열려 있어 거의 일어나지 않음)
            }
        }

        /// <summary>튜토리얼 손가락 시범: (시작, 끝) 화면 좌표. 시범이 필요 없으면 false.</summary>
        public bool HintGesture(out Vector2 from, out Vector2 to, out bool fromCard)
        {
            from = to = default;
            fromCard = false;
            if (!Tutorial || LessonDone || !PlayerTurn || DraggingCard >= 0 || _boardPress || ActiveTip != null) return false;
            if (Selected >= 0 && !_blueAt.HasValue)
            {
                // 카드를 눌러 골랐으면: 목표 칸을 누르라는 손가락
                from = to = WorldToGui(_view.ToWorld(Lesson.Target)) + new Vector2(0f, 10f * UiScale);
                return true;
            }
            if (_blueAt is { } at)
            {
                if (!(Lesson.WaterDir is { } dir)) return false;
                from = WorldToGui(_view.ToWorld(at));
                to = WorldToGui(_view.ToWorld(at + dir * 2));
                return true;
            }
            if (Hand.Cards.Count == 0) return false;
            fromCard = true;
            from = Hud.ToScreenPos(Hand.Cards[0].Pos);
            // 카드를 끌 때 노리는 칸은 손가락보다 CardLift 위 → 손가락은 목표 칸보다 그만큼 아래로 끈다
            to = WorldToGui(_view.ToWorld(Lesson.Target)) + new Vector2(0f, CardLift * UiScale);
            return true;
        }

        private Vector2 WorldToGui(Vector3 world)
        {
            var s = _cam.WorldToScreenPoint(world);
            return new Vector2(s.x, UnityEngine.Screen.height - s.y);
        }
    }
}
