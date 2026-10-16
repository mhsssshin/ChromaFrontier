using System.Collections.Generic;
using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>수 하나·라운드 하나의 결과를 메시지와 이펙트로 보여 준다.</summary>
    public sealed partial class GameBootstrap
    {
        private void AfterMove(Side actor, Cell at, Cell at2)
        {
            var who = actor == Side.Player ? "나" : "AI";
            var r = Game.LastResult;
            _aiTimer = AiDelay;
            if (r == null)
            {
                Message = $"{who}: 패 버리기 — 손패를 모두 버리고 새로 뽑았다";
                return;
            }
            if (r.Skill == Skill.None) at = r.At; // 얼음에서 미끄러졌으면 실제로 놓인 칸
            PlayEffects(actor, r, at, at2);

            if (r.Skill != Skill.None)
            {
                Message = $"{who}: {Palette.Name(r.Skill)}";
                switch (r.Skill)
                {
                    case Skill.Warp:
                        _view.Flash(new[] { at, at2 });
                        Message += " — 두 칸을 맞바꿨다. 턴 계속!";
                        break;
                    case Skill.Lock:
                        var area = new List<Cell>();
                        foreach (var c in Game.Board.Cells())
                            if (c.Chebyshev(at) <= Balance.LockRadius) area.Add(c);
                        _view.Flash(area);
                        Message += $" — {Balance.LockTurns / Balance.TicksPerRound}턴 동안 배치 금지";
                        break;
                    case Skill.Bleach:
                        _view.Flash(new[] { at });
                        Message += $" — {Palette.Name(r.Bleached)} 유닛 무력화";
                        break;
                    case Skill.Mine:
                        if (actor == Side.Player) _view.Flash(new[] { at });
                        Message = actor == Side.Player ? "나: 잠복 지뢰를 묻었다" : "AI가 어딘가에 무언가를 숨겼다...";
                        break;
                }
                return;
            }

            _view.Flash(r.Captured);
            if (r.MineTriggered)
            {
                Message = actor == Side.Player
                    ? $"쾅! AI의 잠복 지뢰를 밟았다 — {r.Captured.Count}칸을 빼앗김"
                    : $"쾅! AI가 내 지뢰를 밟았다 — +{r.Captured.Count}칸";
                return;
            }
            Message = $"{who}: {Palette.Name(r.Placed)} 배치 → +{r.Captured.Count}칸";
            if (r.Slid) Message += " (얼음에 미끄러짐)";
            if (r.Resonance) Message += $"   ★ 공명! {Palette.ResonanceName(r.Placed)}";
            if (r.Fused != Element.None)
            {
                Message += r.Boosted
                    ? $"   ★★ 3차 조합! {Palette.Name(r.Fused)} 강화"
                    : $"   ★ {Palette.Name(r.Fused)} 융합!";
                if (r.StoleCatalyst) Message += " (상대 유닛 흡수)";
            }
            if (r.ExtraTurn) Message += "   추가 턴!";
        }

        /// <summary>
        /// 수 하나의 연출: 놓은 자리에 원소 이펙트 → 융합 이펙트 → 점유된 칸마다 거리순 시차로 고리·반짝임.
        /// </summary>
        private void PlayEffects(Side actor, PlayResult r, Cell at, Cell at2)
        {
            var white = Color.white;
            var pos = _view.ToWorld(at);
            var lift = new Vector3(0f, 0.35f, 0f);

            if (r.Skill != Skill.None)
            {
                switch (r.Skill)
                {
                    case Skill.Warp:
                        var violet = new Color(0.8f, 0.6f, 1f);
                        _fx.Spawn(FxView.Void, pos + lift, white, 0.9f);
                        _fx.Spawn(FxView.Void, _view.ToWorld(at2) + lift, white, 0.9f, 0.08f);
                        _fx.Spawn(FxView.Sparkle, pos + lift, violet, 1.2f, 0.2f);
                        _fx.Spawn(FxView.Sparkle, _view.ToWorld(at2) + lift, violet, 1.2f, 0.25f);
                        break;
                    case Skill.Lock:
                        foreach (var c in Game.Board.Cells())
                            if (c.Chebyshev(at) <= Balance.LockRadius)
                                _fx.Spawn(FxView.Smoke, _view.ToWorld(c) + lift * 0.5f, new Color(0.55f, 0.6f, 0.75f), 0.8f, c.Manhattan(at) * 0.05f);
                        break;
                    case Skill.Bleach:
                        _fx.Spawn(FxView.Smoke, pos + lift, white, 1.2f);
                        _fx.Spawn(FxView.Sparkle, pos + lift * 1.5f, white, 1.2f, 0.15f);
                        break;
                    case Skill.Mine:
                        if (actor == Side.Player) _fx.Spawn(FxView.Smoke, pos, new Color(0.7f, 0.6f, 0.5f), 0.7f);
                        break;
                }
                return;
            }

            if (r.MineTriggered)
            {
                _fx.Spawn(FxView.Burst, pos + lift, white, 1.8f);
                _fx.Spawn(FxView.Smoke, pos + lift, new Color(0.5f, 0.45f, 0.45f), 1.6f, 0.15f);
                _shake = 0.35f;
            }
            else
            {
                switch (r.Placed)
                {
                    case Element.Red: _fx.Spawn(FxView.Burst, pos + lift, white, r.Resonance ? 2.2f : 1.2f, 0.12f); break;
                    case Element.Blue: _fx.Spawn(FxView.Splash, pos + lift, white, r.Resonance ? 2.2f : 1.2f, 0.12f); break;
                    case Element.Yellow: _fx.Spawn(FxView.Zap, pos + lift, white, r.Resonance ? 2.4f : 1.3f, 0.12f); break;
                }
                var fuseDelay = 0.3f;
                switch (r.Fused)
                {
                    case Element.Orange:
                        _fx.Spawn(FxView.Burst, pos + lift, white, 2.4f, fuseDelay);
                        _shake = 0.3f;
                        break;
                    case Element.Purple:
                        _fx.Spawn(FxView.Zap, pos + lift * 2f, new Color(0.85f, 0.6f, 1f), 2f, fuseDelay);
                        _fx.Spawn(FxView.Ring, pos, new Color(0.7f, 0.45f, 1f), 3f, fuseDelay);
                        break;
                    case Element.Green:
                        _fx.Spawn(FxView.Smoke, pos + lift, new Color(0.6f, 1f, 0.6f), 2f, fuseDelay);
                        _fx.Spawn(FxView.Ring, pos, new Color(0.5f, 1f, 0.5f), 3f, fuseDelay);
                        break;
                    case Element.White:
                        _fx.Spawn(FxView.Holy, pos + lift, white, r.Boosted ? 3.6f : 2.6f, fuseDelay, 12f);
                        _shake = 0.25f;
                        break;
                    case Element.Black:
                        _fx.Spawn(FxView.Void, pos + lift, white, r.Boosted ? 4f : 3f, fuseDelay, 12f);
                        _shake = 0.45f;
                        break;
                }
                foreach (var c in r.Catalysts)
                    _fx.Spawn(FxView.Sparkle, _view.ToWorld(c) + lift, Palette.Of(r.Fused != Element.None ? r.Fused : r.Placed), 1f, fuseDelay * 0.5f);
                if (r.Resonance)
                {
                    _fx.Spawn(FxView.Ring, pos, Palette.Of(r.Placed), 3f, 0.15f);
                    _shake = Mathf.Max(_shake, 0.2f);
                }
            }

            // 점유된 칸: 가까운 칸부터 차례로 고리 + 반짝임 (팀 색)
            var owner = r.MineTriggered ? actor.Opponent() : actor;
            var team = Palette.Of(owner);
            var baseDelay = r.Fused != Element.None ? 0.35f : 0.12f;
            foreach (var c in r.Captured)
            {
                var d = baseDelay + c.Manhattan(at) * 0.06f;
                var p = _view.ToWorld(c);
                _fx.Spawn(FxView.Ring, p, Color.Lerp(team, Color.white, 0.35f), 1f, d);
                _fx.Spawn(FxView.Sparkle, p + lift, Color.Lerp(team, Color.white, 0.6f), 0.6f, d + 0.05f);
            }
        }

        /// <summary>라운드 끝 기믹(화산·블랙홀·컨베이어) 연출과 안내.</summary>
        private void PlayRoundEffects()
        {
            if (Game.Round == _lastRound) return;
            _lastRound = Game.Round;
            var lift = new Vector3(0f, 0.35f, 0f);
            var notes = new List<string>();
            foreach (var ev in Game.RoundEvents)
            {
                var p = _view.ToWorld(ev.Cell);
                switch (ev.Kind)
                {
                    case RoundEventKind.Lava:
                        _fx.Spawn(FxView.Burst, p + lift, Color.white, 1.8f, 0.2f);
                        _fx.Spawn(FxView.Smoke, p + lift * 2f, new Color(0.45f, 0.4f, 0.4f), 1.6f, 0.35f);
                        _shake = Mathf.Max(_shake, 0.3f);
                        if (!notes.Contains("화산 폭발!")) notes.Add("화산 폭발!");
                        break;
                    case RoundEventKind.BlackHole:
                        _fx.Spawn(FxView.Void, p + lift, Color.white, 0.9f, 0.1f);
                        if (!notes.Contains("블랙홀이 주변을 삼켰다")) notes.Add("블랙홀이 주변을 삼켰다");
                        break;
                    case RoundEventKind.Conveyor:
                        _fx.Spawn(FxView.Smoke, p + lift, new Color(0.8f, 0.8f, 0.85f), 0.9f, 0.1f);
                        if (!notes.Contains("컨베이어가 움직였다")) notes.Add("컨베이어가 움직였다");
                        break;
                }
            }
            if (notes.Count > 0) Message += "\n라운드 기믹: " + string.Join(", ", notes);
        }
    }
}
