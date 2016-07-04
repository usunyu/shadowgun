

using UnityEngine;
using System.Collections;
using System.Threading;
using VRPanorama;




//using UnityEditor;

using System;

using System.Net;





namespace VRPanorama


{
	[RequireComponent (typeof (AudioListener))]
public class VRCaptureRT : MonoBehaviour
{
//Render Cubemap Init
		public int cubemapSize = 128;
		public bool oneFacePerFrame = false;
		public Camera cubeCam;
		private RenderTexture rtex;
		private RenderTexture rtexr;
//

	public bool captureAudio = false;
	private	float remainingTime;
	private	int minutesRemain; 
	private	int secondsRemain; 

	public bool alignPanoramaWithHorizont = true;
	private Material VRAA;
	private RenderTexture unfilteredRt;
	

	private GameObject renderHead;
	private GameObject rig;
	private GameObject cam;
	private GameObject panoramaCam;

	

	private RenderTexture flTex;
	private RenderTexture frTex;
	private RenderTexture llTex;
	private RenderTexture lrTex;
	private RenderTexture rlTex;
	private RenderTexture rrTex;
	private RenderTexture dlTex;
	private RenderTexture drTex;
	private RenderTexture blTex;
	private RenderTexture brTex;
	private RenderTexture tlTex;
	private RenderTexture trTex;

	private RenderTexture tlxTex;
	private RenderTexture trxTex;
	private RenderTexture dlxTex;
	private RenderTexture drxTex;

		private Material FL;
		private Material FR;
		private Material LL;
		private Material LR;
		private Material RL;
		private Material RR;
		private Material DL;
		private Material DR;
		private Material BL;
		private Material BR;
		private Material TL;
		private Material TR;

		private Material DLX;
		private Material DRX;
		private Material TLX;
		private Material TRX;


		
		private RenderTexture rt;

	private Texture2D screenShot;




		private Camera camLL;
		private Camera camRL;
		private Camera camTL;
		private Camera camBL;
		private Camera camFL;
		private Camera camDL;
		
		private Camera camLR;
		private Camera camRR;
		private Camera camTR;
		private Camera camBR;
		private Camera camFR;
		private Camera camDR;
		
		private Camera camDRX;
		private Camera camDLX;
		private Camera camTRX;
		private Camera camTLX;

		private GameObject renderPanorama = null;

	
		private GameObject cloneCamLL = null;
		private GameObject cloneCamRL = null;
		private GameObject cloneCamTL = null;
		private GameObject cloneCamBL = null;
		private GameObject cloneCamFL = null;
		private GameObject cloneCamDL = null;
		private GameObject cloneCamLR = null;
		private GameObject cloneCamRR = null;
		private GameObject cloneCamTR = null;
		private GameObject cloneCamBR = null;
		private GameObject cloneCamFR = null;
		private GameObject cloneCamDR = null;
		private GameObject cloneCamDRX = null;
		private GameObject cloneCamDLX = null;
		private GameObject cloneCamTRX = null;
		private GameObject cloneCamTLX = null;


		GameObject camll;
		GameObject camrl;
		GameObject camfl;
		GameObject camtl;
		GameObject cambl;
		GameObject camdl;
	
	
	public enum VRModeList{EquidistantSBS, EquidistantStereo, EquidistantMono};
	[Header("VR Panorama Renderer")]
	public VRModeList panoramaType;

		public enum VRFormatList{JPG, PNG};
		public VRFormatList ImageFormatType;

		[Tooltip("Store PNG sequence in this folder.")]
		public string Folder = "VR_Sequence";


		[Tooltip("Sequence framerate")]
		public int FPS = 25;
		[Tooltip("Sequence resolution")]
		public int resolution = 2048;
		public int resolutionH = 1080;
		
		

		public int NumberOfFramesToRender;
		public int renderFromFrame;

		[VRPanorama] public string sequenceLength;


		public float IPDistance = 0.066f;
		public float EnvironmentDistance = 5;


		public bool openDestinationFolder = true;





		[Header("Make H.264/mp4 Movie")]


		public bool encodeToMp4 = true;
		public int Mp4Bitrate = 12000;


		[Header("RenderTime/Quality Optimization")]
		[Range(1, 16)]
		public int renderQuality = 8;

		public string formatString;
		private int qualityTemp;
		public bool mailme = false;
		public string _mailto = "name@domain.com";
		public string _pass;
		public string _mailfrom = "name@gmail.com";
		public string _prefix = "img";
	
