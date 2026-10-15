using System.Collections.Generic;
using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// 손패의 화면상 움직임 (기준 좌표 540x960). Core 손패와 매 프레임 맞추면서:
    /// 새로 들어온 카드는 덱 더미에서 뒷면으로 날아와 뒤집히고, 낸 카드는 보드 쪽으로 날아가며 사라지고,
    /// 남은 카드는 부채꼴 자리로 미끄러진다. 그리기와 클릭 판정은 Hud 가 한다.
    /// </summary>
    public sealed class HandView
    {
        public const float CardW = 104f, CardH = 146f;
        public static readonly Vector2 DeckPos = new(52f, 752f);
        private const float Spacing = 92f, BaseY = 960f - 22f - CardH * 0.5f;
        private const float DealStagger = 0.09f, FlipTime = 0.28f, LeaveTime = 0.32f;

        public sealed class Visual
        {
            public Card Card;
            public Vector2 Pos;
            public float Rot, Scale = 1f, Alpha = 1f;
            public float Delay, Age;       // 뽑기: Delay 동안 덱에서 대기, Age 로 뒤집기 진행
            public Vector2 LeaveTo;        // 떠나는 카드의 목적지
            public bool FaceUp => Age >= FlipTime * 0.5f;
            public float FlipWidth => Age >= FlipTime ? 1f : Mathf.Abs(Mathf.Cos(Age / FlipTime * Mathf.PI));
        }

        public readonly List<Visual> Cards = new();
        public readonly List<Visual> Leaving = new();
        public int Hover = -1;

        public void Reset()
        {
            Cards.Clear();
            Leaving.Clear();
            Hover = -1;
        }

        /// <param name="dragging">끌고 있는 카드 (-1 이면 없음). 그 카드는 손가락(dragRef) 바로 아래를 작게 따라온다.</param>
        public void Update(IReadOnlyList<Card> hand, int selected, float dt, int dragging = -1, Vector2 dragRef = default)
        {
            Match(hand);

            var n = Cards.Count;
            for (var i = 0; i < n; i++)
            {
                var v = Cards[i];
                if (v.Delay > 0f)
                {
                    v.Delay -= dt;
                    continue;
                }
                v.Age += dt;
                var k = i - (n - 1) * 0.5f;
                var target = new Vector2(270f + k * Spacing, BaseY + k * k * 3f);
                var rot = k * 4f;
                var scale = 1f;
                var rate = 14f;
                if (i == dragging)
                {
                    // 손가락이 노리는 칸(손가락 위쪽)을 가리지 않게 카드는 손가락 아래에 작게
                    target = dragRef + new Vector2(0f, 46f);
                    rot = 0f;
                    scale = 0.62f;
                    rate = 30f;
                }
                else if (i == selected)
                {
                    target.y -= 28f;
                    rot = 0f;
                    scale = 1.08f;
                }
                else if (i == Hover)
                {
                    target.y -= 12f;
                    scale = 1.04f;
                }
                var t = 1f - Mathf.Exp(-rate * dt);
                v.Pos = Vector2.Lerp(v.Pos, target, t);
                v.Rot = Mathf.Lerp(v.Rot, rot, t);
                v.Scale = Mathf.Lerp(v.Scale, scale, t);
            }

            for (var i = Leaving.Count - 1; i >= 0; i--)
            {
                var v = Leaving[i];
                v.Age += dt;
                var t = 1f - Mathf.Exp(-10f * dt);
                v.Pos = Vector2.Lerp(v.Pos, v.LeaveTo, t);
                v.Scale = Mathf.Lerp(v.Scale, 0.6f, t);
                v.Alpha = Mathf.Clamp01(1f - v.Age / LeaveTime);
                if (v.Alpha <= 0f) Leaving.RemoveAt(i);
            }
        }

        /// <summary>
        /// 이전 화면 카드와 지금 손패를 순서대로 맞춘다 (Core 손패는 낸 카드를 빼고 뒤에 붙이므로 순서가 유지된다).
        /// 못 맞춘 옛 카드는 떠나고, 못 맞춘 새 카드는 덱에서 뽑혀 온다.
        /// </summary>
        private void Match(IReadOnlyList<Card> hand)
        {
            if (SameAsVisuals(hand)) return;
            var next = new List<Visual>(hand.Count);
            var j = 0;
            var dealt = 0;
            foreach (var card in hand)
            {
                var found = -1;
                for (var k = j; k < Cards.Count; k++)
                    if (Cards[k].Card.Equals(card)) { found = k; break; }
                if (found >= 0)
                {
                    for (var k = j; k < found; k++) Leave(Cards[k], false);
                    next.Add(Cards[found]);
                    j = found + 1;
                }
                else
                {
                    next.Add(new Visual
                    {
                        Card = card, Pos = DeckPos, Rot = -12f, Scale = 0.55f,
                        Delay = dealt++ * DealStagger, Age = 0f,
                    });
                }
            }
            var toDeck = next.TrueForAll(v => v.Age == 0f && v.Delay >= 0f) && Cards.Count > 0; // 전부 새 카드 = 멀리건
            for (var k = j; k < Cards.Count; k++) Leave(Cards[k], toDeck);
            Cards.Clear();
            Cards.AddRange(next);
        }

        private bool SameAsVisuals(IReadOnlyList<Card> hand)
        {
            if (hand.Count != Cards.Count) return false;
            for (var i = 0; i < hand.Count; i++)
                if (!hand[i].Equals(Cards[i].Card)) return false;
            return true;
        }

        private void Leave(Visual v, bool toDeck)
        {
            v.Age = 0f;
            v.LeaveTo = toDeck ? DeckPos : new Vector2(270f, 470f);
            Leaving.Add(v);
        }
    }
}
