using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Prime31;

namespace PlatformerPathfinder2D
{
    /// <summary>
    /// Acquires path data from the grid and sends it back to whatever script needs the relevant data.
    /// 
    /// CURRENT ISSUES: Grid links are often not properly identified, and can cause an index error.
    /// </summary>
    public class PlatformerPathfinder : MonoBehaviour
    {
        public Transform seeker, target;

        public PlatformerPathGrid grid;
        public List<PlatformerPathNode> gridData;

        public List<PlatformerPathNode> path = new List<PlatformerPathNode>();
        public List<PlatformerPathLink> pathLinks = new List<PlatformerPathLink>();

        void Awake()
        {
            grid = GetComponent<PlatformerPathGrid>();
            gridData = grid.gridData.grid;
        }

        public void FindPath(Vector2 startPos, Vector2 targetPos)
        {
            PlatformerPathNode startNode = grid.NodeFromWorldPoint(startPos);
            PlatformerPathNode endNode = grid.NodeFromWorldPoint(targetPos);

            List<PlatformerPathNode> openSet = new List<PlatformerPathNode>();
            HashSet<PlatformerPathNode> closedSet = new HashSet<PlatformerPathNode>();


            PlatformerPathNode startOffset = null;
            PlatformerPathNode endOffset = null;
            if (startNode.nodeType == PlatNodeType.none || startNode.nodeType == PlatNodeType.blocked)
            {
                startOffset = grid.FindNeighbouringValidNode(startNode);
                if (startOffset == null)
                {

                    if (startNode.nodeType == PlatNodeType.none)
                    {
                        startOffset = grid.FindValidNodeBelow(startNode);
                    }
                    else
                    {
                        startOffset = grid.FindNeighbouringOpenNode(startNode);
                        if (startOffset == null && startNode.nodeType == PlatNodeType.blocked)
                        {
                            Debug.Log("Something has gone horribly wrong with the starting node, aborting.");
                        }
                        else
                        {
                            startOffset = grid.FindValidNodeBelow(startOffset);
                        }
                    }
                }
            }

            if (endNode.nodeType == PlatNodeType.none || endNode.nodeType == PlatNodeType.blocked)
            {
                endOffset = grid.FindNeighbouringValidNode(endNode);
                if (endOffset == null)
                {
                    if (endNode.nodeType == PlatNodeType.none)
                    {
                        endOffset = grid.FindValidNodeBelow(endNode);
                    }
                    else
                    {
                        endOffset = grid.FindNeighbouringOpenNode(endNode);
                        if (endOffset == null && endNode.nodeType == PlatNodeType.blocked)
                        {
                            Debug.Log("Something has gone horribly wrong with the target node, aborting.");
                        }
                        else
                        {
                            endOffset = grid.FindValidNodeBelow(endOffset);
                        }
                    }
                }
            }

            if (startOffset != null)
            {
                startNode = startOffset;
            }
            if (endOffset != null)
            {
                endNode = endOffset;
            }

            openSet.Add(startNode);
            while (openSet.Count > 0)
            {
                PlatformerPathNode currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode == endNode)
                {
                    RetracePath(startNode, endNode);
                    return;
                }

                for (int i = 0; i < gridData[currentNode.gridIndex].links.Count; i++)
                {
                    PlatformerPathLink l = gridData[currentNode.gridIndex].links[i];

                    if (closedSet.Contains(gridData[l.endNode]))
                    {
                        continue;
                    }

                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, gridData[l.endNode]);
                    if (newMovementCostToNeighbour < gridData[l.endNode].gCost || !openSet.Contains(gridData[l.endNode]))
                    {
                        gridData[l.endNode].gCost = newMovementCostToNeighbour;
                        gridData[l.endNode].hCost = GetDistance(gridData[l.endNode], endNode);
                        gridData[l.endNode].parent = currentNode.gridIndex;

                        if (!openSet.Contains(gridData[l.endNode]))
                        {
                            openSet.Add(gridData[l.endNode]);
                        }
                    }
                }
            }
        }

        void RetracePath(PlatformerPathNode startNode, PlatformerPathNode endNode)
        {
            path = new List<PlatformerPathNode>();
            pathLinks = new List<PlatformerPathLink>();
            PlatformerPathNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                for (int i = 0; i < gridData[currentNode.parent].links.Count; i++)
                {
                    if (currentNode.links[i].endNode == currentNode.gridIndex)
                    {
                        pathLinks.Add(currentNode.links[i]);
                    }
                }
                currentNode = grid.gridData.grid[currentNode.parent];
            }
            path.Reverse();
            pathLinks.Reverse();
        }

        public int GetDistance(PlatformerPathNode nodeA, PlatformerPathNode nodeB)
        {
            int dstX = (int)Mathf.Abs(nodeA.gridPosition.x - nodeB.gridPosition.x);
            int dstY = (int)Mathf.Abs(nodeA.gridPosition.y - nodeB.gridPosition.y);

            if (dstX > dstY)
            {
                return 14 * dstY + 10 * (dstX - dstY);
            }
            else
            {
                return 14 * dstX + 10 * (dstY - dstX);
            }
        }

        public List<PlatformerPathNode> GetPath()
        {
            return path;
        }

        public List<PlatformerPathLink> GetPathLinks()
        {
            return pathLinks;
        }
    }
}