	[VRPanorama] public string RenderInfo = " " ;


		public int bufferSize;
		public int numBuffers;
		private int outputRate = 48000;
		private int headerSize = 44; //default for uncompressed wav
		
		public bool  recOutput;
		



		void Awake(){

			
			
		}

	

		void Start (){


				PreparePano ();
				RenderPano ();
			}




	public void PreparePano () {
			qualityTemp = resolution / 32 * renderQuality;



		GameObject rig = (GameObject)Instantiate(Resources.Load("Rig"));
		rig.name = "Rig";

		rig.transform.SetParent(transform , false);


		cam = gameObject;
		cam.GetComponent<Camera>().fieldOfView = 100;
		cam.GetComponent<Camera>().renderingPath = RenderingPath.DeferredShading;


		float IPDistanceV = -IPDistance;



			
		
		camll = GameObject.Find ("Rig/Left");
		camrl = GameObject.Find ("Rig/Right");
		camfl = GameObject.Find ("Rig/Front");
		camtl = GameObject.Find ("Rig/Top");
		cambl = GameObject.Find ("Rig/Back");
		camdl = GameObject.Find ("Rig/Down");

	
		
		
		
		cloneCamLL = Instantiate (cam);

			Destroy(cloneCamLL.GetComponent(typeof(Animator)));
			Destroy(cloneCamLL.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamLL.GetComponent(typeof(AudioListener)));

		cloneCamRL = Instantiate (cam);
			Destroy(cloneCamRL.GetComponent(typeof(Animator)));
			Destroy(cloneCamRL.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamRL.GetComponent(typeof(AudioListener)));

		cloneCamTL = Instantiate (cam);
			Destroy(cloneCamTL.GetComponent(typeof(Animator)));
			Destroy(cloneCamTL.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamTL.GetComponent(typeof(AudioListener)));

		cloneCamBL = Instantiate (cam);
			Destroy(cloneCamBL.GetComponent(typeof(Animator)));
			Destroy(cloneCamBL.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamBL.GetComponent(typeof(AudioListener)));

		cloneCamFL = Instantiate (cam);
			Destroy(cloneCamFL.GetComponent(typeof(Animator)));
			Destroy(cloneCamFL.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamFL.GetComponent(typeof(AudioListener)));

		cloneCamDL = Instantiate (cam);
			Destroy(cloneCamDL.GetComponent(typeof(Animator)));
			Destroy(cloneCamDL.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamDL.GetComponent(typeof(AudioListener)));
		
		cloneCamLR = Instantiate (cam);
			Destroy(cloneCamLR.GetComponent(typeof(Animator)));
			Destroy(cloneCamLR.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamLR.GetComponent(typeof(AudioListener)));

		cloneCamRR = Instantiate (cam);
			Destroy(cloneCamRR.GetComponent(typeof(Animator)));
			Destroy(cloneCamRR.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamRR.GetComponent(typeof(AudioListener)));

		cloneCamTR = Instantiate (cam);
			Destroy(cloneCamTR.GetComponent(typeof(Animator)));
			Destroy(cloneCamTR.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamTR.GetComponent(typeof(AudioListener)));

		cloneCamBR = Instantiate (cam);
			Destroy(cloneCamBR.GetComponent(typeof(Animator)));
			Destroy(cloneCamBR.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamBR.GetComponent(typeof(AudioListener)));

		cloneCamFR = Instantiate (cam);
			Destroy(cloneCamFR.GetComponent(typeof(Animator)));
			Destroy(cloneCamFR.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamFR.GetComponent(typeof(AudioListener)));

		cloneCamDR = Instantiate (cam);
			Destroy(cloneCamDR.GetComponent(typeof(Animator)));
			Destroy(cloneCamDR.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamDR.GetComponent(typeof(AudioListener)));
			
		cloneCamDRX = Instantiate (cam);
			Destroy(cloneCamDRX.GetComponent(typeof(Animator)));
			Destroy(cloneCamDRX.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamDRX.GetComponent(typeof(AudioListener)));

		cloneCamDLX = Instantiate (cam);
			Destroy(cloneCamDLX.GetComponent(typeof(Animator)));
			Destroy(cloneCamDLX.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamDLX.GetComponent(typeof(AudioListener)));

		cloneCamTRX = Instantiate (cam);
			Destroy(cloneCamTRX.GetComponent(typeof(Animator)));
			Destroy(cloneCamTRX.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamTRX.GetComponent(typeof(AudioListener)));

		cloneCamTLX = Instantiate (cam);
			Destroy(cloneCamTLX.GetComponent(typeof(Animator)));
			Destroy(cloneCamTLX.GetComponent(typeof(VRCaptureRT)));
			Destroy(cloneCamTLX.GetComponent(typeof(AudioListener)));
			
		camLL = cloneCamLL.GetComponent<Camera>();
		camRL = cloneCamRL.GetComponent<Camera>();
		camTL = cloneCamTL.GetComponent<Camera>();
		camBL = cloneCamBL.GetComponent<Camera>();
		camFL = cloneCamFL.GetComponent<Camera>();
		camDL = cloneCamDL.GetComponent<Camera>();
		
		camLR = cloneCamLR.GetComponent<Camera>();
		camRR = cloneCamRR.GetComponent<Camera>();
		camTR = cloneCamTR.GetComponent<Camera>();
		camBR = cloneCamBR.GetComponent<Camera>();
		camFR = cloneCamFR.GetComponent<Camera>();
		camDR = cloneCamDR.GetComponent<Camera>();

		camDRX = cloneCamDRX.GetComponent<Camera>();
		camDLX = cloneCamDLX.GetComponent<Camera>();
		camTRX = cloneCamTRX.GetComponent<Camera>();
		camTLX = cloneCamTLX.GetComponent<Camera>();
		
		cloneCamLL.transform.SetParent(camll.transform , false);
		cloneCamRL.transform.SetParent(camrl.transform , false);
		cloneCamTL.transform.SetParent(camtl.transform , false);
		cloneCamBL.transform.SetParent(cambl.transform , false);
		cloneCamFL.transform.SetParent(camfl.transform , false);
		cloneCamDL.transform.SetParent(camdl.transform , false);

		cloneCamTLX.transform.SetParent(camtl.transform , false);
		cloneCamDLX.transform.SetParent(camdl.transform , false);


			if (panoramaType == VRModeList.EquidistantMono)
				IPDistanceV = 0;

		Vector3 IPD = new Vector3 (IPDistanceV, 0, 0);
		Vector3 IPDX = new Vector3 (0, IPDistanceV, 0);


				cloneCamLL.transform.localPosition = -IPD/2;
				cloneCamRL.transform.localPosition = -IPD/2;
				cloneCamTL.transform.localPosition = -IPD/2 * (-1f);
				cloneCamBL.transform.localPosition = -IPD/2;
				cloneCamFL.transform.localPosition = -IPD/2;
				cloneCamDL.transform.localPosition = -IPD/2;

				cloneCamLR.transform.SetParent(camll.transform , false);
				cloneCamLR.transform.localPosition = IPD/2;
				cloneCamRR.transform.SetParent(camrl.transform , false);
				cloneCamRR.transform.localPosition = IPD/2;
				cloneCamTR.transform.SetParent(camtl.transform , false);
				cloneCamTR.transform.localPosition = IPD/2 * (-1f);
				cloneCamBR.transform.SetParent(cambl.transform , false);
				cloneCamBR.transform.localPosition = IPD/2;
				cloneCamFR.transform.SetParent(camfl.transform , false);
				cloneCamFR.transform.localPosition = IPD/2;
				cloneCamDR.transform.SetParent(camdl.transform , false);
				cloneCamDR.transform.localPosition = IPD/2;

				cloneCamDLX.transform.localPosition = -IPDX/2;
				cloneCamTLX.transform.localPosition = -IPDX/2;

				cloneCamTRX.transform.SetParent(camtl.transform , false);
				cloneCamTRX.transform.localPosition = IPDX/2;
				cloneCamDRX.transform.SetParent(camdl.transform , false);
				cloneCamDRX.transform.localPosition = IPDX/2;
				


		

		
		
		renderHead = (GameObject)Instantiate(Resources.Load("360RenderHead"));
		renderHead.hideFlags = HideFlags.HideInHierarchy;


			if (panoramaType == VRModeList.EquidistantStereo)
		renderPanorama = (GameObject)Instantiate(Resources.Load("360UnwrappedRT"));

			if (panoramaType == VRModeList.EquidistantMono)
				renderPanorama = (GameObject)Instantiate(Resources.Load("360UnwrappedRTMono"));

			if (panoramaType == VRModeList.EquidistantSBS)
				renderPanorama = (GameObject)Instantiate(Resources.Load("360UnwrappedRTSBS"));


		panoramaCam = GameObject.Find ("PanoramaCamera");

			renderPanorama.hideFlags = HideFlags.HideInHierarchy;

	
		


			cloneCamFL.transform.LookAt(camfl.transform.position + camfl.transform.forward * EnvironmentDistance, camfl.transform.up);
			cloneCamFR.transform.LookAt (camfl.transform.position + camfl.transform.forward * EnvironmentDistance, camfl.transform.up);

			cloneCamLL.transform.LookAt(camll.transform.position + camll.transform.forward * EnvironmentDistance, camll.transform.up);
			cloneCamLR.transform.LookAt (camll.transform.position + camll.transform.forward * EnvironmentDistance, camll.transform.up);

			cloneCamRL.transform.LookAt(camrl.transform.position + camrl.transform.forward * EnvironmentDistance, camrl.transform.up);
			cloneCamRR.transform.LookAt (camrl.transform.position + camrl.transform.forward * EnvironmentDistance, camrl.transform.up);

			cloneCamBL.transform.LookAt(cambl.transform.position + cambl.transform.forward * EnvironmentDistance, cambl.transform.up);
			cloneCamBR.transform.LookAt (cambl.transform.position + cambl.transform.forward * EnvironmentDistance, cambl.transform.up);

			cloneCamTL.transform.LookAt(camtl.transform.position + camtl.transform.forward * EnvironmentDistance, camtl.transform.up);
			cloneCamTR.transform.LookAt (camtl.transform.position + camtl.transform.forward * EnvironmentDistance, camtl.transform.up);

			cloneCamDL.transform.LookAt(camdl.transform.position + camdl.transform.forward * EnvironmentDistance, camdl.transform.up);
			cloneCamDR.transform.LookAt (camdl.transform.position + camdl.transform.forward * EnvironmentDistance, camdl.transform.up);


			cloneCamTLX.transform.LookAt(camtl.transform.position + camtl.transform.forward * EnvironmentDistance, camtl.transform.up);
			cloneCamTRX.transform.LookAt (camtl.transform.position + camtl.transform.forward * EnvironmentDistance, camtl.transform.up);
			
			cloneCamDLX.transform.LookAt(camdl.transform.position + camdl.transform.forward * EnvironmentDistance, camdl.transform.up);
			cloneCamDRX.transform.LookAt (camdl.transform.position + camdl.transform.forward * EnvironmentDistance, camdl.transform.up);
			


		
		}

