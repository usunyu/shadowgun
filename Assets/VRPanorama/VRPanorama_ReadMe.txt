!!!IMPORTANT UPDATE NOTICE: This is a major update featuring lots of changes! If you are updating from previous versions, please remove old VRPanorama folder and all VR Panorama Cameras!!

VRPanorama for Unity 5 by OliVR (version 0.7b)

VRPanorama is a fast and easy way to make fully functional stereoscopic panorama movies and standard screen capture movies for VR Headsets like Oculus or Gear VR. 

FEATURES
-Output Stereoscopic (or monoscopic) 360 panorama image sequence and movies directly from Unity to VR HUDs or video streaming services. 
-PNG or JPG sequence output.
-H.246 (MP4) VIDEO output * (available on PC platform, in development for OSX)
-Support for deferred and forward rendering with screenspace image effects.
-All video sequences use 2x supersampling for better antialiasing. This slows rendering a little bit, but it gives a superior quality CG without jagged edges. 
-Render standard HD video captures. 
-Capture audio support
-VR Panorama RT component for rendering 360 panoramas onscreen in realtime. 


USAGE

	-Right click in Hierarchy View and select VR Panorama Camera, or in menu Game Object/VR Panorama Camera. This action will add VR Panorama Camera into your scene. You can also add a VRCapture script to your existing camera. 

	-Set rendering settings for VRPanorama by modifying VR Capture Component (attached to  VR Panorama Camera object). Palce your favourite image effects on camera (like ambient occlusion, ssr, tonemapping, bloom). Note that some Screen Space image effects (like adaptive Tonemapping) could introduce artifacts, but majority of them will work just fine. 


Capture Type: 
	Can be Equidistant stereo, Equidistant Mono or Video Capture. Use stereo for VR HUDs, and Mono for 360 video streaming services like Youtube. Video Capture is a standard camera HI-quality capture with configurable antialiasing. 

Sequence Format: 
	Can be JPG or PNG. JPG is faster to render, but it introduces some compression artifacts. PNG is loosless but requires more space and is slower to render. 

Save to Folder: 
	Folder under Your project folder, where your image sequence will be stored. If folder doesn't exist, script will create it for you. 

Open Destination Folder: 
	When rendering finishes, script will open a destination folder for you. 

Resolution: 
	For most devices (Gear VR) it should be 2048 for stereoscopic VR panorama movie. For some services like Samsung Milk 360 it should be 4096). Here you can set a custom resolution, but it's better to shoose from a presets below.

Resolution presets: 
	Choose one of them. 1024 gives you a poor quality, but its good for making a first preview. 

FPS:
	Image sequence framerate. Can be any, set it in base of your final device. Note that if you don't have fast moving objects, it can be as low as 25 fps. You can choose from presets instead of typing. 


Number of frames to render: 
	Your final sequence lenght. A scene will stop playing after this number of frames. 
Start at frame: 
	Time in frame domain, from where a rendering will start. Usefull if you have to change only one portion of animation. 




VR CAPTURE SETTINGS:

IP Distance:
	Interpupillary distance. Default for average human is 0.064, but I suggest using 0.066 as for people with larger IP distance, 0.064 can be really uncomfortable. 

Environment Distance:
	Wery importan feature. Setting this value correctly makes a lot of difference. It compensates for stitching artifacts. This is a value of a nearest distance where your stitching has to be perfect. Don't set it too close, and don't set it too far. Best values are from 2 to 7. If your scene is large (like outdoors) go for larger values, if you are rendering a small corridor, make it smaller. 

Align Horizont: 
	Keeps animated camera leveled with horizont. You can use it you already made an animation that rotates cameras on other axes than Y. 

OPTIMIZATIONS:

Render Depth buffer:
	Enables Depth buffer writing (default is disabled, as this saves much VRAM), should be used only if you have Z issues with some shaders.
Speed VS. Quality:
	Supersample Antialiasing and subsample quality. Value of 8 is one to one pixel rendering, but without antialiasing (but you could use Image Antialiasing screen space effect). Smaller values are good for speed preview, they will export into a desired format, but with pixalisation. Value as 16 will give you a 2X supersampled antialliasing that is great for transparent texures. You can go as high as 32, that would be a 4X supersampled AA, but this function has to be used at own risk, especcialy for 4k renderings. (it requires lots of VRAM). 

Encode To MP4:
	Encode a video file for GearVR, Youtube or fast preview after rendering finish. 

MP4 bitrate:
	Encoding bitrate for videos.

