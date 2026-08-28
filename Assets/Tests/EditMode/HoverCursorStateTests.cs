using NUnit.Framework;
using KingdomsOfBharat.UI;

namespace KingdomsOfBharat.Tests
{
    // Covers Roadmap Section 4.3's "build-placement cursor asset missing"
    // closeout: HoverTooltip.ResolveCursorState is the pure decision logic
    // behind the 5-state cursor wiring (Default/AttackMove/Gather/Invalid/
    // BuildPlacement), extracted specifically so it's testable without a
    // scene/camera/raycast setup - see HoverTooltip.cs's own comment on the
    // pattern this mirrors (CivilizationProfile.FindCategoryMultiplier).
    public class HoverCursorStateTests
    {
        [Test]
        public void NoHover_ReturnsDefault()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: false, selectionCanAttack: false,
                hoveringResourceNode: false, selectionCanGather: false);

            Assert.AreEqual(HoverTooltip.HoverCursorState.Default, state);
        }

        [Test]
        public void PlacingBuilding_ReturnsBuildPlacement_RegardlessOfHover()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: true,
                hoveringHostileTarget: true, selectionCanAttack: true,
                hoveringResourceNode: false, selectionCanGather: false);

            Assert.AreEqual(HoverTooltip.HoverCursorState.BuildPlacement, state);
        }

        [Test]
        public void HostileTarget_WithAttacker_ReturnsAttackMove()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: true, selectionCanAttack: true,
                hoveringResourceNode: false, selectionCanGather: false);

            Assert.AreEqual(HoverTooltip.HoverCursorState.AttackMove, state);
        }

        [Test]
        public void HostileTarget_WithoutAttacker_ReturnsInvalid()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: true, selectionCanAttack: false,
                hoveringResourceNode: false, selectionCanGather: false);

            Assert.AreEqual(HoverTooltip.HoverCursorState.Invalid, state);
        }

        [Test]
        public void ResourceNode_WithGatherer_ReturnsGather()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: false, selectionCanAttack: false,
                hoveringResourceNode: true, selectionCanGather: true);

            Assert.AreEqual(HoverTooltip.HoverCursorState.Gather, state);
        }

        [Test]
        public void ResourceNode_WithoutGatherer_ReturnsInvalid()
        {
            var state = HoverTooltip.ResolveCursorState(
                isPlacingBuilding: false,
                hoveringHostileTarget: false, selectionCanAttack: false,
                hoveringResourceNode: true, selectionCanGather: false);

            Assert.AreEqual(HoverTooltip.HoverCursorState.Invalid, state);
        }
    }
}
