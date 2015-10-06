using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace PlatformerPathfinder2D
{
    /// <summary>
    /// Handles the generation and display of grid data.
    /// 
    /// CURRENT ISSUES:
    /// Mass jump link generation is slow and often highly redundant.
    /// Jump link generation doesn't verify otherwise valid jumps.
    /// Node from world point is increasingly innaccurate the further away from the center of the grid the position is.
    /// </summary>
    [ExecuteInEditMode]
    [System.Serializable]
    public class PlatformerPathGrid : MonoBehaviour
    {
        //we are serializing this data, so it needs to be in a scriptableobject
        public PlatPathGridData gridData;

        [Header("Debug")]
        public bool displayGizmos;
        public float debugSphereSize;
        public bool displayBlocked;
        public bool displayBlank;
        public bool displayBasicLinks;
        public bool displayJumpLinks;
        public Transform player;

        [Header("Grid Settings")]
        public LayerMask unnavigatableMask;
        public LayerMask platformMask;
        public Vector2 gridSize;
        public float nodeRadius;
        public float raycastCheckLength;
        public float gridSpacing;
        public float standingSpaceRayLength;

        [Header("Jump Calculation Settings")]
        public float gravity = -25;
        public float minJumpHeight = 1;
        public float minRunSpeed = 3;
        public float maxJumpHeight = 12;
        public float maxRunSpeed = 12;
        public float smoothedMovementFactor = 5f;
        public float maxMultiJumps = 1;
        public Vector2 jumpStartOffset;
        [SerializeField]
        [HideInInspector]
        float nodeDiameter;
        [SerializeField]
        [HideInInspector]
        int gridSizeX, gridSizeY;
        [SerializeField]
        Vector2 gridWorldSize;

        Vector2 worldBottomLeft;

        void Start()
        {
            nodeDiameter = nodeRadius * 2;
        }

        void Update()
        {
            gridSizeX = Mathf.RoundToInt(gridSize.x);
            gridSizeY = Mathf.RoundToInt(gridSize.y);
            gridWorldSize.x = gridSize.x * gridSpacing;
            gridWorldSize.y = gridSize.y * gridSpacing;
            worldBottomLeft = transform.position;
        }

        public int MaxSize
        {
            get
            {
                return gridSizeX * gridSizeY;
            }
        }

        public void CreateNodes()
        {
            //Debug.Log("Grid started!");
            gridData.grid = new List<PlatformerPathNode>();
            
            // create the grid of nodes
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int x = 0; x < gridSizeX; x++)
                {
                    // set the node point
                    Vector2 worldPoint = new Vector2(worldBottomLeft.x + (x * gridSpacing) + nodeRadius, worldBottomLeft.y + (y * gridSpacing) + nodeRadius);

                    // check if the node isn't a solid area
                    bool navigatable = !(Physics2D.CircleCast(worldPoint, nodeRadius, Vector2.zero, 0.0f, unnavigatableMask));
                    PlatNodeType nodeType = PlatNodeType.none;

                    // identify blocked off areas
                    if (!navigatable)
                    {
                        nodeType = PlatNodeType.blocked;
                    }

                    // check if there is a walkable surface/platform below, and grab its normal
                    Vector2 platNormal = Vector2.zero;
                    float angle = 0;
                    RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.down, raycastCheckLength, unnavigatableMask);
                    bool standingSpace = false;
                    if (hit.collider != null && nodeType != PlatNodeType.blocked)
                    {
                        platNormal = hit.normal;
                        angle = Mathf.Abs(Mathf.Atan2(platNormal.x, platNormal.y) * Mathf.Rad2Deg);
                        Debug.Log(angle);

                        // use the normal to check for a slope
                        if (angle == 0.0f)
                        {
                            nodeType = PlatNodeType.solid;

                            if ((!(Physics2D.CircleCast(worldPoint + new Vector2(-gridSpacing, -gridSpacing), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask))
                                && !(Physics2D.CircleCast(worldPoint + new Vector2(gridSpacing, -gridSpacing), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask)))
                                || ((Physics2D.CircleCast(worldPoint + new Vector2(-gridSpacing, 0), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask))
                                && (Physics2D.CircleCast(worldPoint + new Vector2(gridSpacing, 0), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask))))
                            {
                                nodeType = PlatNodeType.soloSolid;
                                Debug.Log(nodeType + " " + worldPoint);
                            }
                            else if (!(Physics2D.CircleCast(worldPoint + new Vector2(-gridSpacing, -gridSpacing), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask))
                                && (Physics2D.CircleCast(worldPoint + new Vector2(gridSpacing, -gridSpacing), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask)))
                            {
                                nodeType = PlatNodeType.leftEdgeSolid;
                            }
                            else if ((Physics2D.CircleCast(worldPoint + new Vector2(-gridSpacing, -gridSpacing), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask))
                                && !(Physics2D.CircleCast(worldPoint + new Vector2(gridSpacing, -gridSpacing), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask)))
                            {
                                nodeType = PlatNodeType.rightEdgeSolid;
                            }

                        }
                        else if (angle > 0.0f)
                        {
                            nodeType = PlatNodeType.slopeSolid;
                        }
                        standingSpace = true;
                        if (Physics2D.Raycast(worldPoint, Vector2.up, standingSpaceRayLength, unnavigatableMask))
                        {
                            standingSpace = false;
                        }
                    }
                    // check for a platform
                    if (Physics2D.Raycast(worldPoint, Vector2.down, raycastCheckLength, platformMask))
                    {
                        nodeType = PlatNodeType.platform;
                        standingSpace = true;
                        if (Physics2D.Raycast(worldPoint, Vector2.up, standingSpaceRayLength, unnavigatableMask))
                        {
                            standingSpace = false;
                        }
                    }
                    // create the grid
                    gridData.grid.Add(new PlatformerPathNode(navigatable, worldPoint, new Vector2(x, y), gridData.grid.Count, nodeType, platNormal, angle, standingSpace));
                }
            }

            EditorUtility.SetDirty(gridData);
        }

        // refresh the nodes without destroying links
        public void RefreshNodes()
        {
            gridSizeX = Mathf.RoundToInt(gridWorldSize.x);
            gridSizeY = Mathf.RoundToInt(gridWorldSize.y);

            // establish the grid size and the starting point
            Vector2 worldBottomLeft = transform.position;
            int index = 0;
            // create the grid of nodes
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int x = 0; x < gridSizeX; x++)
                {
                    // set the node point
                    Vector2 worldPoint = new Vector2(worldBottomLeft.x + (x * gridSpacing) + nodeRadius, worldBottomLeft.y + (y * gridSpacing) + nodeRadius);

                    // check if the node isn't a solid area
                    bool navigatable = !(Physics2D.CircleCast(worldPoint, nodeRadius, Vector2.zero, 0.0f, unnavigatableMask));
                    PlatNodeType nodeType = PlatNodeType.none;

                    // identify blocked off areas
                    if (!navigatable)
                    {
                        nodeType = PlatNodeType.blocked;
                    }

                    // check if there is a walkable surface/platform below, and grab its normal
                    Vector2 platNormal = Vector2.zero;
                    float angle = 0;
                    RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.down, raycastCheckLength, unnavigatableMask);
                    bool standingSpace = false;
                    if (hit.collider != null && nodeType != PlatNodeType.blocked)
                    {
                        platNormal = hit.normal;
                        //angle = Vector2.Angle(worldPoint, platNormal);
                        angle = Mathf.Abs(Mathf.Atan2(platNormal.x, platNormal.y) * Mathf.Rad2Deg);
                        //angle = 1f - Vector2.Dot(platNormal, Vector2.up);
                        Debug.Log(angle);

                        // use the normal to check for a slope
                        if (angle == 0.0f)
                        {
                            nodeType = PlatNodeType.solid;

                            if ((!(Physics2D.CircleCast(worldPoint + new Vector2(-gridSpacing, -gridSpacing), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask))
                                && !(Physics2D.CircleCast(worldPoint + new Vector2(gridSpacing, -gridSpacing), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask)))
                                || ((Physics2D.CircleCast(worldPoint + new Vector2(-gridSpacing, 0), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask))
                                && (Physics2D.CircleCast(worldPoint + new Vector2(gridSpacing, 0), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask))))
                            {
                                nodeType = PlatNodeType.soloSolid;
                                Debug.Log(nodeType + " " + worldPoint);
                            }
                            else if (!(Physics2D.CircleCast(worldPoint + new Vector2(-gridSpacing, -gridSpacing), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask))
                                && (Physics2D.CircleCast(worldPoint + new Vector2(gridSpacing, -gridSpacing), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask)))
                            {
                                nodeType = PlatNodeType.leftEdgeSolid;
                                //Debug.Log(nodeType + " " + worldPoint);
                            }
                            else if ((Physics2D.CircleCast(worldPoint + new Vector2(-gridSpacing, -gridSpacing), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask))
                                && !(Physics2D.CircleCast(worldPoint + new Vector2(gridSpacing, -gridSpacing), nodeRadius, Vector2.zero, 0.0f, unnavigatableMask)))
                            {
                                nodeType = PlatNodeType.rightEdgeSolid;
                                //Debug.Log(nodeType + " " + worldPoint);
                            }

                        }
                        else if (angle > 0.0f)
                        {
                            nodeType = PlatNodeType.slopeSolid;
                        }
                        standingSpace = true;
                        if (Physics2D.Raycast(worldPoint, Vector2.up, standingSpaceRayLength, unnavigatableMask))
                        {
                            standingSpace = false;
                        }
                    }
                    // check for a platform
                    if (Physics2D.Raycast(worldPoint, Vector2.down, raycastCheckLength, platformMask))
                    {
                        nodeType = PlatNodeType.platform;
                        standingSpace = true;
                        if (Physics2D.Raycast(worldPoint, Vector2.up, standingSpaceRayLength, unnavigatableMask))
                        {
                            standingSpace = false;
                        }
                    }
                    // refresh the grid
                    gridData.grid[index].navigatable = navigatable;
                    gridData.grid[index].nodeType = nodeType;
                    gridData.grid[index].standingSpace = standingSpace;
                    gridData.grid[index].nodeNormalAngle = angle;
                    index++;
                }
            }

            EditorUtility.SetDirty(gridData);
        }

        public void CreateBasicLinks()
        {
            //// go through the nodes and establish links
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int x = 0; x < gridSizeX; x++)
                {
                    int i = x + (y * gridSizeX);
                    // LEFT EDGE SOLID
                    if (gridData.grid[i].nodeType == PlatNodeType.leftEdgeSolid)
                    {
                        // check right...
                        if (gridData.grid[i + 1].nodeType != PlatNodeType.blocked && gridData.grid[i + 1].nodeType != PlatNodeType.none)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i + 1].gridIndex));
                        }
                        // and check left for a platform. If not, set up a drop.
                        if (gridData.grid[i - 1].nodeType == PlatNodeType.platform)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i - 1].gridIndex));
                        }
                        else
                        {
                            for (int z = y; z > 0; z--)
                            {
                                int i2 = x - 1 + (z * gridSizeX);
                                if (gridData.grid[i2].nodeType != PlatNodeType.none && gridData.grid[i2].nodeType != PlatNodeType.blocked)
                                {
                                    gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.drop, gridData.grid[i].gridIndex, gridData.grid[i2].gridIndex));
                                    break;
                                }
                                else if (gridData.grid[i2].nodeType == PlatNodeType.blocked)
                                {
                                    break;
                                }
                            }
                        }
                    // SOLID REGULAR
                    }
                    else if (gridData.grid[i].nodeType == PlatNodeType.solid)
                    {
                        // check both sides for walkable nodes.
                        if (gridData.grid[i - 1].nodeType != PlatNodeType.blocked && gridData.grid[i - 1].nodeType != PlatNodeType.none)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i - 1].gridIndex));
                        }
                        if (gridData.grid[i + 1].nodeType != PlatNodeType.blocked && gridData.grid[i + 1].nodeType != PlatNodeType.none)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i + 1].gridIndex));
                        }

                        //now check for slope nodes above:
                        // to the left...
                        if (gridData.grid[i - 1 + gridSizeX].nodeType == PlatNodeType.slopeSolid)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i - 1 + gridSizeX].gridIndex));
                        }
                        // and the right.
                        if (gridData.grid[i + 1 + gridSizeX].nodeType == PlatNodeType.slopeSolid)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i + 1 + gridSizeX].gridIndex));
                        }

                        if (x >= 2 && gridData.grid[i - 2 + gridSizeX].nodeType == PlatNodeType.slopeSolid)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i - 2 + gridSizeX].gridIndex));
                        }
                        if (x <= gridSizeX - 2 && gridData.grid[i + 2 + gridSizeX].nodeType == PlatNodeType.slopeSolid)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i + 2 + gridSizeX].gridIndex));
                        }

                    // RIGHT EDGE SOLID
                    }
                    else if (gridData.grid[i].nodeType == PlatNodeType.rightEdgeSolid)
                    {
                        // check left...
                        if (gridData.grid[i - 1].nodeType != PlatNodeType.blocked && gridData.grid[i - 1].nodeType != PlatNodeType.none)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i - 1].gridIndex));
                        }
                        // and check right for a platform. If not, set up a drop.
                        if (gridData.grid[i + 1].nodeType == PlatNodeType.platform)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i + 1].gridIndex));
                        }
                        else
                        {
                            for (int z = y; z > 0; z--)
                            {
                                int i2 = x + 1 + (z * gridSizeX);
                                if (gridData.grid[i2].nodeType != PlatNodeType.none && gridData.grid[i2].nodeType != PlatNodeType.blocked)
                                {
                                    gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.drop, gridData.grid[i].gridIndex, gridData.grid[i2].gridIndex));
                                    break;
                                }
                                else if (gridData.grid[i2].nodeType == PlatNodeType.blocked)
                                {
                                    break;
                                }
                            }
                        }
                    // SOLO SOLID
                    }
                    else if (gridData.grid[i].nodeType == PlatNodeType.soloSolid)
                    {
                        // check for platforms or drops on both sides.
                        if (gridData.grid[i - 1].nodeType == PlatNodeType.platform)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i - 1].gridIndex));
                        }
                        else
                        {
                            int xOffset = x - 1;
                            for (int z = y; z > 0; z--)
                            {
                                int i2 = xOffset + (z * gridSizeX);
                                if (gridData.grid[i2].nodeType != PlatNodeType.none && gridData.grid[i2].nodeType != PlatNodeType.blocked)
                                {
                                    gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.drop, gridData.grid[i].gridIndex, gridData.grid[i2].gridIndex));
                                    break;
                                }
                                else if (gridData.grid[i2].nodeType == PlatNodeType.blocked)
                                {
                                    //Debug.Log("No floor node found! " + xOffset + " " + z);
                                    break;
                                }
                            }
                        }

                        if (gridData.grid[i + 1].nodeType == PlatNodeType.platform)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i + 1].gridIndex));
                        }
                        else
                        {
                            int xOffset = x + 1;
                            for (int z = y; z > 0; z--)
                            {
                                int i2 = xOffset + (z * gridSizeX);
                                if (gridData.grid[i2].nodeType != PlatNodeType.none && gridData.grid[i2].nodeType != PlatNodeType.blocked)
                                {
                                    gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.drop, gridData.grid[i].gridIndex, gridData.grid[i2].gridIndex));
                                    break;
                                }
                                else if (gridData.grid[i2].nodeType == PlatNodeType.blocked)
                                {
                                    //Debug.Log("No floor node found! " + xOffset + " " + z);
                                    break;
                                }
                            }
                        }
                    }
                    // SLOPE SOLID
                    else if (gridData.grid[i].nodeType == PlatNodeType.slopeSolid)
                    {
                        // check for edge and normal solids to the left and right...
                        if (gridData.grid[i - 1].nodeType != PlatNodeType.blocked && gridData.grid[i - 1].nodeType != PlatNodeType.none)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i - 1].gridIndex));
                        }
                        if (gridData.grid[i + 1].nodeType != PlatNodeType.blocked && gridData.grid[i + 1].nodeType != PlatNodeType.none)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i + 1].gridIndex));
                        }

                        // check for edge and normal solids to the left and right...
                        if (gridData.grid[i - 1 - gridSizeX].nodeType != PlatNodeType.blocked && gridData.grid[i - 1 - gridSizeX].nodeType != PlatNodeType.none)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i - 1 - gridSizeX].gridIndex));
                        }
                        if (gridData.grid[i + 1 - gridSizeX].nodeType != PlatNodeType.blocked && gridData.grid[i + 1 - gridSizeX].nodeType != PlatNodeType.none)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i + 1 - gridSizeX].gridIndex));
                        }

                        // check for edge and normal solids to the left and right...
                        if (gridData.grid[i - 1 + gridSizeX].nodeType != PlatNodeType.blocked && gridData.grid[i - 1 + gridSizeX].nodeType != PlatNodeType.none)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i - 1 + gridSizeX].gridIndex));
                        }
                        if (gridData.grid[i + 1 + gridSizeX].nodeType != PlatNodeType.blocked && gridData.grid[i + 1 + gridSizeX].nodeType != PlatNodeType.none)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i + 1 + gridSizeX].gridIndex));
                        }

                        // BELOW CODE CURRENTLY COMMENTED OUT DUE TO BUGS. Only applicable to grid cells that are the same size as tiles, smaller cells will not have this issue. 

                        // now, check for specific nodes due to the angle.
                        if (gridData.grid[i].nodeNormalAngle == 45)
                        {
                            //// to the left...
                            //if (gridData.grid[x - 1+ gridSizeX].nodeType == PlatNodeType.solid || gridData.grid[x - 1+ gridSizeX].nodeType == PlatNodeType.slopeSolid)
                            //{
                            //    gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[x - 1+ gridSizeX].gridIndex));
                            //}
                            ////if (gridData.grid[x - 1- gridSizeX].nodeType == PlatNodeType.solid || gridData.grid[x - 1- gridSizeX].nodeType == PlatNodeType.slopeSolid)
                            ////{
                            ////    gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[x - 1- gridSizeX].gridIndex));
                            ////}

                            //// and the right.
                            //if (gridData.grid[x + 1+ gridSizeX].nodeType == PlatNodeType.solid || gridData.grid[x + 1+ gridSizeX].nodeType == PlatNodeType.slopeSolid)
                            //{
                            //    gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[x + 1+ gridSizeX].gridIndex));
                            //}
                            ////if (gridData.grid[x + 1- gridSizeX].nodeType == PlatNodeType.solid || gridData.grid[x + 1- gridSizeX].nodeType == PlatNodeType.slopeSolid)
                            ////{
                            ////    gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[x + 1- gridSizeX].gridIndex));
                            ////}
                        }
                        else if (gridData.grid[i].nodeNormalAngle >= 20 && gridData.grid[i].nodeNormalAngle <= 30)
                        {
                            //// to the left...
                            //if (x >= 2 && (gridData.grid[x - 2+ gridSizeX].nodeType == PlatNodeType.solid || gridData.grid[x - 2+ gridSizeX].nodeType == PlatNodeType.slopeSolid))
                            //{
                            //    gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[x - 2- gridSizeX].gridIndex));
                            //}
                            ////if (x >= 2 && (gridData.grid[x - 2- gridSizeX].nodeType == PlatNodeType.solid || gridData.grid[x - 2+ gridSizeX].nodeType == PlatNodeType.slopeSolid))
                            ////{
                            ////    gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[x - 2- gridSizeX].gridIndex));
                            ////}

                            //// and the right.
                            //if (x <= gridSizeX - 2 && (gridData.grid[x + 2+ gridSizeX].nodeType == PlatNodeType.solid || gridData.grid[x + 2+ gridSizeX].nodeType == PlatNodeType.slopeSolid))
                            //{
                            //    gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[x + 2+ gridSizeX].gridIndex));
                            //}
                            ////if (x <= gridSizeX - 2 && (gridData.grid[x + 2 - gridSizeX].nodeType == PlatNodeType.solid || gridData.grid[x + 2 - gridSizeX].nodeType == PlatNodeType.slopeSolid))
                            ////{
                            ////    gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[x + 2 - gridSizeX].gridIndex));
                            ////}
                        }
                    }
                    // PLATFORM
                    else if (gridData.grid[i].nodeType == PlatNodeType.platform)
                    {
                        // check for walkables on either side.
                        if (gridData.grid[i - 1].nodeType != PlatNodeType.blocked && gridData.grid[i - 1].nodeType != PlatNodeType.none)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i - 1].gridIndex));
                        }
                        if (gridData.grid[i + 1].nodeType != PlatNodeType.blocked && gridData.grid[i + 1].nodeType != PlatNodeType.none)
                        {
                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.walk, gridData.grid[i].gridIndex, gridData.grid[i + 1].gridIndex));
                        }
                        // check for a drop.
                        for (int z = y - 1; z > 0; z--)
                        {
                            int i2 = x + (z * gridSizeX);
                            if (gridData.grid[i2].nodeType != PlatNodeType.none && gridData.grid[i2].nodeType != PlatNodeType.blocked)
                            {
                                //Debug.Log("Floor node found! " + x + " " + z);
                                gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.drop, gridData.grid[i].gridIndex, gridData.grid[i2].gridIndex));
                                break;
                            }
                            else if (gridData.grid[i2].nodeType == PlatNodeType.blocked)
                            {
                                //Debug.Log("No floor node found! " + x + " " + z);
                                break;
                            }
                        }
                    }
                }
            }
            EditorUtility.SetDirty(gridData);
        }

        public void CreateBasicLinkPair(PlatformerPathNode startNode, PlatformerPathNode endNode, PathLinkType linkType)
        {
            bool linkAlreadyPresent = false;
            for (int i = 0; i < gridData.grid[startNode.gridIndex].links.Count; i++)
            {
                if (gridData.grid[startNode.gridIndex].links[i].endNode == endNode.gridIndex)
                {
                    linkAlreadyPresent = true;
                }
            }
            if (!linkAlreadyPresent)
            {
                gridData.grid[startNode.gridIndex].links.Add(new PlatformerPathLink(linkType, startNode.gridIndex, endNode.gridIndex));
            }
            else
            {
                Debug.Log("A link is already present, bub. Whoops!");
            }

        }

        public void CreateJumpLinks()
        {
            //we do another loop over the grid again for jumping arcs
            for (int i = 0; i < gridData.grid.Count; i++)
            {
                // JUMP LINK CALCULATIONS
                if (gridData.grid[i].nodeType != PlatNodeType.none && gridData.grid[i].nodeType != PlatNodeType.blocked)
                {
                    float jumpForce = Mathf.Sqrt(2f * maxJumpHeight * -gravity);
                    float maximumJumpHeight = jumpForce * maxMultiJumps;

                    for (int i2 = 0; i2 < gridData.grid.Count; i2++)
                    {
                        if (gridData.grid[i2].nodeType != PlatNodeType.none && gridData.grid[i2].nodeType != PlatNodeType.blocked)
                        {
                            bool shouldJump = true;

                            //if the second node is the same as the first, ignore.
                            if (gridData.grid[i].worldPosition == gridData.grid[i2].worldPosition)
                            {
                                shouldJump = false;
                            }

                            // quick check for whether this node already has a link to another node
                            foreach (PlatformerPathLink l in gridData.grid[i].links)
                            {
                                if (l.endNode == gridData.grid[i2].gridIndex)
                                {
                                    shouldJump = false;
                                }
                            }

                            // if all else possibly fails, 
                            foreach (PlatformerPathLink l in gridData.grid[i].links)
                            {
                                FindNode(l.endNode, gridData.grid[i].gridIndex, gridData.grid[i2].gridIndex);
                            }

                            //eliminate areas that are directly below, probably already covered by a drop link
                            if ((gridData.grid[i].gridPosition.x == gridData.grid[i2].gridPosition.x && gridData.grid[i2].gridPosition.y < gridData.grid[i].gridPosition.y)
                                || (Mathf.Abs(gridData.grid[i2].gridPosition.x - gridData.grid[i].gridPosition.x) == 1 && gridData.grid[i2].gridPosition.y < gridData.grid[i].gridPosition.y))
                            {
                                shouldJump = false;
                            }

                            Vector2 currentPos = gridData.grid[i].worldPosition + jumpStartOffset;
                            Vector2 startingPos = gridData.grid[i].worldPosition + jumpStartOffset;
                            Vector2 endPos = gridData.grid[i2].worldPosition;
                            Vector2 velocity = Vector2.zero;
                            Vector2 boxCastsize = new Vector2(0.6f, 1.1f);

                            if (shouldJump)
                            {
                                // defines a list of points for the arc for debug purposes
                                List<Vector2> jumpArc = new List<Vector2>();
                                jumpArc.Add(startingPos);
                                for (float jump = minJumpHeight; jump <= maxJumpHeight; jump++)
                                {
                                    bool validJump = false; bool invalidJump = false;

                                    for (float run = minRunSpeed; run <= maxRunSpeed; run++)
                                    {
                                        currentPos = gridData.grid[i].worldPosition + jumpStartOffset;

                                        velocity = Vector2.zero;
                                        jumpForce = Mathf.Sqrt(2f * jump * -gravity);
                                        velocity.y = jumpForce;
                                        validJump = false; invalidJump = false;

                                        float time = 0f;
                                        jumpArc.Clear();
                                        // defines a jumping arc, step by step.
                                        while (!validJump && !invalidJump)
                                        {
                                            time += 0.01f;

                                            float moveDir = 1;
                                            if (gridData.grid[i2].worldPosition.x < startingPos.x)
                                            {
                                                moveDir = -1;
                                            }

                                            currentPos = startingPos + new Vector2(run * moveDir, jump) * time + new Vector2(0, gravity) * 0.5f * time * time;
                                            jumpArc.Add(currentPos);

                                            Vector2 boxCastPos = currentPos + new Vector2(0, 0.6f);
                                            // checks for collision
                                            if (Physics2D.BoxCast(currentPos, boxCastsize, 0.0f, Vector2.up, 0f, unnavigatableMask))
                                            {
                                                //Debug.Log("This jump failed because collision. " + currentPos + " " + gridData.grid[i2].worldPosition + " " + velocity);
                                                invalidJump = true;
                                                break;
                                            }
                                            // checks if current pos has missed the target
                                            if (gridData.grid[i2].worldPosition.y - currentPos.y > 1 && currentPos.y < -10)
                                            {
                                                //Debug.Log("This jump failed because the currentPos missed and just kept going. " + currentPos + " " + gridData.grid[i2].worldPosition + " " + velocity);
                                                invalidJump = true;
                                                break;
                                            }
                                            // check if within valid distance
                                            if (Vector2.Distance(currentPos, gridData.grid[i2].worldPosition) < 0.6
                                                 && currentPos.y > gridData.grid[i2].worldPosition.y)
                                            {
                                                validJump = true;
                                                break;
                                            }
                                            // if the end point is below and there's an unblocked drop, it's valid.
                                            if (Mathf.Abs(gridData.grid[i2].worldPosition.x - currentPos.x) < 0.5 && currentPos.y > gridData.grid[i2].worldPosition.y
                                                && !Physics2D.BoxCast(currentPos, boxCastsize, Vector2.Distance(currentPos, endPos), Vector2.down, 0f, unnavigatableMask))
                                            {
                                                validJump = true;
                                                break;
                                            }
                                        }

                                        if (validJump)
                                        {
                                            gridData.grid[i].links.Add(new PlatformerPathLink(PathLinkType.jump, gridData.grid[i].gridIndex, gridData.grid[i2].gridIndex, jump, run, jumpArc));
                                            //Debug.Log("Link found. Starting node: " + grid[x, y].worldPosition + " End Node: " + grid[x2, y2].worldPosition + " Current Pos: " + currentPos);
                                            break;
                                        }
                                    }

                                    if (validJump)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // performs a check for a single pair of nodes.
        public void CreateJumpLinkPair(bool overrideCheck, PlatformerPathNode startNode, PlatformerPathNode endNode, float runMin, float runMax, float jumpMin, float jumpMax)
        {
            bool linkAlreadyPresent = false;
            for (int i = 0; i < gridData.grid[startNode.gridIndex].links.Count; i++)
            {
                if (gridData.grid[startNode.gridIndex].links[i].endNode == endNode.gridIndex)
                {
                    linkAlreadyPresent = true;
                }
            }
            if (linkAlreadyPresent)
            {
                Debug.Log("A link is already present, bub. Whoops!");
            }
            else
            {
                // the validation check can be overriden if the designer believes that the jump is valid even if the jump check fails.
                if (overrideCheck)
                {
                    List<Vector2> jumpArc = new List<Vector2>();
                    jumpArc.Add(startNode.worldPosition);
                    jumpArc.Add(endNode.worldPosition);
                    gridData.grid[startNode.gridIndex].links.Add(new PlatformerPathLink(PathLinkType.jump, startNode.gridIndex, endNode.gridIndex, jumpMin, runMin, jumpArc));
                }
                else
                {
                    Vector2 currentPos = startNode.worldPosition;
                    Vector2 startingPos = startNode.worldPosition;
                    Vector2 endPos = endNode.worldPosition;
                    Vector2 velocity = Vector2.zero;
                    Vector2 boxCastsize = new Vector2(0.6f, 1.1f);
                    List<Vector2> jumpArc = new List<Vector2>();
                    jumpArc.Add(startingPos);
                    for (float jump = jumpMin; jump <= jumpMax; jump++)
                    {
                        bool validJump = false; bool invalidJump = false;

                        for (float run = runMin; run <= runMax; run++)
                        {
                            currentPos = startNode.worldPosition;

                            velocity = Vector2.zero;
                            validJump = false; invalidJump = false;

                            float time = 0f;
                            jumpArc.Clear();
                            while (!validJump && !invalidJump)
                            {
                                time += 0.01f;

                                float moveDir = 1;
                                if (endNode.worldPosition.x < startingPos.x)
                                {
                                    moveDir = -1;
                                }

                                currentPos = startingPos + new Vector2(run * moveDir, jump) * time + new Vector2(0, gravity) * 0.5f * time * time;
                                jumpArc.Add(currentPos);
                                velocity = startingPos + new Vector2(run * moveDir, jump) * time + new Vector2(0, gravity) * 0.5f * time * time;

                                Vector2 boxCastPos = currentPos + new Vector2(0, 0.6f);
                                if (Physics2D.BoxCast(currentPos, boxCastsize, 0.0f, Vector2.up, 0f, unnavigatableMask))
                                {
                                    //Debug.Log("This jump failed because collision. " + currentPos + " " + gridData.grid[i2].worldPosition + " " + velocity);
                                    invalidJump = true;
                                    break;
                                }
                                if (endNode.worldPosition.y - currentPos.y > 1 && currentPos.y < -10)
                                {
                                    //Debug.Log("This jump failed because the currentPos missed and just kept going. " + currentPos + " " + gridData.grid[i2].worldPosition + " " + velocity);
                                    invalidJump = true;
                                    break;
                                }
                                if (Vector2.Distance(currentPos, endNode.worldPosition) < 0.6
                                     && currentPos.y > endNode.worldPosition.y)
                                {
                                    validJump = true;
                                    break;
                                }
                                // if the end point is below and there's an unblocked drop, it's valid.
                                if (Mathf.Abs(endNode.worldPosition.x - currentPos.x) < 0.5 && currentPos.y > endNode.worldPosition.y
                                    && !Physics2D.BoxCast(currentPos, boxCastsize, Vector2.Distance(currentPos, endPos), Vector2.down, 0f, unnavigatableMask))
                                {
                                    validJump = true;
                                    break;
                                }
                            }

                            if (validJump)
                            {
                                gridData.grid[startNode.gridIndex].links.Add(new PlatformerPathLink(PathLinkType.jump, startNode.gridIndex, endNode.gridIndex, jump, run, jumpArc));
                                //Debug.Log("Link found. Starting node: " + grid[x, y].worldPosition + " End Node: " + grid[x2, y2].worldPosition + " Current Pos: " + currentPos);
                                break;
                            }
                        }

                        if (validJump)
                        {
                            break;
                        }
                    }
                }
            }
        }

        // a recursive function that attempts to see if a node can be accessed from another node via basic links.
        public bool FindNode(int currentNode, int previousNode, int targetNode)
        {
            if (gridData.grid[currentNode].gridPosition == gridData.grid[targetNode].gridPosition)
            {
                Debug.Log("Found an inappropriate jump.");
                return true;
            }
            foreach (PlatformerPathLink l in gridData.grid[currentNode].links)
            {
                if (gridData.grid[l.endNode].gridPosition != gridData.grid[previousNode].gridPosition && (l.linkType == PathLinkType.walk || l.linkType == PathLinkType.drop))
                {
                    return FindNode(l.endNode, currentNode, targetNode);
                }
            }

            return false;
        }

        // acquires a node from a world position.
        // currently increasingly inaccurate the further away from the center of the grid the position is.
        public PlatformerPathNode NodeFromWorldPoint(Vector2 worldPosition)
        {
            //float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
            //float percentY = (worldPosition.y + gridWorldSize.y / 2) / gridWorldSize.y;
            //Debug.Log(worldPosition);
            float percentX = worldPosition.x / gridWorldSize.x;
            float percentY = worldPosition.y / gridWorldSize.y;
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);
            //Debug.Log("Clamp.");
            int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
            int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

            //Debug.Log("finish!");
            return gridData.grid[x + (gridSizeX * y)];
        }

        // checks for neighbouring valid node
        public PlatformerPathNode FindNeighbouringValidNode(PlatformerPathNode node)
        {
            if (node.gridPosition.x > 0)
            {
                //left...
                if (gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y))].nodeType != PlatNodeType.none
                    && gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y))].nodeType != PlatNodeType.blocked)
                {
                    return gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y))];
                }
            }
            if (node.gridPosition.x < gridSizeX)
            {
                //right...
                if (gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y))].nodeType != PlatNodeType.none
                    && gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y))].nodeType != PlatNodeType.blocked)
                {
                    return gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y))];
                }
            }

            if (node.gridPosition.x < gridSizeX && node.gridPosition.y < gridSizeY)
            {
                // in a clockwise direction, starting from up...
                if (gridData.grid[(int)node.gridPosition.x + (gridSizeX * ((int)node.gridPosition.y + 1))].nodeType != PlatNodeType.none
                    && gridData.grid[(int)node.gridPosition.x + (gridSizeX * ((int)node.gridPosition.y + 1))].nodeType != PlatNodeType.blocked)
                {
                    return gridData.grid[(int)node.gridPosition.x + (gridSizeX * ((int)node.gridPosition.y + 1))];
                }
                // up and right...
                if (gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y + 1))].nodeType != PlatNodeType.none
                    && gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y + 1))].nodeType != PlatNodeType.blocked)
                {
                    return gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y + 1))];
                }
            }

            if (node.gridPosition.x > 0)
            {
                if (node.gridPosition.y < gridSizeY)
                {
                    // up and left...
                    if (gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y + 1))].nodeType != PlatNodeType.none
                    && gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y + 1))].nodeType != PlatNodeType.blocked)
                    {
                        return gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y + 1))];
                    }
                }
            }

            if (node.gridPosition.y > 0)
            {
                if (node.gridPosition.x < gridSizeX)
                {
                    //right and down...
                    if (gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y - 1))].nodeType != PlatNodeType.none
                        && gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y - 1))].nodeType != PlatNodeType.blocked)
                    {
                        return gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y - 1))];
                    }
                }
                //down...
                if (gridData.grid[(int)node.gridPosition.x + (gridSizeX * ((int)node.gridPosition.y - 1))].nodeType != PlatNodeType.none
                    && gridData.grid[(int)node.gridPosition.x + (gridSizeX * ((int)node.gridPosition.y - 1))].nodeType != PlatNodeType.blocked)
                {
                    return gridData.grid[(int)node.gridPosition.x + (gridSizeX * ((int)node.gridPosition.y - 1))];
                }

                if (node.gridPosition.x > 0)
                {
                    //left and down...
                    if (gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y - 1))].nodeType != PlatNodeType.none
                        && gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y - 1))].nodeType != PlatNodeType.blocked)
                    {
                        return gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y - 1))];
                    }
                }
            }

            return null;
        }

        // checks for a neighbouring open node
        public PlatformerPathNode FindNeighbouringOpenNode(PlatformerPathNode node)
        {
            if (node.gridPosition.x < gridSizeX && node.gridPosition.y < gridSizeY)
            {
                // in a clockwise direction, starting from up...
                if (gridData.grid[(int)node.gridPosition.x + (gridSizeX * ((int)node.gridPosition.y + 1))].nodeType == PlatNodeType.none)
                {
                    return gridData.grid[(int)node.gridPosition.x + (gridSizeX * ((int)node.gridPosition.y + 1))];
                }
                // up and right...
                if (gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y + 1))].nodeType == PlatNodeType.none)
                {
                    return gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y + 1))];
                }
            }

            if (node.gridPosition.x < gridSizeX)
            {
                //right...
                if (gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y))].nodeType == PlatNodeType.none)
                {
                    return gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y))];
                }
            }

            if (node.gridPosition.y > 0)
            {
                if (node.gridPosition.x < gridSizeX)
                {
                    //right and down...
                    if (gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y - 1))].nodeType == PlatNodeType.none)
                    {
                        return gridData.grid[(int)node.gridPosition.x + 1 + (gridSizeX * ((int)node.gridPosition.y - 1))];
                    }
                }
                //down...
                if (gridData.grid[(int)node.gridPosition.x + (gridSizeX * ((int)node.gridPosition.y - 1))].nodeType == PlatNodeType.none)
                {
                    return gridData.grid[(int)node.gridPosition.x + (gridSizeX * ((int)node.gridPosition.y - 1))];
                }

                if (node.gridPosition.x > 0)
                {
                    //left and down...
                    if (gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y - 1))].nodeType == PlatNodeType.none)
                    {
                        return gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y - 1))];
                    }
                }
            }

            if (node.gridPosition.x > 0)
            {
                //left...
                if (gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y))].nodeType == PlatNodeType.none)
                {
                    return gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y))];
                }

                if (node.gridPosition.y < gridSizeY)
                {
                    // up and left...
                    if (gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y + 1))].nodeType == PlatNodeType.none)
                    {
                        return gridData.grid[(int)node.gridPosition.x - 1 + (gridSizeX * ((int)node.gridPosition.y + 1))];
                    }
                }
            }

            return null;
        }

        // checks for a valid node below the input node
        public PlatformerPathNode FindValidNodeBelow(PlatformerPathNode node)
        {
            for (int y = (int)node.gridPosition.y - 1; y <= 0; y--)
            {
                if (gridData.grid[(int)node.gridPosition.x + (gridSizeX * y)].nodeType != PlatNodeType.none
                    && gridData.grid[(int)node.gridPosition.x + (gridSizeX * y)].nodeType != PlatNodeType.blocked)
                {
                    return gridData.grid[(int)node.gridPosition.x + (gridSizeX * y)];
                }
                else if (gridData.grid[(int)node.gridPosition.x + (gridSizeX * y)].nodeType == PlatNodeType.blocked)
                {
                    return gridData.grid[(int)node.gridPosition.x + (gridSizeX * (y - 1))];
                }
            }

            return null;
        }

        public void EraseAllLinks()
        {
            for (int i = 0; i < gridData.grid.Count; i++)
            {
                gridData.grid[i].links.Clear();
            }
        }

        public void EraseJumpLinks()
        {
            for (int i = 0; i < gridData.grid.Count; i++)
            {
                for (int l = gridData.grid[i].links.Count - 1; l >= 0; l--)
                {
                    if (gridData.grid[i].links[l].linkType == PathLinkType.jump)
                    {
                        gridData.grid[i].links.RemoveAt(l);
                    }
                }
            }
        }

        // debug display for the grid
        [HideInInspector]
        public List<PlatformerPathNode> path;
        private void OnDrawGizmos()
        {
            if (displayGizmos)
            {
                Gizmos.DrawWireCube(new Vector3(transform.position.x + gridWorldSize.x / 2, transform.position.y + gridWorldSize.y / 2, 0), new Vector2(gridSize.x * gridSpacing, gridSize.y * gridSpacing));

                if (gridData.grid != null)
                {
                    PlatformerPathNode playerNode = NodeFromWorldPoint(player.position);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(playerNode.worldPosition, new Vector3(gridSpacing, gridSpacing));

                    foreach (PlatformerPathNode n in gridData.grid)
                    {
                        if (path != null)
                        {
                            if (path.Contains(n))
                            {
                                Gizmos.color = Color.red;
                                Gizmos.DrawCube(n.worldPosition, new Vector3(gridSpacing, gridSpacing));
                            }
                        }
                        Gizmos.color = (n.nodeType == PlatNodeType.none) ? Color.white
                        : (n.nodeType == PlatNodeType.solid) ? Color.black
                        : (n.nodeType == PlatNodeType.leftEdgeSolid || n.nodeType == PlatNodeType.rightEdgeSolid) ? Color.magenta
                        : (n.nodeType == PlatNodeType.soloSolid) ? Color.gray
                        : (n.nodeType == PlatNodeType.platform) ? Color.yellow
                        : (n.nodeType == PlatNodeType.slopeSolid) ? Color.blue
                        : (n.nodeType == PlatNodeType.blocked) ? Color.red
                        : Color.red;

                        if (n.nodeType == PlatNodeType.none && displayBlank)
                        {
                            Gizmos.DrawSphere(n.worldPosition, debugSphereSize);
                        }
                        else if (n.nodeType == PlatNodeType.blocked && displayBlocked)
                        {
                            Gizmos.DrawSphere(n.worldPosition, debugSphereSize);
                        }
                        else if (n.nodeType != PlatNodeType.none && n.nodeType != PlatNodeType.blocked)
                        {
                            if (!n.standingSpace)
                            {
                                Gizmos.color = new Color(0.25f, 0.55f, 0.7f);
                            }
                            Gizmos.DrawSphere(n.worldPosition, debugSphereSize);
                        }

                        foreach (PlatformerPathLink l in n.links)
                        {
                            Gizmos.color = (l.linkType == PathLinkType.walk) ? Color.yellow
                                : (l.linkType == PathLinkType.drop) ? Color.cyan
                                : (l.linkType == PathLinkType.jump) ? Color.green
                                : Color.blue;
                            if (l.linkType == PathLinkType.jump)
                            {
                                if (displayJumpLinks)
                                {
                                    for (int i = 1; i < l.jumpArc.Count; i++)
                                    {
                                        Gizmos.DrawLine(l.jumpArc[i - 1], l.jumpArc[i]);
                                    }
                                }
                            }
                            else
                            {
                                if (displayBasicLinks)
                                {
                                    Gizmos.DrawLine(gridData.grid[l.startingNode].worldPosition, gridData.grid[l.endNode].worldPosition);
                                }
                            }

                        }

                    }
                }
            }

        }
    }
}


