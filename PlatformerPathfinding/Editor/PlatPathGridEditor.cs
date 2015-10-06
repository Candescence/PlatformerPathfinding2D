using UnityEngine;
using System.Collections;
using UnityEditor;

namespace PlatformerPathfinder2D
{
    /// <summary>
    /// This particular editor script enables in-editor generation and modification of pathfinding grids.
    /// </summary>
    [CustomEditor(typeof(PlatformerPathGrid))]
    public class PlatPathGridEditor : Editor
    {

        PlatformerPathGrid grid;

        public void OnEnable()
        {
            grid = (PlatformerPathGrid)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Generate Node Grid"))
            {
                grid.CreateNodes();
            }

            if (GUILayout.Button("Resfresh Node Grid"))
            {
                grid.RefreshNodes();
            }

            if (GUILayout.Button("Generate Walk/Fall Links"))
            {
                grid.CreateBasicLinks();
            }

            if (GUILayout.Button("Generate Jump Links"))
            {
                grid.CreateJumpLinks();
            }

            if (GUILayout.Button("Erase All Links"))
            {
                grid.EraseAllLinks();
            }

            if (GUILayout.Button("Erase Jump Links"))
            {
                grid.EraseJumpLinks();
            }

            SceneView.RepaintAll();
        }
    }

}