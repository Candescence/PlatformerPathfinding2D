# PlatformerPathfinding2D
2D platformer pathfinding system for Unity, using a basic A* algorithm.

An open-sourced version of a personal sub-project involving the creation of a 2D platformer-centric varation of A* pathfinding, based off princples from [an article from Yoann Pignole on 2D platformer pathfinding](http://www.gamasutra.com/blogs/YoannPignole/20150427/241995/The_Hobbyist_Coder_3__2D_platformers_pathfinding__part_12.php) and [Sebastian Lague's wonderful A* tutorials.](https://www.youtube.com/watch?v=-L-WgKMFuhE) Pathfinding agent requires [Prime31's CharacterController2D asset](https://github.com/prime31/CharacterController2D) to work.

[Download the example project here, which includes a test map and a basic editor interface. Will be updated over time.](http://www.mediafire.com/download/dqvnc7jnworc216/PlatformerPathfinding2DDemo.zip)

STILL MAJOR ISSUES PRESENT. THE BASIC FUNCTIONALITY STILL NEEDS SOME FIXES. It's not ready for general use yet.

# Structure and Features
The pathfinding functionality uses a ScriptableObject-based saving system (along with a large dose of serialization). This allows the pathfinding grid to not only be editable in-editor in both edit and play modes, but also re-usable across multiple scenes if required. The grid, upon generation, performs automatic assignment of nodes of different types, and the grid can automatically generate links between those nodes.

However, due to the way the grid is structured, it is heavily unsuited for dynamic generation of pathfinding nodes. This could be worked around in time, but at the moment, it's best used for environments that are static in nature.

On the plus side, however, this allows more fine-grained control for the designer.

The pathfinding itself is fairly basic, and doesn't have a complex heurstic. An attempt to implement a heap went awry due to a bug in the heap that couldn't be properly identified.

# Best Practices
Currently, the reccomended size of grid spaces should be about half the size of the tiles in your grid, or the average environmental object if you're not using tilesets, but the grid is designed for use with tilesets. Nodes should have a radius of half the grid space. The smaller size ensures increased pathfinding accuracy for certain link types and allows slope nodes to be closer together to prevent inaccuracies during generation.

It's best to avoid using the jump link generator, as it will take time, and will likely produce an excessive number of redundant links, currently.

# Current Issues

The pathfinding often has issues with some nodes' link lists.
The jump link generation is still somewhat inaccurate.
The AI agent has problems with jump nodes.
The AI agent has indexing problems when reaching the end node.

# The To-Do List

Fix the major bugs preventing basic functionality
Implement dynamic links (for things such as locked doors and other obstacles)
Implement wall nodes
Implement wall-scaling nodes (for wall-climbing/jumping and ledge climbing)

# License

[Attribution-NonCommercial-ShareAlike 3.0 Unported](http://creativecommons.org/licenses/by-nc-sa/3.0/legalcode) with [simple explanation.](http://creativecommons.org/licenses/by-nc-sa/3.0/deed.en_US) You are free to use the CharacterController2D in any and all games that you make. You cannot sell the CharacterController2D directly or as part of a larger game asset.
