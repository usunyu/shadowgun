using UnityEngine;
using System.Collections;



namespace VRPanorama {
	[RequireComponent (typeof (VRCapture))]
public class VRCompositor : MonoBehaviour {

	private RenderTexture thisRT;
	private Camera camVR;
	public Camera[] cameraLayers;


	void Awake () {
		camVR = gameObject.GetComponent<Camera>();
		for(int i = 0; i < cameraLayers.Length; i++){
			cameraLayers[i].fieldOfView = 100;	
			cameraLayers[i].renderingPath = RenderingPath.DeferredShading;
		}

	
	}
	

	void Update () {

		thisRT = camVR.targetTexture;
			for(int i = 0; i < cameraLayers.Length; i++){
		cameraLayers[i].targetTexture = thisRT;
		cameraLayers[i].Render();
		}
		
	}
}
}