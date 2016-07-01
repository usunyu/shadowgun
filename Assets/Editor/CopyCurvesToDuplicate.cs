//
// By using or accessing the source codes or any other information of the Game SHADOWGUN: DeadZone ("Game"),
// you ("You" or "Licensee") agree to be bound by all the terms and conditions of SHADOWGUN: DeadZone Public
// License Agreement (the "PLA") starting the day you access the "Game" under the Terms of the "PLA".
//
// You can review the most current version of the "PLA" at any time at: http://madfingergames.com/pla/deadzone
//
// If you don't agree to all the terms and conditions of the "PLA", you shouldn't, and aren't permitted
// to use or access the source codes or any other information of the "Game" supplied by MADFINGER Games, a.s.
//

using UnityEditor;
using UnityEngine;
using System.Collections;

public class CurvesTransferer
{
const string duplicatePostfix = "_copy";

static void CopyClip(string importedPath, string copyPath)
{
    AnimationClip src = AssetDatabase.LoadAssetAtPath(importedPath, typeof(AnimationClip)) as AnimationClip;
    AnimationClip newClip = new AnimationClip();
    newClip.name = src.name + duplicatePostfix;
    AssetDatabase.CreateAsset(newClip, copyPath);
    AssetDatabase.Refresh();
}

    [MenuItem("Assets/Transfer Clip Curves to Copy")]
    static void CopyCurvesToDuplicate()
    {
        // Get selected AnimationClip
        AnimationClip imported = Selection.activeObject as AnimationClip;
        if (imported == null)
        {
            Debug.Log("Selected object is not an AnimationClip");
            return;
        }

        // Find path of copy
        string importedPath = AssetDatabase.GetAssetPath(imported);
        string copyPath = importedPath.Substring(0, importedPath.LastIndexOf("/"));
        copyPath += "/" + imported.name + duplicatePostfix + ".anim";

        CopyClip(importedPath, copyPath);

        AnimationClip copy = AssetDatabase.LoadAssetAtPath(copyPath, typeof(AnimationClip)) as AnimationClip;
        if (copy == null)
        {
            Debug.Log("No copy found at " + copyPath);
            return;
        }
        // Copy curves from imported to copy
        AnimationClipCurveData[] curveDatas = AnimationUtility.GetAllCurves(imported, true);
        for (int i = 0; i < curveDatas.Length; i++)
        {
			EditorCurveBinding binding = new EditorCurveBinding();
			binding.path = curveDatas[i].path;
			binding.propertyName = curveDatas[i].propertyName;
			binding.type = curveDatas[i].type;

            AnimationUtility.SetEditorCurve(copy, binding, curveDatas[i].curve);
        }

        Debug.Log("Copying curves into " + copy.name + " is done");
    }
}