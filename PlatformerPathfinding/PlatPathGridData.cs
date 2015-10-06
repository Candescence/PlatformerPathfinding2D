using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace PlatformerPathfinder2D
{
    /// <summary>
    /// This holds the grid map data, which is useful for editing and using across multiple scenes.
    /// </summary>
    [CreateAssetMenu]
    [System.Serializable]
    public class PlatPathGridData : ScriptableObject
    {
        [SerializeField]
        public List<PlatformerPathNode> grid;
        [SerializeField]
        public List<PlatformerPathNode> dynamicNodes;
    }
}