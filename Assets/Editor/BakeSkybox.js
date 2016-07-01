class BakeSkybox
{
	static var screenSize = 1024;
	static var directory = "Assets/Skyboxes";
	static var skyboxShader = "Mobile/Skybox";
	
	
	static var skyBoxImage = new Array ("front", "right", "back", "left", "up", "down");
	static var skyBoxProps = new Array ("_FrontTex", "_RightTex", "_BackTex", "_LeftTex", "_UpTex", "_DownTex");
	
	static var skyDirection = new Array (Vector3 (0,0,0), Vector3 (0,-90,0), Vector3 (0,180,0), Vector3 (0,90,0), Vector3 (-90,0,0), Vector3 (90,0,0));
	
	@MenuItem("MADFINGER/Bake Skybox", false, 4)
	static function CaptureSkybox()
	{
		if (!System.IO.Directory.Exists(directory))
			System.IO.Directory.CreateDirectory(directory);
		
		for (var t in Selection.transforms)
			captureSkyBox(t);
	}
	
	static function captureSkyBox(t : Transform)
	{
		var go = new GameObject ("SkyboxCamera", Camera);
		
		go.GetComponent.<Camera>().backgroundColor = Color.black;
		go.GetComponent.<Camera>().clearFlags = CameraClearFlags.Skybox;
		go.GetComponent.<Camera>().fieldOfView = 90;    
		go.GetComponent.<Camera>().aspect = 1.0;
		
		go.transform.position = t.position;
		go.transform.rotation = Quaternion.identity;
		
		// render skybox        
		for (var orientation = 0; orientation < skyDirection.length ; orientation++)
		{
			var assetPath = System.IO.Path.Combine(directory, t.name + "_" + skyBoxImage[orientation] + ".png");
			captureSkyBoxFace(orientation, go.GetComponent.<Camera>(), assetPath);
		}
		GameObject.DestroyImmediate (go);
		
		// wire skybox material
		AssetDatabase.Refresh();
		
		var skyboxMaterial = new Material (Shader.Find(skyboxShader));        
		for (orientation = 0; orientation < skyDirection.length ; orientation++)
		{
			var texPath = System.IO.Path.Combine(directory, t.name + "_" + skyBoxImage[orientation] + ".png");
			var tex : Texture2D = AssetDatabase.LoadAssetAtPath(texPath, Texture2D) as Texture2D;
			tex.wrapMode = TextureWrapMode.Clamp;
			skyboxMaterial.SetTexture(skyBoxProps[orientation], tex);
		}
		
		// save material
		var matPath = System.IO.Path.Combine(directory, t.name + "_skybox" + ".mat");
		AssetDatabase.CreateAsset(skyboxMaterial, matPath);
	}

	static function captureSkyBoxFace(orientation : int, cam : Camera, assetPath : String)
	{
		cam.transform.eulerAngles = skyDirection[orientation];
		var rt = new RenderTexture (screenSize, screenSize, 24);
		cam.GetComponent.<Camera>().targetTexture = rt;
		cam.GetComponent.<Camera>().Render();
		RenderTexture.active = rt;
		
		var screenShot = new Texture2D (screenSize, screenSize, TextureFormat.RGB24, false);
		screenShot.ReadPixels (Rect (0, 0, screenSize, screenSize), 0, 0); 
		
		RenderTexture.active = null;
		GameObject.DestroyImmediate (rt);
		
		var bytes = screenShot.EncodeToPNG(); 
		System.IO.File.WriteAllBytes (assetPath, bytes);
		
		AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
	}
}