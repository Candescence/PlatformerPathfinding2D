using UnityEngine;
using System.Collections;
using UnityEditor;

namespace PlatformerPathfinder2D
{
    /// <summary>
    /// A simple menu item for creating platformer path grid data (though essentially redudant in newer versions of Unity where you can create scriptableobject assets via right-click).
    /// </summary>
    public class MakePlatPathGridData
    {
        [MenuItem("Tools/Create PlatPathGridData")]
        public static void CreateGridData()
        {
            PlatPathGridData gridData = ScriptableObject.CreateInstance<PlatPathGridData>();

            AssetDatabase.CreateAsset(gridData, "Assets/NewPlatPathGridData.asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = gridData;
        }
    }
}


