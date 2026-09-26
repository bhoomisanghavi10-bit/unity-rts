using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.AI;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Prompt 13: the AI's economy decisions are pure functions of the world
    // numbers they are given, so they are deterministic and cannot depend on
    // scene search order.
    public class AiWorkerPlannerTests
    {
        private static readonly float[] Rich = { 100f, 100f, 100f, 100f };

        [Test]
        public void NoNeeds_FollowsDesiredShare_FoodFirst()
        {
            ResourceType pick = AiWorkerPlanner.ChooseResource(new[] { 0, 0, 0, 0 }, Rich, 0);
            Assert.AreEqual(ResourceType.Food, pick);
        }

        [Test]
        public void FillsShortfalls_InProportion_NotAllOnOneResource()
        {
            var current = new[] { 0, 0, 0, 0 };
            for (int i = 0; i < 10; i++)
            {
                current[(int)AiWorkerPlanner.ChooseResource(current, Rich, i)]++;
            }

            Assert.GreaterOrEqual(current[(int)ResourceType.Food], 3);
            Assert.GreaterOrEqual(current[(int)ResourceType.Wood], 2);
            Assert.GreaterOrEqual(current[(int)ResourceType.Gold], 1);
            Assert.GreaterOrEqual(current[(int)ResourceType.Stone], 1);
        }

        [Test]
        public void ANeed_PullsWorkersToTheMissingResource()
        {
            var needs = new[] { 0f, 0f, 0f, 100f }; // stone gates the next step
            ResourceType pick = AiWorkerPlanner.ChooseResource(new[] { 2, 2, 1, 0 }, Rich, 5, needs);

            Assert.AreEqual(ResourceType.Stone, pick);
        }

        [Test]
        public void SaturatedResource_IsNotWorthMoreWorkers()
        {
            var stock = new[] { 500f, 50f, 50f, 50f }; // plenty of food
            ResourceType pick = AiWorkerPlanner.ChooseResource(new[] { 0, 0, 0, 0 }, stock, 0);

            Assert.AreNotEqual(ResourceType.Food, pick);
        }

        [Test]
        public void SameInputs_SameChoice_Repeatably()
        {
            var needs = new[] { 10f, 40f, 0f, 20f };
            ResourceType first = AiWorkerPlanner.ChooseResource(new[] { 3, 1, 0, 0 }, Rich, 4, needs);
            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(first, AiWorkerPlanner.ChooseResource(new[] { 3, 1, 0, 0 }, Rich, 4, needs));
            }
        }

        [Test]
        public void PickNearest_ChoosesByDistance_TiesGoToTheLowestIndex_NoneIsMinusOne()
        {
            var site = new Vector3(0, 0, 0);
            Assert.AreEqual(2, AiWorkerPlanner.PickNearest(new[] { new Vector3(9, 0, 0), new Vector3(5, 0, 0), new Vector3(1, 0, 1) }, site));
            Assert.AreEqual(0, AiWorkerPlanner.PickNearest(new[] { new Vector3(3, 0, 0), new Vector3(0, 0, 3) }, site), "Equal distance: the lower index wins, whatever order the scene listed them.");
            Assert.AreEqual(-1, AiWorkerPlanner.PickNearest(new Vector3[0], site));
        }
    }
}
