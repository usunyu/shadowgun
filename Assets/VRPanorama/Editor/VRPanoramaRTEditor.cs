
#if UNITY_EDITOR && !UNITY_WEBPLAYER

using UnityEngine;
using System.Collections;
using UnityEditor;
using VRPanorama;

using System.IO;


namespace VRPanorama {

[CustomEditor(typeof(VRCaptureRT))]
	[RequireComponent (typeof (AudioListener))]

public class VRPanoramaRTEditor : Editor 
{
		private Texture banner = Resources.Load("VRHeaderRT") as Texture;
		private bool changePrefix = false;

	// Use this for initialization
	public override void OnInspectorGUI () {

			VRCaptureRT VRP = (VRCaptureRT)target;




			GUILayout.Box (banner, GUILayout.ExpandWidth(true));
		


			GUILayout.BeginVertical ("box");

			GUILayout.Label ("VR Panorama RT");

			VRP.panoramaType = (VRPanorama.VRCaptureRT.VRModeList)EditorGUILayout.EnumPopup("Panorama Type",VRP.panoramaType);

			VRP.resolution = 2048;






			GUILayout.EndVertical ();



				GUILayout.BeginVertical ("box");
				GUILayout.BeginVertical ("box");
				GUILayout.Label ("Panorama Settings");
				GUILayout.EndVertical ();
				if (VRP.panoramaType == VRPanorama.VRCaptureRT.VRModeList.EquidistantStereo) VRP.IPDistance = EditorGUILayout.FloatField (new GUIContent("IP Distance", "Interpupilar distance"), VRP.IPDistance);
				if (VRP.panoramaType == VRPanorama.VRCaptureRT.VRModeList.EquidistantStereo) VRP.EnvironmentDistance = EditorGUILayout.FloatField (new GUIContent("Environment Distance", "Distance where stiching is perfect: adjust in base of your scene"), VRP.EnvironmentDistance);

				if (VRP.panoramaType == VRPanorama.VRCaptureRT.VRModeList.EquidistantSBS) VRP.IPDistance = EditorGUILayout.FloatField (new GUIContent("IP Distance", "Interpupilar distance"), VRP.IPDistance);
				if (VRP.panoramaType == VRPanorama.VRCaptureRT.VRModeList.EquidistantSBS) VRP.EnvironmentDistance = EditorGUILayout.FloatField (new GUIContent("Environment Distance", "Distance where stiching is perfect: adjust in base of your scene"), VRP.EnvironmentDistance);
 				VRP.alignPanoramaWithHorizont = EditorGUILayout.Toggle (new GUIContent("Align with Horizont", "Forces camera to be aligned with horizont by forcing only rotations on Y axis, usefull fhen using existing animations that have camera X or Z rotations"), VRP.alignPanoramaWithHorizont);
	
				GUILayout.EndVertical ();





			if (VRP.panoramaType == VRPanorama.VRCaptureRT.VRModeList.EquidistantMono)
			{
			GUILayout.BeginVertical ("box");
			GUILayout.Label ("Optimizations");
			VRP.renderQuality = EditorGUILayout.IntSlider("Speed vs.Quality", VRP.renderQuality, 1, 32);
			string q = "Lowest quality";
			if (VRP.renderQuality > 1)
				q = "low quality preview";
			if (VRP.renderQuality > 14)
				q = "optimal";
			if (VRP.renderQuality > 18)
				q = "best and slow";

			GUILayout.Label ("Quality: " + q);
			int size = VRP.resolution / 32 * VRP.renderQuality;
			if (size > 8192)
				size = 8192;
			GUILayout.Label ("One cube side is: " + size + "x" + size );
			GUILayout.Label ("VR Panorama will render " + size * size * 6 / 1000000 + " megapixels per frame");
			GUILayout.Label ("and " + size * size * 6 / 1000000 * (VRP.NumberOfFramesToRender - VRP.renderFromFrame) + " megapixels for a whole animation");


			GUILayout.EndVertical ();
			}

			if (VRP.panoramaType == VRPanorama.VRCaptureRT.VRModeList.EquidistantStereo || VRP.panoramaType == VRPanorama.VRCaptureRT.VRModeList.EquidistantSBS )
			{
				GUILayout.BeginVertical ("box");
				GUILayout.Label ("Optimizations");
				VRP.renderQuality = EditorGUILayout.IntSlider("Speed vs.Quality", VRP.renderQuality, 1, 32);
				string q = "Lowest quality";
				if (VRP.renderQuality > 1)
					q = "low quality preview";
				if (VRP.renderQuality > 14)
					q = "optimal";
				if (VRP.renderQuality > 18)
					q = "best and slow";
				
				GUILayout.Label ("Quality: " + q);
				int size = VRP.resolution / 32 * VRP.renderQuality;
				if (size > 8192)
					size = 8192;
				GUILayout.Label ("One cube side is: " + size + "x" + size );
				GUILayout.Label ("VR Panorama will render " + size * size * 16 / 1000000 + " megapixels per frame");
				
				
				GUILayout.EndVertical ();
			}



			
			




			if (GUI.changed)
				EditorUtility.SetDirty(VRP);
	
	}
	


}
}

#endif

#if UNITY_WEBPLAYER

using UnityEngine;
using System.Collections;
using UnityEditor;
using VRPanorama;

namespace VRPanorama {
	
	[CustomEditor(typeof(VRCapture))]
	
	public class VRPanoramaRTEditor : Editor 
	{
		private Texture banner = Resources.Load("VRHeader") as Texture;
		
		
		// Use this for initialization
		public override void OnInspectorGUI () {
			GUILayout.Box (banner, GUILayout.ExpandWidth(true));
			GUILayout.Label ("VR Panorama can't be initialised on Webplayer platform, please change your buiding mode to Standalone platform");
			Debug.LogError ("VR Panorama can't be initialised on Webplayer platform, please change your buiding mode to Standalone (under Build Settings/Platform/Standalone - Switch platform");
			
		}
	}
}

#endif
