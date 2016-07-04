using UnityEngine;
using System.Collections;
using VRPanorama;

namespace VRPanorama{
public class RenderCubemap : MonoBehaviour {

		
		
		
		

			
		public int cubemapSize = 1024;
		public Camera cam;
		private RenderTexture rtex;
		
		void  Start (){

			UpdateCubemap();
		}
		
		void  LateUpdate (){

				UpdateCubemap ();

		}
		
		void  UpdateCubemap (){

			
			if (!rtex) {	
				rtex = new RenderTexture (cubemapSize, cubemapSize, 0);
				rtex.isCubemap = true;
				rtex.hideFlags = HideFlags.HideAndDontSave;

				rtex.generateMips = false;
				Renderer rend = GetComponent<Renderer>();
				rend.sharedMaterial.SetTexture ("_Cube", rtex);
			}
			
			cam.transform.position = transform.position;
			cam.RenderToCubemap (rtex, 63);

		}
		
		void  OnDisable (){
			
			DestroyImmediate (rtex);
		}
	}
}