using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PathLinkType
{
    walk, drop, jump
}

/// <summary>
/// Defines a link between nodes.
/// </summary>
[System.Serializable]
public class PlatformerPathLink {

    public PathLinkType linkType;
    // the parent and end nodes are defined as indexes to ensure serialization.
    public int startingNode;
    public int endNode;
    public float linkScore;
    public float jumpHeight;
    public float jumpSpeed;
    public float multiJumpsRequired;
    public float xDistance;
    public float yDistance;
    public bool dynamicLink;
    public bool open;

    public List<Vector2> jumpArc;

    // needed for a basic link
    public PlatformerPathLink(PathLinkType _linkType, int _startingNode, int _endNode)
    {
        linkType = _linkType;
        startingNode = _startingNode;
        endNode = _endNode;
    }

    // needed for a jump link
    public PlatformerPathLink(PathLinkType _linkType, int _startingNode, int _endNode, float _jumpHeight, float _jumpSpeed, List<Vector2> _jumpArc)
    {
        linkType = _linkType;
        startingNode = _startingNode;
        endNode = _endNode;
        jumpHeight = _jumpHeight;
        jumpSpeed = _jumpSpeed;
        jumpArc = _jumpArc;
    }
}