		public void RenderPano () {

			if (panoramaType == VRModeList.EquidistantStereo || panoramaType == VRModeList.EquidistantSBS)
			{
				screenShot = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
			}
			else {
				screenShot = new Texture2D(resolution, resolution/2, TextureFormat.RGB24, false);
			}

			float aAfactor = resolution ;


			flTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
			llTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
			rlTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
			dlTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
			blTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
			tlTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
			

			
			FL = Resources.Load("RTs/Materials/FL") as Material;
			FR = Resources.Load("RTs/Materials/FR") as Material;
			LL = Resources.Load("RTs/Materials/LL") as Material;
			LR = Resources.Load("RTs/Materials/LR") as Material;
			RL = Resources.Load("RTs/Materials/RL") as Material;
			RR = Resources.Load("RTs/Materials/RR") as Material;
			DL = Resources.Load("RTs/Materials/DL") as Material;
			DR = Resources.Load("RTs/Materials/DR") as Material;
			BL = Resources.Load("RTs/Materials/BL") as Material;
			BR = Resources.Load("RTs/Materials/BR") as Material;
			TL = Resources.Load("RTs/Materials/TL") as Material;
			TR = Resources.Load("RTs/Materials/TR") as Material;


			camLL.targetTexture = llTex;
			camRL.targetTexture = rlTex;
			camTL.targetTexture = tlTex;
			camBL.targetTexture = blTex;
			camFL.targetTexture = flTex;
			camDL.targetTexture = dlTex;


			FL.SetFloat("_U", aAfactor);
			FR.SetFloat("_U", aAfactor);
			LL.SetFloat("_U", aAfactor);
			LR.SetFloat("_U", aAfactor);
			RL.SetFloat("_U", aAfactor);
			RR.SetFloat("_U", aAfactor);
			DL.SetFloat("_U", aAfactor);
			DR.SetFloat("_U", aAfactor);
			BL.SetFloat("_U", aAfactor);
			BR.SetFloat("_U", aAfactor);
			TL.SetFloat("_U", aAfactor);
			TR.SetFloat("_U", aAfactor);


			if (panoramaType == VRModeList.EquidistantStereo || panoramaType == VRModeList.EquidistantSBS){

				dlxTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				drxTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				tlxTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				trxTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				
				
				frTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				lrTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				rrTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				drTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				brTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				trTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);


				
				FL.SetTexture("_Main", flTex);
				FR.SetTexture("_Main", frTex);
				LL.SetTexture("_Main", llTex);
				LR.SetTexture("_Main", lrTex);
				RL.SetTexture("_Main", rlTex);
				RR.SetTexture("_Main", rrTex);
				DL.SetTexture("_Main", dlTex);
				DR.SetTexture("_Main", drTex);
				BL.SetTexture("_Main", blTex);
				BR.SetTexture("_Main", brTex);
				TL.SetTexture("_Main", tlTex);
				TR.SetTexture("_Main", trTex);
				
				
				TL.SetTexture("_MainR", trTex);
				TR.SetTexture("_MainR", tlTex);
				DL.SetTexture("_MainR", drTex);
				DR.SetTexture("_MainR", dlTex);
				
				TL.SetTexture("_MainX", trxTex);
				TR.SetTexture("_MainX", tlxTex);
				TL.SetTexture("_MainRX", tlxTex);
				TR.SetTexture("_MainRX", trxTex);
				DL.SetTexture("_MainX", dlxTex);
				DR.SetTexture("_MainX", drxTex);
				DL.SetTexture("_MainRX", drxTex);
				DR.SetTexture("_MainRX", dlxTex);
				
				
				
				camLR.targetTexture = lrTex;
				camRR.targetTexture = rrTex;
				camTR.targetTexture = trTex;
				camBR.targetTexture = brTex;
				camFR.targetTexture = frTex;
				camDR.targetTexture = drTex;
				
				
				camDRX.targetTexture = drxTex;
				camDLX.targetTexture = dlxTex;
				camTRX.targetTexture = trxTex;
				camTLX.targetTexture = tlxTex;
				
				
				
			}
			
