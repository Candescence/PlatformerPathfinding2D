using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Prime31;
using PlatformerPathfinder2D;

/// <summary>
/// A CharacterController2D script used as an example of traversing the grid. Requires Prime31's CharacterController2D asset.
/// 
/// CURRENT ISSUES:
/// Incomplete pathfinding functionality, including hang-ups with certain links, and an unexplained inability to traverse jump links.
/// </summary>
public class Actor2DPathMove : MonoBehaviour
{
    // Gravity
    public float gravity = -25f;
    // running speed
    public float runSpeed = 5f;
    // how fast do we change direction? higher means faster
    public float groundDamping = 20f;
    // how fast do we change direction? higher means faster 
    public float inAirDamping = 5f;
    // Jumping height
    public float jumpHeight = 3f;
    // The maximum distance between the object and the transform before the task can confirm success
    public float distance = 0.25f;
    // If the target is above or below this amount, jump or fall through platforms.
    public float heightBuffer;

    public Transform pathCheck;
    // The transform that the object is moving towards
    public Transform target;

    public Vector2 velocity;
    //public bool onGround;

    public int currentNodeIndex;
    public int currentLinkIndex;

    [HideInInspector]
    private float normalizedHorizontalSpeed = 0;

    private CharacterController2D _controller;
    private Vector3 _velocity;

    public PlatformerPathGrid grid;
    List<PlatformerPathNode> gridData;

    public int currentIndex = 0;

    public List<PlatformerPathNode> path = new List<PlatformerPathNode>();
    public List<PlatformerPathLink> pathLinks = new List<PlatformerPathLink>();
    PlatformerPathLink currentLink = null;

    PlatformerPathNode targetNode;

    public PlatformerPathfinder pathfinder;
    
    public void Awake()
    {
        _controller = GetComponent<CharacterController2D>();
        
        gridData = grid.gridData.grid;
    }

    public void Update()
    {

        if (path.Count > 0)
        {
            if (grid.NodeFromWorldPoint(pathCheck.position) == grid.NodeFromWorldPoint(target.position))
            {
                path.Clear();
            }
            if (grid.NodeFromWorldPoint(pathCheck.position) == path[currentIndex] || Vector2.Distance(pathCheck.position, path[currentIndex].worldPosition) <= grid.gridSpacing)
            {
                currentIndex++;
            }
            for (int i = 0; i < path[currentIndex-1].links.Count; i++)
            {
                if (path[currentIndex-1].links[i].endNode == path[currentIndex].gridIndex)
                {
                    currentLink = path[currentIndex].links[i];
                    currentLinkIndex = currentLink.endNode;
                }
            }

            if (_controller.isGrounded)
                _velocity.y = 0;

            if (path[currentIndex].worldPosition.x < transform.position.x)
            {
                normalizedHorizontalSpeed = -1;
            }
            else if (path[currentIndex].worldPosition.x > transform.position.x)
            {
                normalizedHorizontalSpeed = 1;
            }

            if (currentLink != null && currentLink.linkType == PathLinkType.drop && _controller.isGrounded)
            {
                _velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);
            }
            if (path[currentIndex].worldPosition.y - pathCheck.position.y >= grid.gridSpacing
                && path[currentIndex].worldPosition.x - pathCheck.position.x <= grid.nodeRadius
                && path[currentIndex].worldPosition.x - pathCheck.position.x >= -grid.nodeRadius
                && _controller.isGrounded)
            {
                _velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);
            }

            // apply horizontal speed smoothing it. dont really do this with Lerp. Use SmoothDamp or something that provides more control
            float smoothedMovementFactor = _controller.isGrounded ? groundDamping : inAirDamping; // how fast do we change direction?
            _velocity.x = Mathf.Lerp(_velocity.x, normalizedHorizontalSpeed * runSpeed, Time.deltaTime * smoothedMovementFactor);

            // apply gravity before moving
            _velocity.y += gravity * Time.deltaTime;

            if (currentLink != null && currentLink.linkType == PathLinkType.drop)
            {
                _velocity.y = gravity / 10;
                _controller.ignoreOneWayPlatformsThisFrame = true;
            }

            _controller.move(_velocity * Time.deltaTime);

            // grab our current _velocity to use as a base for all calculations
            _velocity = _controller.velocity;
        } else
        {
            normalizedHorizontalSpeed = 0;
            // apply horizontal speed smoothing it. dont really do this with Lerp. Use SmoothDamp or something that provides more control
            float smoothedMovementFactor = _controller.isGrounded ? groundDamping : inAirDamping; // how fast do we change direction?
            _velocity.x = Mathf.Lerp(_velocity.x, normalizedHorizontalSpeed * runSpeed, Time.deltaTime * smoothedMovementFactor);

            // apply gravity before moving
            _velocity.y += gravity * Time.deltaTime;

            _controller.move(_velocity * Time.deltaTime);

            // grab our current _velocity to use as a base for all calculations
            _velocity = _controller.velocity;
        }

        
    }

    public void NewPath()
    {
        Debug.Log("New path!");
        currentIndex = 1;
        pathfinder.FindPath(pathCheck.position, target.position);
        path = pathfinder.GetPath();
        pathLinks = pathfinder.GetPathLinks();
    }
}
