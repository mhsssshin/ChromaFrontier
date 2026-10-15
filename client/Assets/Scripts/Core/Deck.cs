using System.Collections.Generic;

namespace ChromaFrontier.Core
{
    /// <summary>
    /// 스몰 덱 순환: 뽑을 덱이 비면 무덤을 섞어 다시 덱으로. 손패는 항상 HandSize 장을 맞춘다.
    /// 드로우 보정: 다 채운 손패에 유닛이 한 장도 없으면, 마지막에 뽑은 스킬을 덱 맨 아래로 돌리고 유닛을 대신 넣는다.
    /// </summary>
    public sealed class Deck
    {
        private readonly List<Card> _draw;
        private readonly List<Card> _discard;
        public readonly List<Card> Hand;

        /// <param name="shuffle">false 면 섞지 않고 목록 순서대로 뽑는다 (튜토리얼처럼 손패를 정해 둘 때)</param>
        public Deck(IEnumerable<Card> cards, Rng rng, bool shuffle = true)
        {
            _draw = new List<Card>(cards);
            _discard = new List<Card>();
            Hand = new List<Card>();
            if (shuffle) Shuffle(_draw, rng);
            else _draw.Reverse(); // 뒤에서부터 뽑으므로 뒤집어 두면 목록 순서대로 손패에 들어온다
            Refill(rng);
        }

        private Deck(Deck src)
        {
            _draw = new List<Card>(src._draw);
            _discard = new List<Card>(src._discard);
            Hand = new List<Card>(src.Hand);
        }

        public Deck Clone() => new(this);

        public int DrawCount => _draw.Count;
        public int DiscardCount => _discard.Count;

        public bool HandHasUnit
        {
            get
            {
                foreach (var c in Hand)
                    if (c.IsUnit) return true;
                return false;
            }
        }

        public void Refill(Rng rng)
        {
            while (Hand.Count < Balance.HandSize)
            {
                if (_draw.Count == 0 && !Recycle(rng)) break;
                Hand.Add(_draw[_draw.Count - 1]);
                _draw.RemoveAt(_draw.Count - 1);
            }
            GuaranteeUnit(rng);
        }

        public Card Play(int handIndex)
        {
            var c = Hand[handIndex];
            Hand.RemoveAt(handIndex);
            _discard.Add(c);
            return c;
        }

        /// <summary>패 버리기(전술적 멀리건): 손패 전부를 덱에 섞어 넣고 새로 뽑는다.</summary>
        public void Mulligan(Rng rng)
        {
            _draw.AddRange(Hand);
            Hand.Clear();
            Shuffle(_draw, rng);
            Refill(rng);
        }

        private void GuaranteeUnit(Rng rng)
        {
            if (Hand.Count == 0 || HandHasUnit) return;
            var i = _draw.FindLastIndex(c => c.IsUnit);
            if (i < 0 && Recycle(rng)) i = _draw.FindLastIndex(c => c.IsUnit);
            if (i < 0) return;
            var unit = _draw[i];
            _draw.RemoveAt(i);
            _draw.Insert(0, Hand[Hand.Count - 1]);
            Hand[Hand.Count - 1] = unit;
        }

        /// <summary>무덤을 섞어 덱 밑에 넣는다. 무덤이 비었으면 false.</summary>
        private bool Recycle(Rng rng)
        {
            if (_discard.Count == 0) return false;
            Shuffle(_discard, rng);
            _draw.InsertRange(0, _discard);
            _discard.Clear();
            return true;
        }

        private static void Shuffle(List<Card> list, Rng rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
