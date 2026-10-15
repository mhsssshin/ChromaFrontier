using NUnit.Framework;

namespace ChromaFrontier.Core.Tests
{
    public class TutorialTests
    {
        [Test]
        public void EveryLesson_TargetIsPlayable_AndProducesTheLessonResult()
        {
            for (var i = 0; i < TutorialCatalog.Count; i++)
            {
                var lesson = TutorialCatalog.Lessons[i];
                var g = lesson.CreateGame();
                Assert.AreEqual(1, g.DeckOf(Side.Player).Hand.Count, lesson.Title);
                Assert.AreEqual(lesson.Card, g.DeckOf(Side.Player).Hand[0], lesson.Title);

                PlayResult r;
                if (lesson.Card.IsUnit)
                {
                    Assert.IsTrue(g.CanPlace(Side.Player, lesson.Target), lesson.Title);
                    r = g.Play(0, lesson.Target, lesson.WaterDir ?? Cell.Orthogonal[0]);
                }
                else
                {
                    Assert.IsTrue(g.CanTarget(Side.Player, lesson.Card.Skill, lesson.Target), lesson.Title);
                    r = g.Cast(0, lesson.Target);
                }
                Assert.IsTrue(lesson.Check(r), lesson.Title);
            }
        }

        [Test]
        public void UnshuffledDeck_DrawsInListOrder()
        {
            var cards = new[] { Card.Unit(Element.Red), Card.Unit(Element.Blue), Card.Of(Skill.Mine) };
            var deck = new Deck(cards, new Rng(1), shuffle: false);
            CollectionAssert.AreEqual(cards, deck.Hand);
        }
    }
}
