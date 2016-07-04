using UnityEngine;
using UnityEditor;
using VRPanorama;
using System.Collections;


namespace VRPanorama
{
public class VRPanoramaMenu : MonoBehaviour {
//	private GameObject VRPano;
	[MenuItem("GameObject/VR Panorama Camera", false, 10)]
	static void CreateVRCameraObject(MenuCommand menuCommand) {
		

		GameObject VRPano = PrefabUtility.InstantiatePrefab(Resources.Load("VRPanoramaCamera")) as GameObject;
	 	VRPano.name = "VR Panorama Camera";
		PrefabUtility.DisconnectPrefabInstance (VRPano);
		

	    GameObjectUtility.SetParentAndAlign(VRPano, menuCommand.context as GameObject);
		

 		Undo.RegisterCreatedObjectUndo(VRPano, "Create " + VRPano.name);
		Selection.activeObject = VRPano;
	}

	

	}
}
