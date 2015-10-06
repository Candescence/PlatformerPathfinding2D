using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PlatNodeType
{
    none = 0,
    blocked = 1,
    solid = 2,
    leftEdgeSolid = 3,
    rightEdgeSolid = 4,
    slopeSolid = 5,
    platform = 6,
    dynamic = 7,
    soloSolid,
    slopeBottomSolid,
    slopeTopSolid,
    cornerLeft,
    cornerRight,
    wallLeft,
    wallRight,
    wallBothSides
}
// : IHeapItem<PlatformerPathNode>


/// <summary>
/// Defines a node in the PlatformerPathfinding2D grid. Some remnants of the code here is from an attempt to convert it into a heap object that failed.
/// </summary>
[System.Serializable]
public class PlatformerPathNode {

    // This checks if the actual node can be occupied (since these nodes are not strictly "walkable").
    public bool navigatable;
    public bool standingSpace;
    // position on the grid
    public Vector2 worldPosition;
    public Vector2 gridPosition;
    public int gridIndex;
    public int platformIndex;
    // this defines the node type
    public PlatNodeType nodeType;
    public Vector3 nodeNormal;
    public float nodeNormalAngle;

    // defines A* costs.
    public int gCost;
    public int hCost;
    public int parent;

    public List<PlatformerPathLink> links;

    public PlatformerPathNode(bool _navigatable, Vector2 _worldPos, Vector2 _gridPosition, int _gridIndex, PlatNodeType _nodeType, Vector3 _nodeNormal, float _normAngle, bool _standingSpace)
    {
        navigatable = _navigatable;
        worldPosition = _worldPos;
        gridPosition = _gridPosition;
        gridIndex = _gridIndex;
        nodeType = _nodeType;
        nodeNormal = _nodeNormal;
        nodeNormalAngle = _normAngle;
        standingSpace = _standingSpace;
        links = new List<PlatformerPathLink>();
    }

    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    //public int HeapIndex
    //{
    // this bit throws out an inexplicable error.
    //    get
    //    {
    //        return HeapIndex;
    //    }
    //    set
    //    {
    //        HeapIndex = value;
    //    }
    //}

    //public int CompareTo(PlatformerPathNode nodeToCompare)
    //{
    //    int compare = fCost.CompareTo(nodeToCompare.fCost);
    //    if (compare == 0)
    //    {
    //        compare = hCost.CompareTo(nodeToCompare.hCost);
    //    }
    //    return -compare;
    //}

}
