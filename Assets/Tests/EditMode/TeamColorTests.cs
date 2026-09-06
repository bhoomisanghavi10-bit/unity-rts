using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // Wave 5 item 29: pure GameObject/Mesh construction, no MonoBehaviour
    // lifecycle timing involved (unlike the factories' own full Spawn
    // methods, which need Play mode) - so directly EditMode-testable.
    public class TeamColorTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
        }

        [Test]
        public void TeamColor_ReturnsThreeDistinctFactionColors()
        {
            Color player = TeamColor.For(FactionId.Player);
            Color enemy = TeamColor.For(FactionId.Enemy);
            Color enemy2 = TeamColor.For(FactionId.Enemy2);

            Assert.AreNotEqual(player, enemy);
            Assert.AreNotEqual(player, enemy2);
            Assert.AreNotEqual(enemy, enemy2);
        }

        [Test]
        public void AttachToBuilding_AddsRendererColoredForFaction()
        {
            var visualRoot = new GameObject("Visual");
            _spawned.Add(visualRoot);
            var bounds = new Bounds(new Vector3(0f, 2f, 0f), new Vector3(4f, 4f, 4f));

            TeamColorAccent.AttachToBuilding(visualRoot.transform, bounds, FactionId.Enemy);

            Renderer renderer = visualRoot.GetComponentInChildren<Renderer>();
            Assert.IsNotNull(renderer, "Expected a banner renderer parented under the visual root.");
            Assert.AreEqual(TeamColor.For(FactionId.Enemy), renderer.sharedMaterial.color);
        }

        [Test]
        public void AttachToBuilding_ParentsBannerUnderVisualRootForAutomaticCleanup()
        {
            var visualRoot = new GameObject("Visual");
            _spawned.Add(visualRoot);
            var bounds = new Bounds(Vector3.zero, new Vector3(2f, 3f, 2f));

            TeamColorAccent.AttachToBuilding(visualRoot.transform, bounds, FactionId.Player);

            Assert.AreEqual(1, visualRoot.transform.childCount);
            Assert.AreEqual(visualRoot.transform, visualRoot.transform.GetChild(0).parent);
        }

        [Test]
        public void AttachToHumanoid_WithoutAnimator_FallsBackToModelRoot()
        {
            var model = new GameObject("PlainModel");
            _spawned.Add(model);

            TeamColorAccent.AttachToHumanoid(model, FactionId.Player);

            Renderer renderer = model.GetComponentInChildren<Renderer>();
            Assert.IsNotNull(renderer, "Expected a pennant renderer even with no Animator (War Elephant case).");
        }

        [Test]
        public void AttachToHumanoid_PennantColorMatchesFaction()
        {
            var model = new GameObject("PlainModel2");
            _spawned.Add(model);

            TeamColorAccent.AttachToHumanoid(model, FactionId.Enemy2);

            Transform flag = model.transform.Find("TeamColorAccent/Flag");
            Assert.IsNotNull(flag, "Expected a 'TeamColorAccent/Flag' child.");
            Renderer renderer = flag.GetComponent<Renderer>();
            Assert.AreEqual(TeamColor.For(FactionId.Enemy2), renderer.sharedMaterial.color);
        }

        [Test]
        public void AttachToBoat_AddsRendererColoredForFaction()
        {
            var root = new GameObject("BoatRoot");
            _spawned.Add(root);
            var bounds = new Bounds(Vector3.zero, new Vector3(1.2f, 0.5f, 3.2f));

            TeamColorAccent.AttachToBoat(root.transform, bounds, FactionId.Player);

            Renderer renderer = root.GetComponentInChildren<Renderer>();
            Assert.IsNotNull(renderer);
            Assert.AreEqual(TeamColor.For(FactionId.Player), renderer.sharedMaterial.color);
        }
    }
}
