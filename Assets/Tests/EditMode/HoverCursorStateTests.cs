using NUnit.Framework;
using KingdomsOfBharat.UI;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Covers Roadmap Section 4.3's "build-placement cursor asset missing"
    // closeout: HoverTooltip.ResolveCursorState is the pure decision logic
    // behind the cursor wiring, extracted specifically so it's testable
    // without a scene/camera/raycast setup - see HoverTooltip.cs's own
    // comment on the pattern this mirrors
    // (CivilizationProfile.FindCategoryMultiplier). The Tier 2 UI art
    // delivery split the single Gather state into 4 per-resource states
    // (Wood/Food/Gold/Stone), so ResolveCursorState now also takes the
    // hovered ResourceType.
    public class HoverCursorStateTests
    {
        [Test]
        public void NoHover_ReturnsDefault()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: false, selectionCanAttack: false,
                hoveringResourceNode: false, resourceType: ResourceType.Wood, selectionCanGather: false);

            Assert.AreEqual(HoverTooltip.HoverCursorState.Default, state);
        }

        [Test]
        public void PlacingBuilding_ReturnsBuildPlacement_RegardlessOfHover()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: true,
                hoveringHostileTarget: true, selectionCanAttack: true,
                hoveringResourceNode: false, resourceType: ResourceType.Wood, selectionCanGather: false);

            Assert.AreEqual(HoverTooltip.HoverCursorState.BuildPlacement, state);
        }

        [Test]
        public void HostileTarget_WithAttacker_ReturnsAttackMove()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: true, selectionCanAttack: true,
                hoveringResourceNode: false, resourceType: ResourceType.Wood, selectionCanGather: false);

            Assert.AreEqual(HoverTooltip.HoverCursorState.AttackMove, state);
        }

        [Test]
        public void HostileTarget_WithoutAttacker_ReturnsInvalid()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: true, selectionCanAttack: false,
                hoveringResourceNode: false, resourceType: ResourceType.Wood, selectionCanGather: false);

            Assert.AreEqual(HoverTooltip.HoverCursorState.Invalid, state);
        }

        [Test]
        public void ResourceNode_Wood_WithGatherer_ReturnsGatherWood()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: false, selectionCanAttack: false,
                hoveringResourceNode: true, resourceType: ResourceType.Wood, selectionCanGather: true);

            Assert.AreEqual(HoverTooltip.HoverCursorState.GatherWood, state);
        }

        [Test]
        public void ResourceNode_Food_WithGatherer_ReturnsGatherFood()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: false, selectionCanAttack: false,
                hoveringResourceNode: true, resourceType: ResourceType.Food, selectionCanGather: true);

            Assert.AreEqual(HoverTooltip.HoverCursorState.GatherFood, state);
        }

        [Test]
        public void ResourceNode_Gold_WithGatherer_ReturnsGatherGold()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: false, selectionCanAttack: false,
                hoveringResourceNode: true, resourceType: ResourceType.Gold, selectionCanGather: true);

            Assert.AreEqual(HoverTooltip.HoverCursorState.GatherGold, state);
        }

        [Test]
        public void ResourceNode_Stone_WithGatherer_ReturnsGatherStone()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: false, selectionCanAttack: false,
                hoveringResourceNode: true, resourceType: ResourceType.Stone, selectionCanGather: true);

            Assert.AreEqual(HoverTooltip.HoverCursorState.GatherStone, state);
        }

        [Test]
        public void ResourceNode_WithoutGatherer_ReturnsInvalid()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: false, selectionCanAttack: false,
                hoveringResourceNode: true, resourceType: ResourceType.Wood, selectionCanGather: false);

            Assert.AreEqual(HoverTooltip.HoverCursorState.Invalid, state);
        }
    }
}
