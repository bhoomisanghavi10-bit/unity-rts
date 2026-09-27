using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // Farm's visual is a flat, pre-rendered isometric sprite that swaps
    // live between 3 real gameplay states (construction/full/depleted) -
    // see FarmVisual's own header comment. Covers the pure state-selection
    // logic directly via the internal RefreshFromState, same convention
    // Farm.Tick/ConstructionSite's own internal test hooks already use -
    // no Update() loop needed.
    public class FarmVisualTests
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

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        // Farm without a ConstructionSite sibling (matches a plain unit
        // test's own minimal setup - IsComplete falls back to true, same
        // shape FarmTests.cs's own CreateFarm already establishes).
        private (GameObject root, FarmVisual visual) CreateCompleteFarm()
        {
            GameObject root = CreateGameObject("Farm");
            root.AddComponent<FactionMember>().Configure(FactionId.Player);
            root.AddComponent<Farm>();
            GameObject visualGo = FarmVisual.Build(root.transform, 2f);
            return (root, visualGo.GetComponent<FarmVisual>());
        }

        private string TextureNameOf(FarmVisual visual)
        {
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            return renderer.sharedMaterial.mainTexture.name;
        }

        // ResourceStockpile.For(Player) must resolve to a real instance
        // before Farm.Tick's harvest branch runs (it Adds to it directly) -
        // matches FarmTests.cs's own CreateStockpile convention.
        private void CreateStockpile(float wood = 1000f)
        {
            GameObject go = CreateGameObject("Stockpile");
            var stockpile = go.AddComponent<KingdomsOfBharat.ResourceGathering.ResourceStockpile>();
            stockpile.SetTotal(KingdomsOfBharat.ResourceGathering.ResourceType.Wood, wood);
        }

        [Test]
        public void RefreshFromState_UnderConstruction_ShowsConstructionSprite()
        {
            GameObject root = CreateGameObject("Farm");
            root.AddComponent<FactionMember>().Configure(FactionId.Player);
            root.AddComponent<Farm>();
            var site = root.AddComponent<ConstructionSite>();
            site.Configure(10f); // incomplete by default
            GameObject visualGo = FarmVisual.Build(root.transform, 2f);
            FarmVisual visual = visualGo.GetComponent<FarmVisual>();

            visual.RefreshFromState();

            Assert.AreEqual("Construction", TextureNameOf(visual));
        }

        [Test]
        public void RefreshFromState_CompleteAndNotDepleted_ShowsFullSprite()
        {
            (GameObject root, FarmVisual visual) = CreateCompleteFarm();

            visual.RefreshFromState();

            Assert.AreEqual("Full", TextureNameOf(visual));
        }

        [Test]
        public void RefreshFromState_CompleteAndDepleted_ShowsDepletedSprite()
        {
            CreateStockpile();
            (GameObject root, FarmVisual visual) = CreateCompleteFarm();
            Farm farm = root.GetComponent<Farm>();
            farm.BeginWorking(FactionId.Player);
            farm.Tick(10000f); // drain fully

            visual.RefreshFromState();

            Assert.IsTrue(farm.IsDepleted, "Sanity check on the drain.");
            Assert.AreEqual("Depleted", TextureNameOf(visual));
        }

        [Test]
        public void RefreshFromState_TransitionsBackToFull_OnceReseeded()
        {
            CreateStockpile();
            (GameObject root, FarmVisual visual) = CreateCompleteFarm();
            Farm farm = root.GetComponent<Farm>();
            farm.BeginWorking(FactionId.Player);
            farm.Tick(10000f);
            farm.StopWorking();
            visual.RefreshFromState();
            Assert.AreEqual("Depleted", TextureNameOf(visual));

            farm.BeginReseed();
            farm.Tick(10000f);
            visual.RefreshFromState();

            Assert.IsFalse(farm.IsDepleted, "Sanity check on the reseed.");
            Assert.AreEqual("Full", TextureNameOf(visual));
        }

        [Test]
        public void RefreshFromState_DoesNotReassignMaterial_WhenStateUnchanged()
        {
            (GameObject root, FarmVisual visual) = CreateCompleteFarm();
            visual.RefreshFromState();
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            Material before = renderer.sharedMaterial;

            visual.RefreshFromState();

            Assert.AreSame(before, renderer.sharedMaterial,
                "A no-op refresh should not reassign an equivalent-but-different material instance.");
        }

        [Test]
        public void Build_SizesQuadToTheGivenFootprint()
        {
            GameObject root = CreateGameObject("Farm");
            GameObject visualGo = FarmVisual.Build(root.transform, 3.5f);

            Assert.AreEqual(new Vector3(3.5f, 1f, 3.5f), visualGo.transform.localScale);
        }

        [Test]
        public void Build_AddsNoCollider_SoBuildingPlacerAndSelectionManagerRaycastsAreUnaffected()
        {
            GameObject root = CreateGameObject("Farm");
            GameObject visualGo = FarmVisual.Build(root.transform, 2f);

            Assert.IsNull(visualGo.GetComponent<Collider>());
        }

        // Regression: ConstructionSite's squash-to-grow animation
        // (EnsureInitialized/ApplyHeight) is built for a 3D box-shaped
        // visual - every other building's imported model - and assumes
        // localScale.y is a real height. FarmVisual's quad has every
        // vertex at local y=0 (scaling Y does nothing visually) but
        // ConstructionSite's Y-POSITION math doesn't know that, and was
        // observed live sinking a fresh Farm foundation ~0.48 units
        // underground (invisible) for its entire build duration before
        // this fix. ConstructionSite must detect a FarmVisual sibling and
        // skip the animation entirely, leaving the decal at its own
        // built position/scale throughout construction.
        [Test]
        public void ConstructionSite_DoesNotMoveOrScale_TheFarmVisualQuad_WhileUnderConstruction()
        {
            GameObject root = CreateGameObject("Farm");
            root.AddComponent<FactionMember>().Configure(FactionId.Player);
            root.AddComponent<Farm>();
            GameObject visualGo = FarmVisual.Build(root.transform, 2f);
            Vector3 expectedLocalPos = visualGo.transform.localPosition;
            Vector3 expectedLocalScale = visualGo.transform.localScale;

            var site = root.AddComponent<ConstructionSite>();
            site.Configure(10f);
            site.EnsureInitialized();

            Assert.AreEqual(expectedLocalPos, visualGo.transform.localPosition,
                "The flat decal must stay at FarmVisual.Build's own position - not sunk underground by the squash animation.");
            Assert.AreEqual(expectedLocalScale, visualGo.transform.localScale,
                "The flat decal's scale must be untouched by the squash animation.");
        }
    }
}