			else {	
				
				
				FL.SetTexture("_Main", flTex);
				FR.SetTexture("_Main", flTex);
				LL.SetTexture("_Main", llTex);
				LR.SetTexture("_Main", llTex);
				RL.SetTexture("_Main", rlTex);
				RR.SetTexture("_Main", rlTex);
				DL.SetTexture("_Main", dlTex);
				DR.SetTexture("_Main", dlTex);
				BL.SetTexture("_Main", blTex);
				BR.SetTexture("_Main", blTex);
				TL.SetTexture("_Main", tlTex);
				TR.SetTexture("_Main", tlTex);
				
				
				
				
				TL.SetTexture("_MainR", tlTex);
				
				DL.SetTexture("_MainR", dlTex);
				
				
				TL.SetTexture("_MainX", tlTex);
				
				TL.SetTexture("_MainRX", tlTex);
				
				DL.SetTexture("_MainX", dlTex);
				
				DL.SetTexture("_MainRX", dlTex);
				
				
				
				TR.SetTexture("_MainR", tlTex);
				
				DR.SetTexture("_MainR", dlTex);
				
				
				TR.SetTexture("_MainX", tlTex);
				
				TR.SetTexture("_MainRX", tlTex);
				
				DR.SetTexture("_MainX", dlTex);
				
				DR.SetTexture("_MainRX", dlTex);
				
				
				
				
			}


