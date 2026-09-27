using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Shared building-mesh spawn path, mirroring HumanModelFactory: looks
    // for a real model under Resources/Buildings/<resourceName> first, and
    // falls back to a hand-assembled procedural silhouette
    // (ProceduralBuildingFactory - stepped/spired/towered shapes, not just
    // a flat cube) whenever nothing's been imported yet - so every
    // Building factory keeps working, just with a more building-like
    // placeholder shape than a single box, until a real pack exists to
    // source model names from.
    //
    // Every gameplay component (Barracks/Farm/House/TownCenter,
    // ConstructionSite, FactionMember) still gets added by each factory to
    // the returned ROOT object, same as before the model swap -
    // HoverTooltip/SelectionManager read components straight off
    // hit.collider.gameObject with no GetComponentInParent fallback, so
    // the root is where the Collider has to live too, with the actual
    // visual mesh one level deeper as a child (same split HumanModelFactory
    // already uses for units, for the same reason).
    public static class BuildingModelFactory
    {
        private const float GroundClearance = 0.02f;

        // Full visual sweep (2026-08-26, following the ship/Dock bug
        // report): Tower's world bounds at identity rotation were
        // (25.88, 3.78, 7.01) - wider than a highway, barely taller than
        // a House. A tower's height should dominate its footprint, not
        // the other way around. -90 on Z gives (3.78, 25.88, 7.01) - tall
        // and narrow, confirmed visually via scene-view screenshot
        // (upright crenellated tower, not on its side) before committing.
        // Every other building's bounds were sane (footprint > height, or
        // legitimately tall-and-narrow like TownCenter's tiered-temple
        // design, which was already visually confirmed correct in an
        // earlier live Play mode screenshot this session) - Tower was the
        // only real rotation bug among the 9 sourced building models.
        private static readonly System.Collections.Generic.Dictionary<string, Quaternion> ImportRotationCorrections =
            new System.Collections.Generic.Dictionary<string, Quaternion>
            {
                { "Tower", Quaternion.Euler(0f, 0f, -90f) },
            };

        // See the "Front-facing correction" comment at its own call site
        // (inside BuildVisual) for the full writeup. Keyed by
        // "{civId}_{resourceName}" since this is per-civ (each civ's model
        // was authored/exported independently, so there's no reason to
        // expect them to share one correct angle) - TownCenter only for
        // now, per explicit user scoping.
        private static readonly System.Collections.Generic.Dictionary<string, float> FrontFacingYCorrections =
            new System.Collections.Generic.Dictionary<string, float>
            {
                { "Chola_TownCenter", 180f },
                { "Rajput_TownCenter", 180f },
                { "Maurya_TownCenter", 180f },
                { "Vijayanagara_TownCenter", 180f },
                { "Maratha_TownCenter", 270f },
            };

        // Width-vs-footprint correction (ad hoc, 2026-09-28). The low-poly
        // swap sessions (2026-09-27) only ever normalized each model's
        // HEIGHT against the established per-building scale hierarchy -
        // nothing checked the model's actual footprint (X/Z) against the
        // tile-based BuildingFootprint size used for placement/collision/
        // spawn-offset math elsewhere. Measured live: TownCenter is
        // 2.0-3.1x its own 6-tile footprint on every age tier and every
        // civ, Tower's Ancient/Classical/Durg tiers are 2.9-3.6x its
        // 2-tile footprint (Tower's Imperial tier, from an earlier/
        // separate import session, is already a sane 1.76x) - severe
        // enough that units at their normal spawn offset end up visually
        // swallowed by the building's own oversized roof/wall geometry
        // (reported as "can't see my workers, fog is blocking the view" -
        // it wasn't fog, the TownCenter's real mesh was covering them).
        // Every other building sampled (Barracks/House/Market/Dock/the 3
        // drop-off sheds) already sits in a much healthier ~1.0-1.9x
        // range - a modest roof overhang past the walkable footprint is
        // normal there and is left alone.
        //
        // Fixed generically rather than hand-tuning ~20 individual civ/age
        // combinations: any building whose real rendered footprint
        // exceeds MaxFootprintOverhangRatio times its own BuildingFootprint
        // tile size gets scaled down on only its two footprint axes,
        // preserving the already-correct height exactly (see
        // ScaleWidthOnly). Self-correcting for any future model swap or
        // civ/age combination this session didn't individually measure,
        // rather than a table of hand-picked per-asset magic numbers that
        // would go stale the next time art changes. Wall/Gate are
        // deliberately absent below - both are intentionally long, thin,
        // freeform shapes (drag-placed, not tile-square), so this ratio
        // concept doesn't apply to them.
        //
        // A flush 1.0x (tried 2026-09-28) reads too boxy in practice -
        // user-confirmed a modest roof overhang past the walkable tile
        // looks right, back to the originally-picked 1.8x ceiling.
        private const float MaxFootprintOverhangRatio = 1.8f;

        private static readonly System.Collections.Generic.Dictionary<string, float> FootprintWorldSizes =
            new System.Collections.Generic.Dictionary<string, float>
            {
                { "TownCenter", BuildingFootprint.World(BuildingFootprint.TownCenterTiles) },
                { "Tower", BuildingFootprint.World(BuildingFootprint.TowerTiles) },
                { "Barracks", BuildingFootprint.World(BuildingFootprint.BarracksTiles) },
                { "House", BuildingFootprint.World(BuildingFootprint.HouseTiles) },
                { "Market", BuildingFootprint.World(BuildingFootprint.MarketTiles) },
                { "Durg", BuildingFootprint.World(BuildingFootprint.DurgTiles) },
                { "Karmashala", BuildingFootprint.World(BuildingFootprint.KarmashalaTiles) },
                { "Monastery", BuildingFootprint.World(BuildingFootprint.MonasteryTiles) },
                { "LumberCamp", BuildingFootprint.World(BuildingFootprint.DropOffTiles) },
                { "MiningCamp", BuildingFootprint.World(BuildingFootprint.DropOffTiles) },
                { "Mill", BuildingFootprint.World(BuildingFootprint.DropOffTiles) },
                // Dock's footprint isn't square (BuildingFootprint.Attach
                // gets DockFactory's own Vector2(Size.x, Size.z) directly,
                // not a *Tiles constant) - its longer side stands in as
                // the reference length for this ratio check.
                { "Dock", 4f },
            };

        private static void ClampFootprintWidth(GameObject model, string resourceName)
        {
            if (!FootprintWorldSizes.TryGetValue(resourceName, out float footprintWorldSize))
            {
                return;
            }

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float maxWidth = Mathf.Max(bounds.size.x, bounds.size.z);
            float maxAllowed = footprintWorldSize * MaxFootprintOverhangRatio;
            if (maxWidth <= maxAllowed || maxWidth <= 0.0001f)
            {
                return;
            }

            ScaleWidthOnly(model, maxAllowed / maxWidth);
        }

        // Shrinks whichever two local axes actually map to world X/Z once
        // the model's own import rotation correction (if any) is applied,
        // leaving the local axis that maps to world height (Y) completely
        // untouched - rotation-aware so this stays correct for Tower's
        // baked -90 Z correction (which swaps which local axis is "up")
        // without needing a second, axis-specific table.
        private static void ScaleWidthOnly(GameObject model, float factor)
        {
            Quaternion rotation = model.transform.localRotation;
            float verticalityX = Mathf.Abs((rotation * Vector3.right).y);
            float verticalityY = Mathf.Abs((rotation * Vector3.up).y);
            float verticalityZ = Mathf.Abs((rotation * Vector3.forward).y);
            Vector3 scale = model.transform.localScale;

            if (verticalityX >= verticalityY && verticalityX >= verticalityZ)
            {
                model.transform.localScale = new Vector3(scale.x, scale.y * factor, scale.z * factor);
            }
            else if (verticalityZ >= verticalityX && verticalityZ >= verticalityY)
            {
                model.transform.localScale = new Vector3(scale.x * factor, scale.y * factor, scale.z);
            }
            else
            {
                model.transform.localScale = new Vector3(scale.x * factor, scale.y, scale.z * factor);
            }
        }

        // rootPosition is the same "vertical center of the building"
        // convention every factory already computed for its primitive
        // cube (point + Vector3.up * (size.y * 0.5f), or TownCenterFactory's
        // pre-centered spawn position) - kept as the root's own transform
        // so every existing distance/rally-offset call site (IsClear,
        // rallyOffset, etc.) keeps reading the same position it always
        // has, unaffected by whichever visual (real model or procedural
        // shape) ends up under it.
        // Age-aware building visuals (Wave: age-tiered art import): age is
        // optional so the other 11 non-age-tiered factories (Barracks/Dock/
        // Farm/Gate/House/Karmashala/LumberCamp/Market/Mill/MiningCamp/Durg)
        // keep calling Spawn exactly as before, byte-for-byte the same
        // lookup chain. Only TownCenter/Tower/Wall pass a real AgeId.
        // Resource-path convention (see docs/SESSION_LOG.md for the full
        // writeup): shared non-civ age tiers live at
        // Buildings/{resourceName}_{ageId}; civ-specific age tiers (only
        // TownCenter at Durg, per the standing rule) live at
        // Buildings/{civId}/{resourceName}_{ageId}; Imperial keeps the
        // original unsuffixed Buildings/{civId}/{resourceName} path
        // untouched, so none of the 45 existing Imperial prefabs are
        // re-probed differently.
        public static GameObject Spawn(string resourceName, CivilizationId civId, Vector3 rootPosition, Vector3 fallbackSize, Color civColor, AgeId? age = null, FactionId? faction = null)
        {
            GameObject root = new GameObject(resourceName);
            root.transform.position = rootPosition;
            BuildVisual(root, resourceName, civId, age, fallbackSize, civColor, faction);
            return root;
        }

        // Destroys and rebuilds only the visual child + tight BoxCollider
        // on an already-live building root, leaving every gameplay
        // component already on it (Attackable, GarrisonPoint,
        // BuildingAttacker, HealthBar, FactionMember, Repairable,
        // ConstructionSite, SelectionIndicator, AgeTieredBuildingVisual
        // itself, etc.) completely untouched - a pure re-skin, not a
        // re-spawn. Used by AgeTieredBuildingVisual.RefreshAllForFaction
        // for the confirmed-retroactive Age-up re-skin.
        public static void Refresh(GameObject root, string resourceName, CivilizationId civId, AgeId age, Vector3 fallbackSize, Color civColor, FactionId? faction = null)
        {
            Transform existingVisual = root.transform.Find(VisualChildName);
            if (existingVisual != null)
            {
                Object.DestroyImmediate(existingVisual.gameObject);
            }

            foreach (BoxCollider leftover in root.GetComponents<BoxCollider>())
            {
                Object.DestroyImmediate(leftover);
            }

            BuildVisual(root, resourceName, civId, age, fallbackSize, civColor, faction);
        }

        private const string VisualChildName = "Visual";

        private static void BuildVisual(GameObject root, string resourceName, CivilizationId civId, AgeId? age, Vector3 fallbackSize, Color civColor, FactionId? faction = null)
        {
            // Roadmap Section 4.1: each civ should eventually read as a
            // distinct architectural tradition rather than one shared
            // building set with tint. A civ-specific model (once sourced)
            // lands at Buildings/<CivId>/<resourceName> and is tried first;
            // no manifest/registry to maintain - Resources.Load returning
            // null for a civ that doesn't have a model yet just falls
            // through to the shared lookup chain below unchanged, so
            // models can land one civ/building at a time.
            //
            // TownCenter/Barracks/Farm/House are flat .prefab assets right
            // under Resources/Buildings/ (hand-placed there), but models
            // sourced via the Sketchfab import pipeline land nested as
            // Buildings/<name>/<name>/scene.gltf (the importer's own
            // subfolder-per-model convention, same shape HumanModelFactory
            // already accounts for under Resources/human/) - try the flat
            // path first so nothing about the 4 original buildings changes,
            // then fall back to the nested one.
            // Item 49: a third fallback for Dock specifically - it was
            // sourced alongside the ship models into Assets/Resources/
            // Ships/ (a flat extracted prefab, not the nested Buildings/
            // <name>/<name>/scene convention the other imports use) rather
            // than Assets/Resources/Buildings/, since it's naturally part
            // of the same asset-sourcing pass as the boats.
            // Phase 5 gap-close (Wall/Gate): a fourth fallback - these two
            // landed one level shallower (Buildings/<name>/scene.gltf) than
            // the item-47 imports' Buildings/<name>/<name>/scene.gltf, an
            // import-tool convention difference rather than anything this
            // project chose - so a bare Buildings/<name>/scene path is
            // tried too, after the more specific patterns above.
            // Age-aware pass: a non-Imperial age tries its own civ-specific
            // and shared age-suffixed paths FIRST, then falls through to
            // this same original chain unchanged if neither exists yet
            // (so a not-yet-imported age tier degrades gracefully instead
            // of erroring, same "degrade gracefully" spirit as every
            // fallback below it).
            bool hasAgeTier = age.HasValue && age.Value != AgeId.Imperial;
            GameObject prefab = (hasAgeTier ? Resources.Load<GameObject>($"Buildings/{civId}/{resourceName}_{age.Value}") : null)
                ?? (hasAgeTier ? Resources.Load<GameObject>($"Buildings/{resourceName}_{age.Value}") : null)
                ?? Resources.Load<GameObject>($"Buildings/{civId}/{resourceName}")
                ?? Resources.Load<GameObject>($"Buildings/{resourceName}")
                ?? Resources.Load<GameObject>($"Buildings/{resourceName}/{resourceName}/scene")
                ?? Resources.Load<GameObject>($"Buildings/{resourceName}/scene")
                ?? Resources.Load<GameObject>($"Ships/{resourceName}");

            GameObject model;
            if (prefab != null)
            {
                model = Object.Instantiate(prefab, root.transform);
                // Only a real imported pack gets the partial Lerp tint -
                // it has its own diffuse texture/material to partially
                // preserve. Procedural parts already get the civ color
                // outright at creation time (see ProceduralBuildingFactory).
                TintMaterials(model, civColor);
                ShareMaterialsAcrossLods(model);
            }
            else
            {
                model = ProceduralBuildingFactory.Build(resourceName, civColor, root.transform);
            }

            model.name = VisualChildName;
            model.transform.localPosition = Vector3.zero;
            // Only a real imported model can need an import-orientation
            // correction - a procedural fallback shape is already built
            // correctly oriented, so applying a correction meant for a
            // specific sourced model to it (if that model's Resources
            // asset were ever removed) would wrongly rotate a perfectly
            // fine primitive shape instead.
            model.transform.localRotation = prefab != null && ImportRotationCorrections.TryGetValue(resourceName, out Quaternion correction)
                ? correction
                : Quaternion.identity;

            // Front-facing correction (ad hoc, 2026-09-28): ImportRotationCorrections
            // above only ever fixed a model lying on its side/back (upright
            // vs. sideways) - it says nothing about which way the model's
            // own front/entrance faces once it IS upright, and nothing in
            // this project has ever checked that before. Reported live:
            // Chola's TownCenter showed its back (an open storage porch,
            // no door) toward the fixed isometric camera; rotating its
            // Visual child 180 degrees around Y reveals two open wings
            // flanking a central hall with a banner - the real entrance,
            // user-confirmed. Checked the other 4 civs' TownCenters the
            // same way (spawn, rotate through 0/90/180/270, compare which
            // angle shows a real entrance feature - a staircase, an
            // archway, a distinguishing facade - rather than a blank
            // wall): Rajput's grand staircase+archway is unambiguous at
            // 180 too; Maurya and Vijayanagara are less clear-cut (both
            // show real detail on more than one side) but 180 stays
            // consistent with the other civs; Maratha's plain dark bastion
            // shows no clear entrance from any angle, but 270 is the only
            // one with a distinguishing feature (a red-roofed pavilion)
            // instead of a flat wall. Scoped to TownCenter only per
            // explicit user direction - not yet checked for Tower/
            // Barracks/House/Market/Dock, which may have the same issue.
            // Applied as a further per-civ Y spin on top of whatever
            // ImportRotationCorrections already did (composing correctly
            // for every current entry, since none of them touch Y).
            if (prefab != null && FrontFacingYCorrections.TryGetValue(civId + "_" + resourceName, out float frontFacingY))
            {
                model.transform.localRotation *= Quaternion.Euler(0f, frontFacingY, 0f);
            }

            // Same reasoning as the rotation correction above: only a real
            // imported model can be oversized relative to its own gameplay
            // footprint (a procedural fallback shape is already built at
            // exactly fallbackSize). See ClampFootprintWidth's own comment
            // for the specific bug this closes.
            if (prefab != null)
            {
                ClampFootprintWidth(model, resourceName);
            }

            // Imported packs (TownCenter in particular) ship their own
            // Collider baked into the model, sized by whoever authored the
            // pack rather than this project's units - TownCenter's is
            // roughly 13x22x12 world units once its x2 scale is applied,
            // dwarfing the actual building footprint and catching
            // raycasts/clicks on ground far outside the visible silhouette.
            // Stripped so the only Collider left is the tight one added
            // below, sized from the model's actual rendered bounds - same
            // reasoning as ProceduralBuildingFactory's primitive parts.
            foreach (Collider leftover in model.GetComponentsInChildren<Collider>(true))
            {
                Object.Destroy(leftover);
            }

            // TownCenter.prefab was found to have TownCenter/FactionMember/
            // VisionSource baked into it - its root was even still named
            // "TownCenter(Clone)" inside the saved asset, meaning it was
            // created by saving an already-live, already-Instantiate()'d
            // runtime object as a prefab instead of just its visual mesh.
            // Every one of those components only makes sense on the root
            // this method returns (added moments later, once - Faction,
            // VisionSource.Configure, etc. all assume exactly one), never
            // on the purely-visual child under it - a second copy here
            // means two TownCenters/FactionMembers/etc coexisting under
            // one root, contesting the same faction/rally logic. Stripped
            // generally (any MonoBehaviour, not just this specific asset's
            // known offenders) so the same mistake in a future imported
            // pack fails safe instead of silently duplicating gameplay
            // state again.
            // Plain Destroy() on all of them in one pass left TownCenter
            // itself behind (confirmed live) - RallyPoint's [RequireComponent
            // (typeof(Building))] is checked synchronously against the
            // object's CURRENT components, and Destroy() only defers the
            // actual removal to end-of-frame, so at the moment this loop
            // reached TownCenter, RallyPoint (queued for destruction a few
            // iterations earlier, not yet actually gone) still counted as
            // depending on it and blocked the destroy. DestroyImmediate in
            // dependency-safe passes (destroy whatever's still there each
            // pass, stop once nothing changes) removes dependents like
            // RallyPoint before their requirement is ever checked again.
            for (int pass = 0; pass < 4; pass++)
            {
                MonoBehaviour[] remaining = model.GetComponentsInChildren<MonoBehaviour>(true);
                if (remaining.Length == 0)
                {
                    break;
                }

                foreach (MonoBehaviour leftover in remaining)
                {
                    Object.DestroyImmediate(leftover);
                }
            }

            // Ground level under a center-pivoted building of this size -
            // approximate for TownCenter specifically (its spawn Y
            // predates milestone 14's terrain height variation, same
            // latent flat-Y issue unit spawns had before ResolveGroundHeight
            // was added), exact for Barracks/Farm/House (their point is
            // already ground-raycast-resolved by the caller).
            float groundY = root.transform.position.y - fallbackSize.y * 0.5f;
            Bounds bounds = AlignBaseToGround(model, groundY);
            AddBoundsCollider(root, bounds);

            // Wave 5 item 29: parented under the "Visual" child (this
            // method's own `model`), so it's automatically destroyed and
            // re-created alongside it by Refresh above - no separate
            // cleanup path needed for the Age-up re-skin case.
            if (faction.HasValue)
            {
                TeamColorAccent.AttachToBuilding(model.transform, bounds, faction.Value);

                // Wave 5 item 29 follow-up: recolors gilded/metal trim
                // toward the faction's team color using the model's own
                // metallic map, if it has one - a no-op for procedural
                // fallback shapes and any import without a metallic map.
                // See Core/TeamColorBuildingTint.cs.
                // Every LOD level needs the same baked texture or the trim
                // color would pop when the level switches.
                foreach (Renderer renderer in ModelRenderers(model))
                {
                    TeamColorBuildingTint.TryApplyMetallicTrimTint(renderer, faction.Value);
                }
            }
        }

        // Renderers that belong to the model itself: every LOD level, but not the
        // team-color banner/pennant, which is its own always-visible accent.
        private static System.Collections.Generic.List<Renderer> ModelRenderers(GameObject model)
        {
            var result = new System.Collections.Generic.List<Renderer>();
            LODGroup group = model.GetComponent<LODGroup>();
            if (group != null)
            {
                foreach (LOD lod in group.GetLODs())
                    foreach (Renderer r in lod.renderers)
                        if (r != null) result.Add(r);
                return result;
            }
            Renderer first = model.GetComponentInChildren<Renderer>(true);
            if (first != null) result.Add(first);
            return result;
        }

        // TintMaterials instantiates materials per renderer. A LOD building has one renderer
        // per level, so without this a single House would own three identical tinted materials.
        private static void ShareMaterialsAcrossLods(GameObject model)
        {
            if (model.GetComponent<LODGroup>() == null) return;
            var renderers = ModelRenderers(model);
            Material[] tinted = renderers[0].sharedMaterials;
            for (int i = 1; i < renderers.Count; i++)
            {
                Material[] duplicates = renderers[i].sharedMaterials;
                renderers[i].sharedMaterials = tinted;
                foreach (Material m in duplicates) Object.Destroy(m);
            }
        }

        private static Bounds AlignBaseToGround(GameObject model, float groundY)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(model.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            // Some Sketchfab imports (House/Medieval Tavern in particular)
            // ship with their mesh's local origin nowhere near the mesh
            // itself - one baked-in offset was ~13 world units away once
            // scaled. Only correcting Y here left the root (and every
            // system that reads it - Collider, click detection, rally,
            // camera framing) pointing at the model's origin while the
            // visible mesh rendered a dozen units off to the side, which
            // reads as "walked inside broken giant geometry" up close.
            // Recentering X/Z on the root too, not just aligning Y to the
            // ground, makes root position and visible silhouette agree
            // regardless of how a given pack's pivot was authored.
            Vector3 rootPosition = model.transform.parent.position;
            Vector3 correction = new Vector3(
                rootPosition.x - bounds.center.x,
                groundY + GroundClearance - bounds.min.y,
                rootPosition.z - bounds.center.z);
            model.transform.position += correction;
            bounds.center += correction;

            return bounds;
        }

        // Sized/centered from the model's actual rendered bounds rather
        // than the fallback cube's Size, so clicking/hovering the real
        // model (once one exists) hits roughly its true silhouette instead
        // of an arbitrary placeholder box.
        private static void AddBoundsCollider(GameObject root, Bounds worldBounds)
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(worldBounds.center);
            collider.size = worldBounds.size;
        }

        // Runtime-tint placeholder: nudges the pack's own material color
        // toward the civ color at partial strength so an unfamiliar pack
        // still reads through underneath. HumanModelFactory instead swaps
        // in the Kevin Iglesias pack's own pre-baked Red/Yellow/Blue
        // palette materials, because that pack shipped color variants
        // already - worth the same upgrade here once a building pack's
        // exact material setup is known, since a runtime Lerp tint is a
        // weaker substitute for hand-authored palette variants.
        // Phase 5 gap-close: the Gate model's glTFast-imported Shader
        // Graph material has no "_Color" property at all (Unity's
        // Material.color is a convenience wrapper around exactly that
        // name) - every earlier imported pack happened to use a shader
        // that has one, so this went unnoticed until Gate's import spammed
        // a "doesn't have a color property '_Color'" console error on
        // every spawn and silently skipped tinting. glTFast names its own
        // color property "baseColorFactor" (glTF-spec metallic-roughness
        // workflow) instead - tried as a fallback. A third variant showed
        // up on the Wall model's material: glTFast's specular-glossiness
        // workflow shader instead names it "diffuseFactor" - same problem,
        // same fix, one more fallback before giving up on a given material
        // rather than assuming every pack's shader matches.
        private static void TintMaterials(GameObject go, Color civColor)
        {
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.materials)
                {
                    if (material.HasProperty("_Color"))
                    {
                        material.color = Color.Lerp(material.color, civColor, 0.35f);
                    }
                    else if (material.HasProperty("baseColorFactor"))
                    {
                        Color baseColor = material.GetColor("baseColorFactor");
                        material.SetColor("baseColorFactor", Color.Lerp(baseColor, civColor, 0.35f));
                    }
                    else if (material.HasProperty("diffuseFactor"))
                    {
                        Color diffuseColor = material.GetColor("diffuseFactor");
                        material.SetColor("diffuseFactor", Color.Lerp(diffuseColor, civColor, 0.35f));
                    }
                }
            }
        }
    }
}
