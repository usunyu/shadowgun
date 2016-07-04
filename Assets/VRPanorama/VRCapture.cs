
#if UNITY_EDITOR && !UNITY_WEBPLAYER

using UnityEngine;
using System.Collections;
using System.Threading;
using VRPanorama;
#if !UNITY_5_0
using UnityEngine.VR;
#endif


using UnityEditor;

using System;
using System.IO;
using System.Net;
using System.Net.Mail;



using System.Net.Security;
using System.Security.Cryptography.X509Certificates;



namespace VRPanorama


{
	[RequireComponent (typeof (AudioListener))]
	[RequireComponent (typeof (Camera))]
public class VRCapture : MonoBehaviour
{
//Render Cubemap Init
		public int cubemapSize = 128;
		public bool oneFacePerFrame = false;
		public Camera cubeCam;
		private RenderTexture rtex;
		private RenderTexture rtexr;
//

	public bool captureAudio = false;
	public float volume = 1;
	public bool mute = true;
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

		public int StartFrame = 0;
//		private GameObject cloneCamLL = null;
//		private GameObject cloneCamRL = null;
//		private GameObject cloneCamTL = null;
//		private GameObject cloneCamBL = null;
//		private GameObject cloneCamFL = null;
//		private GameObject cloneCamDL = null;
//		private GameObject cloneCamLR = null;
//		private GameObject cloneCamRR = null;
//		private GameObject cloneCamTR = null;
//		private GameObject cloneCamBR = null;
//		private GameObject cloneCamFR = null;
//		private GameObject cloneCamDR = null;
//		private GameObject cloneCamDRX = null;
//		private GameObject cloneCamDLX = null;
//		private GameObject cloneCamTRX = null;
//		private GameObject cloneCamTLX = null;
	
	
	public enum VRModeList{VideoCapture, EquidistantStereo, EquidistantMono};
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
		public bool depth = false;
		public int depthBufferSize;
		
		private FileStream fileStream;
		public UInt16 achannels;


		void Awake(){


			
		}

	

		void Start (){

			Application.runInBackground = true;
						StartFrame = Time.frameCount;

#if !UNITY_5_0
			UnityEngine.VR.VRSettings.enabled = false;
#endif
			if (mute) AudioListener.volume = 0;
			if (depth == true)
				depthBufferSize = 24;
			else
				depthBufferSize = 0;


			if (captureAudio){
								
								if ((int)AudioSettings.speakerMode == 1) achannels = 1; 
								else if ((int)AudioSettings.speakerMode == 2) achannels = 2; 
								else if ((int)AudioSettings.speakerMode == 3) achannels = 4; 
								else if ((int)AudioSettings.speakerMode == 4) achannels = 5; 
								else if ((int)AudioSettings.speakerMode == 5) achannels = 6; 
								else if ((int)AudioSettings.speakerMode == 6) achannels = 8; 
								Debug.Log ("channels" + (int)AudioSettings.speakerMode + achannels);
				outputRate = AudioSettings.outputSampleRate;
				AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
				AudioListener.volume = volume;

				System.IO.Directory.CreateDirectory(Folder);
				StartWriting(Folder +"/" + Folder + ".wav");
				recOutput = true;
				print("rec start");

		}
			else{	
			if (panoramaType == VRModeList.VideoCapture)
				VideoRenderPrepare();

			else {
				PreparePano ();
				RenderPano ();
			}
			}
		}