			if(Application.isPlaying){
				
				Time.captureFramerate = FPS;
				System.IO.Directory.CreateDirectory(Folder);
			}

		}

		public void RenderPanoRT () {
			
			if (panoramaType == VRModeList.EquidistantStereo || panoramaType == VRModeList.EquidistantSBS)
			{
				screenShot = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
			}
			else {
				screenShot = new Texture2D(resolution, resolution/2, TextureFormat.RGB24, false);
			}
			
			float aAfactor = resolution ;
			
			
			flTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
			llTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
			rlTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
			dlTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
			blTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
			tlTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
			
			
			
			FL = Resources.Load("RTs/Materials/FL") as Material;
			FR = Resources.Load("RTs/Materials/FR") as Material;
			LL = Resources.Load("RTs/Materials/LL") as Material;
			LR = Resources.Load("RTs/Materials/LR") as Material;
			RL = Resources.Load("RTs/Materials/RL") as Material;
			RR = Resources.Load("RTs/Materials/RR") as Material;
			DL = Resources.Load("RTs/Materials/DL") as Material;
			DR = Resources.Load("RTs/Materials/DR") as Material;
			BL = Resources.Load("RTs/Materials/BL") as Material;
			BR = Resources.Load("RTs/Materials/BR") as Material;
			TL = Resources.Load("RTs/Materials/TL") as Material;
			TR = Resources.Load("RTs/Materials/TR") as Material;
			
			
			camLL.targetTexture = llTex;
			camRL.targetTexture = rlTex;
			camTL.targetTexture = tlTex;
			camBL.targetTexture = blTex;
			camFL.targetTexture = flTex;
			camDL.targetTexture = dlTex;
			
			
			FL.SetFloat("_U", aAfactor);
			FR.SetFloat("_U", aAfactor);
			LL.SetFloat("_U", aAfactor);
			LR.SetFloat("_U", aAfactor);
			RL.SetFloat("_U", aAfactor);
			RR.SetFloat("_U", aAfactor);
			DL.SetFloat("_U", aAfactor);
			DR.SetFloat("_U", aAfactor);
			BL.SetFloat("_U", aAfactor);
			BR.SetFloat("_U", aAfactor);
			TL.SetFloat("_U", aAfactor);
			TR.SetFloat("_U", aAfactor);
			
			
			if (panoramaType == VRModeList.EquidistantStereo || panoramaType == VRModeList.EquidistantSBS){
				
				dlxTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				drxTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				tlxTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				trxTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				
				
				frTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				lrTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				rrTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				drTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				brTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				trTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, 0);
				
				
				
				FL.SetTexture("_Main", flTex);
				FR.SetTexture("_Main", frTex);
				LL.SetTexture("_Main", llTex);
				LR.SetTexture("_Main", lrTex);
				RL.SetTexture("_Main", rlTex);
				RR.SetTexture("_Main", rrTex);
				DL.SetTexture("_Main", dlTex);
				DR.SetTexture("_Main", drTex);
				BL.SetTexture("_Main", blTex);
				BR.SetTexture("_Main", brTex);
				TL.SetTexture("_Main", tlTex);
				TR.SetTexture("_Main", trTex);
				
				
				TL.SetTexture("_MainR", trTex);
				TR.SetTexture("_MainR", tlTex);
				DL.SetTexture("_MainR", drTex);
				DR.SetTexture("_MainR", dlTex);
				
				TL.SetTexture("_MainX", trxTex);
				TR.SetTexture("_MainX", tlxTex);
				TL.SetTexture("_MainRX", tlxTex);
				TR.SetTexture("_MainRX", trxTex);
				DL.SetTexture("_MainX", dlxTex);
				DR.SetTexture("_MainX", drxTex);
				DL.SetTexture("_MainRX", drxTex);
				DR.SetTexture("_MainRX", dlxTex);
				
				
				
				camLR.targetTexture = lrTex;
				camRR.targetTexture = rrTex;
				camTR.targetTexture = trTex;
				camBR.targetTexture = brTex;
				camFR.targetTexture = frTex;
				camDR.targetTexture = drTex;
				
				
				camDRX.targetTexture = drxTex;
				camDLX.targetTexture = dlxTex;
				camTRX.targetTexture = trxTex;
				camTLX.targetTexture = tlxTex;
				
				
				
			}
			