Encode H.246 Video from Existing Sequence:
	Encodes to video an existing image sequence. Usefull if you want to re-encode at a different bitrate. 

Encode best quality video for Gear VR from 4k sequence:
	Encodes to video an existing image sequence. It can be used for encoding a best quality H.246 for Gear VR devices. Note that sequence has to be rendered on max 30fps. 

Notify by mail: 
	You have to render a long sequence, and don't want to stay in front of your computer. Check this box and fill your email settings, and you will receive an Email when rendering finishes. You have to use credentials of an existing Gmail account. 

Open Destination Folder (button): 
	Opens a storage file folder. 

Change Image Name: 
	Here you can change a prefix of a numbered image sequence. 

Capture Audio (button):
	First step to do when capturing videos with audio. It will play a scene and capture audio sequence. Be aware that som audio will not be captured as intended, as it will start later cuttong a start of an audio (unity takes some time to initialize scene, but audio will start before). This happenes if an audio track is meant to be played at awake. For wsyncing audio tracks like backing music, please, use a script "AudioSyncWithVRCapture.cs". Audio capture feature is a new feature, so it isn't tested on all systems. Feel free to send me a mial if you notice any bugs.
Audio capture has to be done before Render Panorama capture. It will check if there is an existing audio file, and will add it to a video stream. 

Render Panorama (button):
	Render your panorama with a click. But, you can also render it by playing a scene. 



NEW Feature of VR Panorama adds a VR Capture RT component. This component lets you render onscreen VR Panoramas in Realtime in HD resolution. This is usefull if you have a HD capture/streaming card, and you want to Live Stream your gameplay via internet. It doesn't feature any streaming method, but gives you only a possibility to render Fullscreen VR Panoramas that can be later captured with a hardware device. Note that you would want to use advanced hardware and latest generation GFX cards (something like GTX980ti or Titan). Also, you would use this component fullscreen directly from a compiled player. Be avare that it uses FULL HD standard, so set your player resolution and aspect ratio accordingly (1920x1080, 16:9 aspect ratio). 

Another new feature is a possibility to use layered cameras, by adding a component VRCompositor.cs to your existing VR Camera. 


GENERAL INFORMATION ON STEREOSCOPIC 360 PANORAMA MOVIES

Be aware that VR 360 panorama suffer some stitching artifacts. 
Unfortunately, these artifacts can't be avoided for stereocopic VR panoramas (in general, it applyes to all tecniques). It will happen when objects are near camera (they are bigger as object gets closer to camera, but they will get smaller or eventually dissapear for objects that are far away). Due to a paralax difference between eyes, you can't correct them in any way without breaking  stereoscopic illusion. There are different techiques for minimizing (one of them is usage of vertical scanline rendering technique, but it works only with static stereoscopic panoramas, as for animation, it would introduce heavy wobbling and spatial distortion (closer objects will be vertically stretched resulting in a wrong dimensional perception). 

Here are some tips on how to minimise stitching artifacts: 

-use correctly a Environment Distance value.
-place camera in a way that those stitching edges look at objects that are far from camera. 
-Project your animations in a way that they capture viewers attention at zones that are artifact free. 
-Avoid using thin vertical objects.
-On near planes like floor use textures that don't have large checker or stripe patterns. High frequency textures or uniform colors work better. 

VRPanorama asset is made to use psico-optical perception illusion: these artifacts are more visible in unwrapped image than in VR.

!!!!!!!TROUBLESHOOTING NOTICE!!!!!!: 
	-DO NOT change a default installation directory (Assets/VRPanorama) as this will break correct functioning of a script! 
	-BE SURE THAT YOU AREN'T USING A WEB PALTFORM (under build settings). WEB PLATFORM doesn't support writing to disk, and using it will break the script. 
	-Unity has a shadowing system that is limited on available VRAM. If you notice that in your scene a realtime shadows deissapear, lower your "Speed VS. Quality" settings or a final resolution. This cuold happen if there are many ScreenSpace effects that use RT's, rendering to 4K stereo panorama output, especcialy on GFX cards that have 2 gb or less VRAM.  A future version of VR Panorama will be more optimised on VRAM usage. 
	-When rendering VR Panorama, please turn off your HMD device if Unity 5 Virtual Reality Supported is active. VR Device tracking could mess with Virtual cameras that VR Panorama uses. 
	-Don't forget to feed your animals ;) 
	 
	

Support mail: olix@iol.it

Thanks for purchasing this asset!


Special thanks to Judiva for suggestions and brainstorming, and all nice people that helped me with suggestions and bug finding (I hope that you know who you are) :)