	public void PreparePano () {
		qualityTemp = resolution / 32 * renderQuality;



		GameObject rig = (GameObject)Instantiate(Resources.Load("Rig"));
		rig.name = "Rig";

		rig.transform.SetParent(transform , false);


		cam = gameObject;
		cam.GetComponent<Camera>().renderingPath = RenderingPath.DeferredShading;
		cam.GetComponent<Camera>().fieldOfView = 100;


		float IPDistanceV = -IPDistance;



			
		
		GameObject camll = GameObject.Find ("Rig/Left");
		GameObject camrl = GameObject.Find ("Rig/Right");
		GameObject camfl = GameObject.Find ("Rig/Front");
		GameObject camtl = GameObject.Find ("Rig/Top");
		GameObject cambl = GameObject.Find ("Rig/Back");
		GameObject camdl = GameObject.Find ("Rig/Down");

	
		
		
		
		GameObject cloneCamLL = Instantiate (cam);

			Destroy(cloneCamLL.GetComponent(typeof(Animator)));
			Destroy(cloneCamLL.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamLL.GetComponent(typeof(AudioListener)));

		GameObject cloneCamRL = Instantiate (cam);
			Destroy(cloneCamRL.GetComponent(typeof(Animator)));
			Destroy(cloneCamRL.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamRL.GetComponent(typeof(AudioListener)));

		GameObject cloneCamTL = Instantiate (cam);
			Destroy(cloneCamTL.GetComponent(typeof(Animator)));
			Destroy(cloneCamTL.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamTL.GetComponent(typeof(AudioListener)));

		GameObject cloneCamBL = Instantiate (cam);
			Destroy(cloneCamBL.GetComponent(typeof(Animator)));
			Destroy(cloneCamBL.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamBL.GetComponent(typeof(AudioListener)));

		GameObject cloneCamFL = Instantiate (cam);
			Destroy(cloneCamFL.GetComponent(typeof(Animator)));
			Destroy(cloneCamFL.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamFL.GetComponent(typeof(AudioListener)));

		GameObject cloneCamDL = Instantiate (cam);
			Destroy(cloneCamDL.GetComponent(typeof(Animator)));
			Destroy(cloneCamDL.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamDL.GetComponent(typeof(AudioListener)));
		
		GameObject cloneCamLR = Instantiate (cam);
			Destroy(cloneCamLR.GetComponent(typeof(Animator)));
			Destroy(cloneCamLR.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamLR.GetComponent(typeof(AudioListener)));

		GameObject cloneCamRR = Instantiate (cam);
			Destroy(cloneCamRR.GetComponent(typeof(Animator)));
			Destroy(cloneCamRR.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamRR.GetComponent(typeof(AudioListener)));

		GameObject cloneCamTR = Instantiate (cam);
			Destroy(cloneCamTR.GetComponent(typeof(Animator)));
			Destroy(cloneCamTR.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamTR.GetComponent(typeof(AudioListener)));

		GameObject cloneCamBR = Instantiate (cam);
			Destroy(cloneCamBR.GetComponent(typeof(Animator)));
			Destroy(cloneCamBR.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamBR.GetComponent(typeof(AudioListener)));

		GameObject cloneCamFR = Instantiate (cam);
			Destroy(cloneCamFR.GetComponent(typeof(Animator)));
			Destroy(cloneCamFR.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamFR.GetComponent(typeof(AudioListener)));

		GameObject cloneCamDR = Instantiate (cam);
			Destroy(cloneCamDR.GetComponent(typeof(Animator)));
			Destroy(cloneCamDR.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamDR.GetComponent(typeof(AudioListener)));
			
		GameObject cloneCamDRX = Instantiate (cam);
			Destroy(cloneCamDRX.GetComponent(typeof(Animator)));
			Destroy(cloneCamDRX.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamDRX.GetComponent(typeof(AudioListener)));

		GameObject cloneCamDLX = Instantiate (cam);
			Destroy(cloneCamDLX.GetComponent(typeof(Animator)));
			Destroy(cloneCamDLX.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamDLX.GetComponent(typeof(AudioListener)));

		GameObject cloneCamTRX = Instantiate (cam);
			Destroy(cloneCamTRX.GetComponent(typeof(Animator)));
			Destroy(cloneCamTRX.GetComponent(typeof(VRCapture)));
			Destroy(cloneCamTRX.GetComponent(typeof(AudioListener)));

		GameObject cloneCamTLX = Instantiate (cam);
			Destroy(cloneCamTLX.GetComponent(typeof(Animator)));
			Destroy(cloneCamTLX.GetComponent(typeof(VRCapture)));
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



		renderPanorama = (GameObject)Instantiate(Resources.Load("360Unwrapped"));


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


			if (panoramaType == VRModeList.EquidistantStereo)
			{
				screenShot = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
			}
			else {
				screenShot = new Texture2D(resolution, resolution/2, TextureFormat.RGB24, false);
			}

			float aAfactor = resolution ;


			flTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
			llTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
			rlTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
			dlTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
			blTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
			tlTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
			

			
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


			if (panoramaType == VRModeList.EquidistantStereo){

				dlxTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
				drxTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
				tlxTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
				trxTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
				
				
				frTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
				lrTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
				rrTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
				drTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
				brTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);
				trTex = RenderTexture.GetTemporary (qualityTemp, qualityTemp, depthBufferSize);


				
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
		void Update()
		{

			if (captureAudio){
								


				}





			else{

								if ((Time.frameCount - StartFrame) == NumberOfFramesToRender - 2 && mailme == true)
			{


			}
			}
		}

		void LateUpdate()
		{

			if (captureAudio){
			if (Time.timeSinceLevelLoad > NumberOfFramesToRender/FPS ){
				recOutput = false;
				WriteHeader(); 
				UnityEditor.EditorApplication.isPlaying = false;
				
				}
			}
			else{


			

			if (panoramaType == VRModeList.VideoCapture){
				RenderVideo();
			}

			else	{
		
				if (alignPanoramaWithHorizont){

					Vector3 angleCorection = gameObject.transform.rotation.eulerAngles;
					angleCorection = new Vector3 (0, angleCorection.y, 0);
					gameObject.transform.rotation = Quaternion.Euler(angleCorection);
				}


						UpdateCubemapL (renderHead);
					if (panoramaType == VRModeList.EquidistantStereo) UpdateCubemapR(GameObject.Find ("RcubemapRender"));

				RenderVRPanorama();
				CounterPost ();

			}

			}
		}



		public void RenderVRPanorama()
		{



			if (Time.frameCount - StartFrame < NumberOfFramesToRender && UnityEditor.EditorApplication.isPlaying)
					
				{

								if ((Time.frameCount - StartFrame)>3 && (Time.frameCount - StartFrame)>renderFromFrame){


					SaveScreenshot();
				}

		


		}
}





		public void RenderVideo()
		{
			
			float sequenceTime = (float)NumberOfFramesToRender / (float)FPS;
			int minutesSeq = (int)sequenceTime / 60;
			int secondsSeq = (int)sequenceTime % 60;

			
			sequenceLength = (minutesSeq + " min. " + secondsSeq + " sec. ");
			
			
			
			
						if ((Time.frameCount - StartFrame) < NumberOfFramesToRender && UnityEditor.EditorApplication.isPlaying  )
				
			{

				
				
				RenderInfo = "Rendering";
								if ((Time.frameCount - StartFrame)>3){
					SaveScreenshotVideo();
				}

								remainingTime = Time.realtimeSinceStartup / (Time.frameCount - StartFrame) * (NumberOfFramesToRender-(Time.frameCount - StartFrame));
				minutesRemain = (int) remainingTime/60; 
				secondsRemain = (int) remainingTime%60; 
				
								if (EditorUtility.DisplayCancelableProgressBar("Rendering", " Remaining time: " + minutesRemain + " min. " + secondsRemain + " sec.", (float)(Time.frameCount - StartFrame) / (float)NumberOfFramesToRender))
				{
					EditorUtility.ClearProgressBar();
					Debug.Log (Time.realtimeSinceStartup);
					UnityEditor.EditorApplication.isPlaying = false;	
					
				}
				

			}
			
			else {
				
				EditorUtility.ClearProgressBar();
				Debug.Log (Time.realtimeSinceStartup);

				
				
				if (openDestinationFolder){




					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
						FileName = Path.GetFullPath(string.Format(@"{0}/", Folder)),
						UseShellExecute = true,
						Verb = "open"
					});
				}
				
				if (encodeToMp4){
					string fullPath = Path.GetFullPath(string.Format(@"{0}/", Folder));
					string ffmpegPath = Path.GetFullPath(string.Format(@"{0}/", "Assets\\VRPanorama\\External\\"));
					if (System.IO.File.Exists(Folder +"/" + Folder + ".wav")){
					

						System.Diagnostics.Process.Start( ffmpegPath + "ffmpeg" , " -f image2" +  " -framerate " + FPS + " -i \"" + fullPath + _prefix + "%05d" + formatString + " -i \"" + fullPath + Folder  + ".wav" + "\"" + " -r " + FPS +  " -vcodec libx264 -y -pix_fmt yuv420p -b:v " + Mp4Bitrate + "k" + " -c:a aac -strict experimental -b:a 192k -shortest " + " \"" + fullPath + Folder + ".mp4\"");
			


					}
					else System.Diagnostics.Process.Start( ffmpegPath + "ffmpeg" , " -f image2" + " -framerate " + FPS + " -i \"" + fullPath + _prefix + "%05d" + formatString +  " -r " + FPS +  " -vcodec libx264 -y -pix_fmt yuv420p -b:v " + Mp4Bitrate + "k \"" +fullPath + Folder + ".mp4\"");
				}
				
				UnityEditor.EditorApplication.isPlaying = false;
				
				
				
			}
			
		}