			else {	
				
				
				FL.SetTexture("_Main", flTex);
				FR.SetTexture("_Main", flTex);
				LL.SetTexture("_Main", llTex);
				LR.SetTexture("_Main", llTex);
				RL.SetTexture("_Main", rlTex);
				RR.SetTexture("_Main", rlTex);
				DL.SetTexture("_Main", dlTex);
				DR.SetTexture("_Main", dlTex);
				BL.SetTexture("_Main", blTex);
				BR.SetTexture("_Main", blTex);
				TL.SetTexture("_Main", tlTex);
				TR.SetTexture("_Main", tlTex);
				
				
				
				
				TL.SetTexture("_MainR", tlTex);
				
				DL.SetTexture("_MainR", dlTex);
				
				
				TL.SetTexture("_MainX", tlTex);
				
				TL.SetTexture("_MainRX", tlTex);
				
				DL.SetTexture("_MainX", dlTex);
				
				DL.SetTexture("_MainRX", dlTex);
				
				
				
				TR.SetTexture("_MainR", tlTex);
				
				DR.SetTexture("_MainR", dlTex);
				
				
				TR.SetTexture("_MainX", tlTex);
				
				TR.SetTexture("_MainRX", tlTex);
				
				DR.SetTexture("_MainX", dlTex);
				
				DR.SetTexture("_MainRX", dlTex);
				
				
				
				
			}
			
			

			
		}



		void Update()
		{

		}

		void LateUpdate()
		{
			cloneCamFL.transform.LookAt(camfl.transform.position + camfl.transform.forward * EnvironmentDistance, camfl.transform.up);
			cloneCamFR.transform.LookAt (camfl.transform.position + camfl.transform.forward * EnvironmentDistance, camfl.transform.up);
			
			cloneCamLL.transform.LookAt(camll.transform.position + camll.transform.forward * EnvironmentDistance, camll.transform.up);
			cloneCamLR.transform.LookAt (camll.transform.position + camll.transform.forward * EnvironmentDistance, camll.transform.up);
			
			cloneCamRL.transform.LookAt(camrl.transform.position + camrl.transform.forward * EnvironmentDistance, camrl.transform.up);
			cloneCamRR.transform.LookAt (camrl.transform.position + camrl.transform.forward * EnvironmentDistance, camrl.transform.up);
			
			cloneCamBL.transform.LookAt(cambl.transform.position + cambl.transform.forward * EnvironmentDistance, cambl.transform.up);
			cloneCamBR.transform.LookAt (cambl.transform.position + cambl.transform.forward * EnvironmentDistance, cambl.transform.up);
			
			cloneCamTL.transform.LookAt(camtl.transform.position + camtl.transform.forward * EnvironmentDistance, camtl.transform.up);
			cloneCamTR.transform.LookAt (camtl.transform.position + camtl.transform.forward * EnvironmentDistance, camtl.transform.up);
			
			cloneCamDL.transform.LookAt(camdl.transform.position + camdl.transform.forward * EnvironmentDistance, camdl.transform.up);
			cloneCamDR.transform.LookAt (camdl.transform.position + camdl.transform.forward * EnvironmentDistance, camdl.transform.up);
			
			
			cloneCamTLX.transform.LookAt(camtl.transform.position + camtl.transform.forward * EnvironmentDistance, camtl.transform.up);
			cloneCamTRX.transform.LookAt (camtl.transform.position + camtl.transform.forward * EnvironmentDistance, camtl.transform.up);
			
			cloneCamDLX.transform.LookAt(camdl.transform.position + camdl.transform.forward * EnvironmentDistance, camdl.transform.up);
			cloneCamDRX.transform.LookAt (camdl.transform.position + camdl.transform.forward * EnvironmentDistance, camdl.transform.up);





			if (captureAudio){
			if (Time.timeSinceLevelLoad > NumberOfFramesToRender/FPS ){

				
				}
			}
			else{
			#if !UNITY_5_0
		//		if (!UnityEngine.VR.VRDevice.isPresent && !UnityEngine.VR.VRSettings.enabled ){
			#endif

			


		
				if (alignPanoramaWithHorizont){

					Vector3 angleCorection = gameObject.transform.rotation.eulerAngles;
					angleCorection = new Vector3 (0, angleCorection.y, 0);
					gameObject.transform.rotation = Quaternion.Euler(angleCorection);
				}


						UpdateCubemapL (renderHead);
				if (panoramaType == VRModeList.EquidistantStereo || panoramaType == VRModeList.EquidistantSBS) UpdateCubemapR(GameObject.Find ("RcubemapRender"));

				RenderVRPanorama();
			//	CounterPost ();


			#if !UNITY_5_0

			#endif
			}
		}



		public void RenderVRPanorama()
		{





					SaveScreenshot();


		


	
}





		public void RenderVideo()
		{

			
		}








		public void SaveScreenshot()
	{

			if (ImageFormatType == VRFormatList.JPG)
			{
			string filePath = string.Format("{0}/"+_prefix+"{1:D05}.jpg", Folder, Time.frameCount);
			formatString = ".jpg\"";
			Texture2D screenShot = GetScreenshot(true);
			


			}

			else 
			{
				Texture2D screenShot = GetScreenshot(true);


			}

	}
	

		
		
		public Texture2D GetScreenshot(bool eye)
		{


//			rt = RenderTexture.GetTemporary (resolution, resolution/2, 0);
//
//			if (eye){ 
//				GameObject.Find ("QuadL").transform.localPosition = new Vector3 (0f, 0f, 1.5f);
//				GameObject.Find ("QuadR").transform.localPosition = new Vector3 (0f, 0f, 6.5f);
//				
//			}
//
//			Camera VRCam = panoramaCam.GetComponent<Camera> ();
//			VRCam.targetTexture = rt;
//			VRCam.Render();
//
//			RenderTexture.active = rt;            





			if (panoramaType == VRModeList.EquidistantStereo)
			{
			
//				screenShot.ReadPixels(new Rect(0, 0, resolution, resolution/2), 0, 0);
//		
//
//				GameObject.Find ("QuadL").transform.localPosition = new Vector3 (0f, 0f, 6.5f);
//				GameObject.Find ("QuadR").transform.localPosition = new Vector3 (0f, 0f, 1.5f);
//
//				VRCam.targetTexture = rt;
//				VRCam.Render();
//				
//				RenderTexture.active = rt;
//
//				screenShot.ReadPixels(new Rect(0, 0, resolution, resolution/2), 0, resolution/2);
//				RenderTexture.ReleaseTemporary(rt);





			}
			else {
//				screenShot.ReadPixels(new Rect(0, 0, resolution, resolution/2 ), 0, 0);
//				RenderTexture.ReleaseTemporary(rt); 
			}
			return screenShot;

	}

		public void SendEmail()
		{
			

		}


		void OnDrawGizmos()
		{

//			Handles.RadiusHandle (Quaternion.identity, 
//			                      transform.position, 
//			                      EnvironmentDistance);

		}


		////////////	VIDEO SCREENSHOT	


		public Texture2D GetVideoScreenshot()
		{
			
			

			Camera VRCam = Camera.main ;
			VRCam.targetTexture = unfilteredRt;
			VRCam.Render();
			
			
			
			
			RenderTexture.active = unfilteredRt;            
			VRAA.mainTexture = unfilteredRt;
			VRAA.SetInt("_U", resolution*2);
			VRAA.SetInt("_V", resolutionH*2);
			
			Graphics.Blit (unfilteredRt, rt, VRAA, -1);
			
			screenShot.ReadPixels(new Rect(0, 0, resolution, resolutionH), 0, 0);
			
			
			return screenShot;
			
		}




		public void VideoRenderPrepare () {
			
			VRAA = Resources.Load("Materials/VRAA") as Material;
			
			
			unfilteredRt = new RenderTexture(resolution*2, resolutionH*2, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.sRGB);
			
			
			rt = new RenderTexture(resolution, resolutionH, 0, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);

			screenShot = new Texture2D(resolution, resolutionH, TextureFormat.RGB24, false);
			
			
			

			
			
			
}




		public void SaveScreenshotVideo()
		{
			if (ImageFormatType == VRFormatList.JPG)
			{
				Texture2D screenShot = GetVideoScreenshot();

			}
			
			else 
			{
				Texture2D screenShot = GetVideoScreenshot();

			}
			
		}

		public void DestroyPano()
		{

			
		}



		public void RenderStaticVRPanorama()
		{
			


			SaveScreenshot();



				}
	
				

		public void CounterPost()
		{
		
			
		}






		public void  UpdateCubemapL ( GameObject gObject  ){



			if (!rtex) {	
				rtex = new RenderTexture (resolution/4, resolution/4, 0);
				rtex.isCubemap = true;
				rtex.hideFlags = HideFlags.HideAndDontSave;
				
				rtex.generateMips = false;
				Renderer rend = gObject.GetComponent<Renderer>();
				rend.sharedMaterial.SetTexture ("_Cube", rtex);
			}
			Camera cubeCam = gObject.GetComponent<Camera>();
			cubeCam.transform.position = gObject.transform.position;
			cubeCam.RenderToCubemap (rtex, 63);

			
		}



		public void  UpdateCubemapR ( GameObject gObject  ){
			

			
			if (!rtexr) {	
				rtexr = new RenderTexture (resolution/4, resolution/4, 0);
				rtexr.isCubemap = true;
				rtex.hideFlags = HideFlags.HideAndDontSave;
				
				rtexr.generateMips = false;
				Renderer rend = gObject.GetComponent<Renderer>();
				rend.sharedMaterial.SetTexture ("_Cube", rtexr);
			}
			Camera cubeCam = gObject.GetComponent<Camera>();
			cubeCam.transform.position = gObject.transform.position;
			cubeCam.RenderToCubemap (rtexr, 63);
			
		}

				
		void  StartWriting (string name){

		}
		
		void  OnAudioFilterRead ( float[] data , int channels){
			if(recOutput)
			{

			}
		}
		






	}





}



