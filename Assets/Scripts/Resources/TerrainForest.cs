using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Multiplayer;

namespace KingdomsOfBharat.ResourceGathering
{
    // Owns the master list of terrain TreeInstances (rendered by Nature
    // Renderer / the Terrain system) and pushes removals back to the terrain.
    // Trees are only data, so gatherability lives on ForestProxy nodes; when a
    // proxy is harvested it asks this class to drop trees. Removals are batched:
    // the whole list is rewritten at most once a second, and only when dirty.
    public class TerrainForest : MonoBehaviour
    {
        private const float FlushInterval = 1f;

        private Terrain _terrain;
        private TreeInstance[] _all;
        private bool[] _dead;
        private bool _dirty;
        private float _nextFlush;

        public void Initialize(Terrain terrain, List<TreeInstance> trees)
        {
            _terrain = terrain;
            _all = trees.ToArray();
            _dead = new bool[_all.Length];
            _dirty = false;
        }

        public void Kill(int index)
        {
            if (_dead == null || index < 0 || index >= _dead.Length || _dead[index])
            {
                return;
            }

            _dead[index] = true;
            _dirty = true;
        }

        private void Update()
        {
            if (_dirty && Time.unscaledTime >= _nextFlush)
            {
                Flush();
            }
        }

        private void Flush()
        {
            _dirty = false;
            _nextFlush = Time.unscaledTime + FlushInterval;
            if (_terrain == null || _terrain.terrainData == null)
            {
                return;
            }

            int alive = 0;
            for (int i = 0; i < _dead.Length; i++)
            {
                if (!_dead[i])
                {
                    alive++;
                }
            }

            var kept = new TreeInstance[alive];
            int w = 0;
            for (int i = 0; i < _all.Length; i++)
            {
                if (!_dead[i])
                {
                    kept[w++] = _all[i];
                }
            }

            _terrain.terrainData.SetTreeInstances(kept, true);
            _terrain.Flush();
        }
    }

    // Lives on an invisible Wood ResourceNode covering a cell of trees. As the
    // node's wood drops, the same fraction of its trees is removed (random
    // order), and all remaining trees go when the node is destroyed.
    public class ForestProxy : MonoBehaviour
    {
        private TerrainForest _forest;
        private ResourceNode _node;
        private int[] _ids;
        private int _alive;

        public void Initialize(TerrainForest forest, ResourceNode node, List<int> treeIds, DeterministicRandom rng)
        {
            _forest = forest;
            _node = node;
            _ids = treeIds.ToArray();
            _alive = _ids.Length;

            // Fisher-Yates so trees vanish scattered, not from one edge.
            for (int i = _ids.Length - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (_ids[i], _ids[j]) = (_ids[j], _ids[i]);
            }

            _node.Changed += OnChanged;
        }

        private void OnChanged(ResourceNode node)
        {
            int target = Mathf.CeilToInt(_ids.Length * node.RemainingFraction);
            RemoveDownTo(target);
        }

        private void OnDestroy()
        {
            if (_node != null)
            {
                _node.Changed -= OnChanged;
            }

            RemoveDownTo(0);
        }

        private void RemoveDownTo(int target)
        {
            if (_forest == null)
            {
                return;
            }

            while (_alive > target)
            {
                _forest.Kill(_ids[--_alive]);
            }
        }
    }
}