		public void SaveScreenshot()
	{

			if (ImageFormatType == VRFormatList.JPG)
			{
								string filePath = string.Format("{0}/"+_prefix+"{1:D05}.jpg", Folder, (Time.frameCount - StartFrame - 3));
			formatString = ".jpg\"";
			Texture2D screenShot = GetScreenshot(true);
			
			byte[] bytes = screenShot.EncodeToJPG(100);
			

			Thread thread = new Thread(delegate() {

					File.WriteAllBytes(filePath, bytes);
				});
				thread.Priority = System.Threading.ThreadPriority.Highest;
				thread.IsBackground = false;
				thread.Start();

			}

			else 
			{
				Texture2D screenShot = GetScreenshot(true);

				byte[] bytes = screenShot.EncodeToPNG();


								string filePath = string.Format("{0}/"+_prefix+"{1:D05}.png", Folder, (Time.frameCount - StartFrame - 3));
				File.WriteAllBytes(filePath, bytes);
				formatString = ".png\"";
			}

	}
	

		
		
		public Texture2D GetScreenshot(bool eye)
		{


			rt = RenderTexture.GetTemporary (resolution, resolution/2, 0);

			if (eye){ 
				GameObject.Find ("QuadL").transform.localPosition = new Vector3 (0f, 0f, 1.5f);
				GameObject.Find ("QuadR").transform.localPosition = new Vector3 (0f, 0f, 6.5f);
				
			}

			Camera VRCam = panoramaCam.GetComponent<Camera> ();
			VRCam.targetTexture = rt;
			VRCam.Render();

			RenderTexture.active = rt;            





			if (panoramaType == VRModeList.EquidistantStereo)
			{
			
				screenShot.ReadPixels(new Rect(0, 0, resolution, resolution/2), 0, 0);
		

				GameObject.Find ("QuadL").transform.localPosition = new Vector3 (0f, 0f, 6.5f);
				GameObject.Find ("QuadR").transform.localPosition = new Vector3 (0f, 0f, 1.5f);

				VRCam.targetTexture = rt;
				VRCam.Render();
				
				RenderTexture.active = rt;

				screenShot.ReadPixels(new Rect(0, 0, resolution, resolution/2), 0, resolution/2);
				RenderTexture.ReleaseTemporary(rt);





			}
			else {
				screenShot.ReadPixels(new Rect(0, 0, resolution, resolution/2 ), 0, 0);
				RenderTexture.ReleaseTemporary(rt);
			}
			return screenShot;

	}

