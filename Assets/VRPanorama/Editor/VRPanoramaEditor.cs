
#if UNITY_EDITOR && !UNITY_WEBPLAYER

using UnityEngine;
using System.Collections;
using UnityEditor;
using VRPanorama;

using System.IO;


namespace VRPanorama {

[CustomEditor(typeof(VRCapture))]
	[RequireComponent (typeof (AudioListener))]

public class VRPanoramaEditor : Editor 
{
		private Texture banner = Resources.Load("VRHeader") as Texture;
		private bool changePrefix = false;

	// Use this for initialization
	public override void OnInspectorGUI () {

			VRCapture VRP = (VRCapture)target;

			if (VRP.ImageFormatType == VRPanorama.VRCapture.VRFormatList.JPG)
			{
				VRP.formatString = ".jpg\"";
			}
			
			else 
			{
				VRP.formatString = ".png\"";
				
			}


			GUILayout.Box (banner, GUILayout.ExpandWidth(true));
		


			GUILayout.BeginVertical ("box");

			GUILayout.Label ("VR Panorama");

			VRP.panoramaType = (VRPanorama.VRCapture.VRModeList)EditorGUILayout.EnumPopup("Capture Type",VRP.panoramaType);
			VRP.ImageFormatType = (VRPanorama.VRCapture.VRFormatList)EditorGUILayout.EnumPopup("Sequence Format",VRP.ImageFormatType);
			VRP.Folder = EditorGUILayout.TextField (new GUIContent("Save to Folder", "Store image sequence in this folder(has to be inside unity project folder)"), VRP.Folder);

			VRP.Folder = VRP.Folder.Replace(" ", "_");

			VRP.openDestinationFolder = EditorGUILayout.Toggle (new GUIContent("Open Destination Folder", ""), VRP.openDestinationFolder);


			VRP.resolution = EditorGUILayout.IntField (new GUIContent("Resolution", "Panorama width resolution"), VRP.resolution);



			if (VRP.panoramaType == VRPanorama.VRCapture.VRModeList.VideoCapture)
				VRP.resolutionH = EditorGUILayout.IntField (new GUIContent("Resolution", "Panorama width resolution"), VRP.resolutionH);

			GUILayout.BeginVertical ("box");
			GUILayout.Label ("Resolution Presets");
			if (VRP.panoramaType == VRPanorama.VRCapture.VRModeList.EquidistantStereo)
			{
			if (GUILayout.Button("Google Cardboard 1024 X 1024"))
				VRP.resolution = 1024;
			if (GUILayout.Button("GearVR Standard 2048 X 2048"))
				VRP.resolution = 2048;
			if (GUILayout.Button("Milk VR Standard 4096 X 4096"))
				VRP.resolution = 4096;
			}

			if (VRP.panoramaType == VRPanorama.VRCapture.VRModeList.EquidistantMono)
			{
				if (GUILayout.Button("Fast Preview 1024 X 512"))
					VRP.resolution = 1024;
				if (GUILayout.Button("Youtube 2K 2048 X 1024"))
					VRP.resolution = 2048;
				if (GUILayout.Button("Youtube 4K 4096 X 2048"))
					VRP.resolution = 4096;
			}

			if (VRP.panoramaType == VRPanorama.VRCapture.VRModeList.VideoCapture)
			{
				if (GUILayout.Button("HD 720p")){
					VRP.resolution = 1280;
					VRP.resolutionH = 720;
				}
				if (GUILayout.Button("HD 1080p")){
					VRP.resolution = 1920;
					VRP.resolutionH = 1080;
				}
				if (GUILayout.Button("UHD 4K")){
					VRP.resolution = 3840;
					VRP.resolutionH = 2160;
				}

			}
			
			
			GUILayout.EndVertical ();
			VRP.FPS = EditorGUILayout.IntField (new GUIContent("FPS", "Animation framerate"), VRP.FPS);

			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("FPS Presets");


				if (GUILayout.Button("24"))
					VRP.FPS = 24;
			
				if (GUILayout.Button("25"))
					VRP.FPS = 25;
				if (GUILayout.Button("30"))
					VRP.FPS = 30;
				if (GUILayout.Button("50"))
					VRP.FPS = 50;
			if (GUILayout.Button("60"))
				VRP.FPS = 60;
			if (GUILayout.Button("75"))
				VRP.FPS = 75;
			if (GUILayout.Button("90"))
				VRP.FPS = 90;
			if (GUILayout.Button("100"))
				VRP.FPS = 100;

			
		
			
			GUILayout.EndHorizontal ();

			VRP.NumberOfFramesToRender = EditorGUILayout.IntField (new GUIContent("Number of frames to render", "Number of frames to render"), VRP.NumberOfFramesToRender);
			VRP.renderFromFrame = EditorGUILayout.IntField (new GUIContent("Resume fromframe", "Resume rendering from frame. This has to be used as resume option after crash or user cancel"), VRP.renderFromFrame);

			float sequenceTime = ((float)VRP.NumberOfFramesToRender - (float)VRP.renderFromFrame) / (float)VRP.FPS;
			int minutesSeq = (int)sequenceTime / 60;
			int secondsSeq = (int)sequenceTime % 60;

			int frames = (VRP.NumberOfFramesToRender - VRP.renderFromFrame) - (minutesSeq * VRP.FPS * 60 + secondsSeq * VRP.FPS);
			
			string sequenceLength = (minutesSeq + " min. " + secondsSeq + " sec. " + frames + " frames");



			GUILayout.Label ("Sequence Length: " + sequenceLength);

			GUILayout.EndVertical ();
			if (VRP.panoramaType == VRPanorama.VRCapture.VRModeList.VideoCapture)
				VRP.alignPanoramaWithHorizont = false;
			if (VRP.panoramaType == VRPanorama.VRCapture.VRModeList.EquidistantStereo || VRP.panoramaType == VRPanorama.VRCapture.VRModeList.EquidistantMono)
			{

				GUILayout.BeginVertical ("box");
				GUILayout.BeginVertical ("box");
				GUILayout.Label ("Panorama Settings");
				GUILayout.EndVertical ();
				if (VRP.panoramaType == VRPanorama.VRCapture.VRModeList.EquidistantStereo) VRP.IPDistance = EditorGUILayout.FloatField (new GUIContent("IP Distance", "Interpupilar distance"), VRP.IPDistance);
				if (VRP.panoramaType == VRPanorama.VRCapture.VRModeList.EquidistantStereo) VRP.EnvironmentDistance = EditorGUILayout.FloatField (new GUIContent("Environment Distance", "Distance where stiching is perfect: adjust in base of your scene"), VRP.EnvironmentDistance);
 				VRP.alignPanoramaWithHorizont = EditorGUILayout.Toggle (new GUIContent("Align with Horizont", "Forces camera to be aligned with horizont by forcing only rotations on Y axis, usefull fhen using existing animations that have camera X or Z rotations"), VRP.alignPanoramaWithHorizont);
	
				GUILayout.EndVertical ();

			}



			if (VRP.panoramaType == VRPanorama.VRCapture.VRModeList.EquidistantMono)
			{
			GUILayout.BeginVertical ("box");
			GUILayout.Label ("Optimizations");
			VRP.depth = EditorGUILayout.Toggle (new GUIContent("Use Depth buffer", ""), VRP.depth);
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

			if (VRP.panoramaType == VRPanorama.VRCapture.VRModeList.EquidistantStereo)
			{
				GUILayout.BeginVertical ("box");
				GUILayout.Label ("Optimizations");
				VRP.depth = EditorGUILayout.Toggle (new GUIContent("Use Depth buffer", ""), VRP.depth);
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
				GUILayout.Label ("and " + size * size * 16 / 1000000 * (VRP.NumberOfFramesToRender - VRP.renderFromFrame) + " megapixels for a whole animation");
				
				
				GUILayout.EndVertical ();
			}

						if (VRP.panoramaType == VRPanorama.VRCapture.VRModeList.VideoCapture)
						{
								GUILayout.BeginVertical ("box");
								GUILayout.Label ("Optimizations");
								VRP.depth = EditorGUILayout.Toggle (new GUIContent("Use Depth buffer", ""), VRP.depth);
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
							

								GUILayout.EndVertical ();
						}



								GUILayout.BeginVertical ("box");
								GUILayout.Label ("Audio");

						VRP.volume = EditorGUILayout.Slider("Audio Volume", VRP.volume, 0, 1);
						VRP.mute = EditorGUILayout.Toggle (new GUIContent("Mute while Rendering", ""), VRP.mute);

					


								GUILayout.EndVertical ();



			GUILayout.BeginVertical ("box");

			VRP.encodeToMp4 = EditorGUILayout.Toggle (new GUIContent("Encode H.264 video after rendering", "Encode H.246 video after rendering finishes"), VRP.encodeToMp4);
			GUILayout.Label ("H.264 Movie Export Options");
			VRP.Mp4Bitrate = EditorGUILayout.IntField (new GUIContent("H.264 Bitrate", "MP4 Bitrate"), VRP.Mp4Bitrate);

			string fullPath = Path.GetFullPath(string.Format(@"{0}/", VRP.Folder));
			string ffmpegPath = Path.GetFullPath(string.Format(@"{0}/", "Assets\\VRPanorama\\External\\"));

			if (GUILayout.Button("Encode H.246 Video From Existing sequence ")) {

				if (System.IO.File.Exists(VRP.Folder +"/" + VRP.Folder + ".wav")){
				
				System.Diagnostics.Process.Start( ffmpegPath + "ffmpeg" , " -f image2" +  " -framerate " + VRP.FPS + " -i \"" + fullPath + VRP._prefix + "%05d" + VRP.formatString + " -i \"" + fullPath + VRP.Folder  + ".wav" + "\"" + " -r " + VRP.FPS +  " -vcodec libx264 -y -pix_fmt yuv420p -b:v " + VRP.Mp4Bitrate + "k" + " -c:a aac -strict experimental -b:a 192k -shortest " + " \"" + fullPath + VRP.Folder + ".mp4\"");
				}
				else System.Diagnostics.Process.Start( ffmpegPath + "ffmpeg" , " -f image2" +  " -framerate " + VRP.FPS + " -i \"" + fullPath + VRP._prefix + "%05d" + VRP.formatString + " -r " + VRP.FPS +  " -vcodec libx264 -y -pix_fmt yuv420p -b:v " + VRP.Mp4Bitrate + "k" + " \"" + fullPath + VRP.Folder + ".mp4\"");
			}



			if (GUILayout.Button("Encode best quality video for Gear VR from 4k sequenc ")) {
//				string fullPath = Path.GetFullPath(string.Format(@"{0}/", VRP.Folder));
//				string ffmpegPath = Path.GetFullPath(string.Format(@"{0}/", "Assets\\VRPanorama\\External\\"));
				if (System.IO.File.Exists(VRP.Folder +"/" + VRP.Folder + ".wav")){

				
					System.Diagnostics.Process.Start( ffmpegPath + "ffmpeg" , " -f image2" + " -framerate " + VRP.FPS + " -i \"" + fullPath + VRP._prefix + "%05d" + VRP.formatString +  " -i \"" + fullPath + VRP.Folder  + ".wav" + "\"" + " -vf scale=3480:1920 " + " -r " + VRP.FPS +  " -vcodec libx264 -y -pix_fmt yuv420p -b:v " + VRP.Mp4Bitrate + "k" + " -c:a aac -strict experimental -b:a 192k -shortest " + " \"" +fullPath + VRP.Folder + "_TB.mp4\"");
			//		Debug.Log                                                (" -f image2" + " -framerate " + VRP.FPS + " -i \"" + fullPath + VRP._prefix + "%05d" + VRP.formatString +  " -i \"" + fullPath + VRP.Folder  + ".wav" + "\"" + " -vf scale=3480:1920 " + " -r " + VRP.FPS +  " -vcodec libx264 -y -pix_fmt yuv420p -b:v " + VRP.Mp4Bitrate + "k" + " -c:a aac -strict experimental -b:a 192k -shortest " + " \"" +fullPath + VRP.Folder + "_TB.mp4\"");
//				System.Diagnostics.Process.Start( ffmpegPath + "ffmpeg" , " -f image2" +  " -framerate " + VRP.FPS + " -i \"" + fullPath + VRP._prefix + "%05d" + VRP.formatString + " -i \"" + fullPath + VRP.Folder  + ".wav" + "\"" + " -r " + VRP.FPS +  " -vcodec libx264 -y -pix_fmt yuv420p -b:v " + VRP.Mp4Bitrate + "k" + " -c:a aac -strict experimental -b:a 192k -shortest " + " \"" + fullPath + VRP.Folder + ".mp4\"");
				
			}

				else System.Diagnostics.Process.Start( ffmpegPath + "ffmpeg" , " -f image2" + " -framerate " + VRP.FPS + " -i \"" + fullPath + VRP._prefix + "%05d" + VRP.formatString + " -vf scale=3480:1920 " +  " -r " + VRP.FPS +  " -vcodec libx264 -y -pix_fmt yuv420p -b:v " + VRP.Mp4Bitrate + "k \"" +fullPath + VRP.Folder + "_TB.mp4\"");
			}



			GUILayout.EndVertical ();
			
			

			GUIStyle style = new GUIStyle(GUI.skin.button);
			style.normal.textColor = Color.red;


			GUILayout.BeginVertical ("box");

			VRP.mailme = EditorGUILayout.Toggle (new GUIContent("Notify by Email", "Notify by Email when rendering finishes"), VRP.mailme);
			if (VRP.mailme){
				VRP._mailto = EditorGUILayout.TextField (new GUIContent("Send confirmation Email TO:", "should be something like username@gmail.com"), VRP._mailto);
				GUILayout.BeginVertical ("box");
			
			GUILayout.Label ("GMAIL account settings");
			VRP._mailfrom = EditorGUILayout.TextField (new GUIContent("FROM Gmail Adress:", "should be something like username@gmail.com"), VRP._mailfrom);
			VRP._pass = EditorGUILayout.PasswordField (new GUIContent("Gmail Password:", "Don't worry, you are the only one to know it!"), VRP._pass);





			GUILayout.EndVertical ();
			}
			GUILayout.EndVertical ();

			GUILayout.BeginVertical ("box");

			changePrefix = EditorGUILayout.Toggle (new GUIContent("Change image name prefix", "Change image name prefix"), changePrefix);
			if (changePrefix == true){
				GUILayout.BeginVertical ("box");
				VRP._prefix = EditorGUILayout.TextField (new GUIContent("Image name prefix", "Image name prefix followed by 5 numbers"), VRP._prefix);
				GUILayout.EndVertical ();
			}
			GUILayout.EndVertical ();

			if (GUILayout.Button("Open destination folder")) {
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
					FileName = Path.GetFullPath(string.Format(@"{0}/", VRP.Folder)),
					UseShellExecute = true,
					Verb = "open"
				});
			}
			if (GUILayout.Button ("Capture Audio", style)){
				VRP.captureAudio = true;
				UnityEditor.EditorApplication.isPlaying = true;
			}

			if (GUILayout.Button("Render Panorama", style)){
				VRP.captureAudio = false;
				UnityEditor.EditorApplication.isPlaying = true;
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
	
	public class VRPanoramaEditor : Editor 
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