		public void SendEmail()
		{
			
			MailMessage mail = new MailMessage();
			
			mail.From = new MailAddress( _mailfrom);
			mail.To.Add(_mailto);
			mail.Subject = "VR Panorama Rendered";
			mail.Body = "Congratulations, VR panorama has finished rendering panorama named" + Folder ;
			
			SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
			smtpServer.Port = 587;
			smtpServer.Credentials = new System.Net.NetworkCredential(_mailfrom, _pass) as ICredentialsByHost;
			smtpServer.EnableSsl = true;
			ServicePointManager.ServerCertificateValidationCallback = 
				delegate(object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
			{ return true; };
			smtpServer.Send(mail);
			Debug.Log("Mail sent");

		}


		void OnDrawGizmos()
		{

			Handles.RadiusHandle (Quaternion.identity, 
			                      transform.position, 
			                      EnvironmentDistance);

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
			
			
						unfilteredRt = new RenderTexture(resolution*4 / 32 * renderQuality, resolutionH * 4 / 32 * renderQuality, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
			
			
			rt = new RenderTexture(resolution, resolutionH, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

						screenShot = new Texture2D(resolution, resolutionH, TextureFormat.ARGB32, true);
			
			
			

			
			
			
			if(Application.isPlaying ){
				Time.captureFramerate = FPS;
				System.IO.Directory.CreateDirectory(Folder);
			}
			else {
				EditorUtility.ClearProgressBar();

			}
			
		}




		public void SaveScreenshotVideo()
		{
			if (ImageFormatType == VRFormatList.JPG)
			{
				Texture2D screenShot = GetVideoScreenshot();
				byte[] bytes = screenShot.EncodeToJPG(100);
								string filePath = string.Format("{0}/"+_prefix+"{1:D05}.jpg", Folder, (Time.frameCount - StartFrame - 3));
				formatString = ".jpg\"";
				File.WriteAllBytes(filePath, bytes);
			}
			
			else 
			{
				Texture2D screenShot = GetVideoScreenshot();
				byte[] bytes = screenShot.EncodeToPNG();
								string filePath = string.Format("{0}/"+_prefix+"{1:D05}.png", Folder, (Time.frameCount - StartFrame -3 ));
				formatString = ".png\"";
				File.WriteAllBytes(filePath, bytes);
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
			
			float sequenceTime = (float)NumberOfFramesToRender / (float)FPS;
			int minutesSeq = (int)sequenceTime / 60;
			int secondsSeq = (int)sequenceTime % 60;

			
			sequenceLength = (minutesSeq + " min. " + secondsSeq + " sec. ");
			
			
			
			
						if ((Time.frameCount - StartFrame) < NumberOfFramesToRender && UnityEditor.EditorApplication.isPlaying)
				
			{



								remainingTime = Time.realtimeSinceStartup / (Time.frameCount - StartFrame) * (NumberOfFramesToRender-(Time.frameCount - StartFrame));
				minutesRemain = (int) remainingTime/60; 
				secondsRemain = (int) remainingTime%60; 
				
								if (EditorUtility.DisplayCancelableProgressBar("Rendering", " Remaining time: " + minutesRemain + " min. " + secondsRemain + " sec.", (float)(Time.frameCount - StartFrame) / (float)NumberOfFramesToRender))
				{
					EditorUtility.ClearProgressBar();
					Debug.Log ("Rendering Time: " + Time.realtimeSinceStartup + " Seconds");
					UnityEditor.EditorApplication.isPlaying = false;
					
					
					
				}
				
				
				
			}
			
			else {
				
				EditorUtility.ClearProgressBar();

				
				
				
				
				
				if (openDestinationFolder){
					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
						FileName = Path.GetFullPath(string.Format(@"{0}/", Folder)),
						UseShellExecute = true,
						Verb = "open"
					});
				}
				
				if (encodeToMp4){
					string fullPath = Path.GetFullPath(string.Format(@"{0}/", Folder));
					string ffmpegPath = Path.GetFullPath(string.Format(@"{0}/", "Assets\\VRPanorama\\External\\"));
					

					if (System.IO.File.Exists(Folder +"/" + Folder + ".wav")){
						
	
						System.Diagnostics.Process.Start( ffmpegPath + "ffmpeg" , " -f image2" +  " -framerate " + FPS + " -i \"" + fullPath + _prefix + "%05d" + formatString + " -i \"" + fullPath + Folder  + ".wav" + "\"" + " -r " + FPS +  " -vcodec libx264 -y -pix_fmt yuv420p -b:v " + Mp4Bitrate + "k" + " -c:a aac -strict experimental -b:a 192k -shortest " + " \"" + fullPath + Folder + ".mp4\"");
						Debug.Log ( " -f image2" + "-start_number" + renderFromFrame + " -framerate " + FPS + " -i \"" + fullPath + _prefix + "%05d" + formatString + " -i \"" + fullPath + Folder  + ".wav" + "\"" + " -r " + FPS +  " -vcodec libx264 -y -pix_fmt yuv420p -b:v " + Mp4Bitrate + "k" + " -c:a aac -strict experimental -b:a 192k -shortest " + " \"" + fullPath + Folder + ".mp4\"");
					}
					else System.Diagnostics.Process.Start( ffmpegPath + "ffmpeg" , " -f image2" + " -framerate " + FPS + " -i \"" + fullPath + _prefix + "%05d" + formatString +  " -r " + FPS +  " -vcodec libx264 -y -pix_fmt yuv420p -b:v " + Mp4Bitrate + "k \"" +fullPath + Folder + ".mp4\"");
				}
				
				if (mailme){
					SendEmail();
				}
				Debug.Log (Time.realtimeSinceStartup);
				UnityEditor.EditorApplication.isPlaying = false;
				
				
				
				
			}
			
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
			fileStream = new FileStream(name, FileMode.Create);
			byte emptyByte = new byte();
			
			for(int i = 0; i < headerSize; i++) //preparing the header
			{
				fileStream.WriteByte(emptyByte);
			}
		}
		
		void  OnAudioFilterRead ( float[] data , int channels){
			if(recOutput)
			{
				ConvertAndWrite(data); 
			}
		}
		
		void  ConvertAndWrite ( float[] dataSource  ){
			
			Int16[] intData = new Int16[dataSource.Length];
			
			
			Byte[] bytesData = new Byte[dataSource.Length * 2];
			
			
			int rescaleFactor = 32767;
			
			for (int i = 0; i<dataSource.Length;i++)
			{
				intData[i] = (short) (dataSource[i]*rescaleFactor);
				Byte[] byteArr = new Byte[2];
				byteArr = BitConverter.GetBytes(intData[i]);
				byteArr.CopyTo(bytesData,i*2);
			}
			
			fileStream.Write(bytesData,0,bytesData.Length);
		}
		
		void  WriteHeader (){
			
			fileStream.Seek(0,SeekOrigin.Begin);
			
			Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
			fileStream.Write(riff,0,4);
			
			Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length-8);
			fileStream.Write(chunkSize,0,4);
			
			Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
			fileStream.Write(wave,0,4);
			
			Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
			fileStream.Write(fmt,0,4);
			
			Byte[] subChunk1 = BitConverter.GetBytes(16);
			fileStream.Write(subChunk1,0,4);
			
			UInt16 two = 2;
			UInt16 one = 1;
			UInt16 six = achannels;
			
			Byte[] audioFormat = BitConverter.GetBytes(one);
			fileStream.Write(audioFormat,0,2);
			
			Byte[] numChannels = BitConverter.GetBytes(six);
			fileStream.Write(numChannels,0,2);
			
			Byte[] sampleRate = BitConverter.GetBytes(outputRate);
			fileStream.Write(sampleRate,0,4);
			
			Byte[] byteRate = BitConverter.GetBytes(outputRate * 2 * achannels );
			
			
			fileStream.Write(byteRate,0,4);
			
						UInt16 four = (UInt16) (achannels + achannels);
			Byte[] blockAlign = BitConverter.GetBytes(four);
			fileStream.Write(blockAlign,0,2);
			
			UInt16 sixteen = 16;
			Byte[] bitsPerSample = BitConverter.GetBytes(sixteen);
			fileStream.Write(bitsPerSample,0,2);
			
			Byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
			fileStream.Write(dataString,0,4);
			
			Byte[] subChunk2 = BitConverter.GetBytes(fileStream.Length-headerSize);
			fileStream.Write(subChunk2,0,4);
			
			fileStream.Close();
		}	






	}





}



#endif

#if UNITY_WEBPLAYER

using UnityEngine;
using System.Collections;
using VRPanorama;

namespace VRPanorama {
	public class VRCapture : MonoBehaviour {
		
		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void Update () {
			
		}
	}
}

#endif