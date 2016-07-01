
// general settings

public var bloomThisTag : String;

public var sepBlurSpread : float = 1.5;
public var useSrcAlphaAsMask : float = 0.5;

// bloom settings

public var bloomIntensity : float = 1.0;
public var bloomThreshhold : float = 0.4;
public var bloomBlurIterations : int = 3;

// lens flare settings

enum LensflareStyle {
	Ghosting = 0,
	Hollywood = 1,
	Combined = 2,
}

enum TweakMode {
	Simple = 0,
	Advanced = 1,
}

public var tweakMode : TweakMode = 1;

public var lensflares : boolean = true;
public var hollywoodFlareBlurIterations : int = 4;
public var lensflareMode : LensflareStyle = 0;
public var hollyStretchWidth : float = 2.5;
public var lensflareIntensity : float = 0.75;
public var lensflareThreshhold : float = 0.5;
public var flareColorA : Color = Color(0.4,0.4,0.8,0.75);
public var flareColorB : Color = Color(0.4,0.8,0.8,0.75);
public var flareColorC : Color = Color(0.8,0.4,0.8,0.75);
public var flareColorD : Color = Color(0.8,0.4,0.0,0.75);
public var blurWidth : float = 1.0;

@script ExecuteInEditMode

@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Bloom and Flares")
				
class BloomAndFlares extends PostEffectsBase 
{
	
	// needed shaders & materials ...
	
	public var addAlphaHackShader : Shader;
	
	private var _alphaAddMaterial : Material;
	
	public var lensFlareShader : Shader; 
	private var _lensFlareMaterial : Material;
	
	public var vignetteShader : Shader;
	private var _vignetteMaterial : Material;
	
	public var separableBlurShader : Shader;
	private var _separableBlurMaterial : Material;
	
	public var addBrightStuffOneOneShader: Shader;
	private var _addBrightStuffBlendOneOneMaterial : Material;
	
	public var hollywoodFlareBlurShader: Shader;
	private var _hollywoodFlareBlurMaterial : Material;
	
	public var hollywoodFlareStretchShader: Shader;	
	private var _hollywoodFlareStretchMaterial : Material;	
	
	public var brightPassFilterShader : Shader;
	private var _brightPassFilterMaterial : Material;
	
	
	function Start () {
		CreateMaterials ();	
	}
	
	function CreateMaterials () {
		
		if(!_lensFlareMaterial) {
			if(!CheckShader(lensFlareShader)) {
				enabled = false;
				return;
			}
			_lensFlareMaterial = new Material (lensFlareShader);
			_lensFlareMaterial.hideFlags = HideFlags.HideAndDontSave; 
		}				
		
		if (!_vignetteMaterial) {
			if(!CheckShader(vignetteShader)) {
				enabled = false;
				return;
			}
			_vignetteMaterial = new Material (vignetteShader);
			_vignetteMaterial.hideFlags = HideFlags.HideAndDontSave; 
		}
				
		if (!_separableBlurMaterial) {
			if(!CheckShader(separableBlurShader)) {
				enabled = false;
				return;
			}
			_separableBlurMaterial = new Material (separableBlurShader);
			_separableBlurMaterial.hideFlags = HideFlags.HideAndDontSave; 
		}

		if (!_addBrightStuffBlendOneOneMaterial) {
			if(!CheckShader(addBrightStuffOneOneShader)) {
				enabled = false;
				return;
			}
			_addBrightStuffBlendOneOneMaterial = new Material (addBrightStuffOneOneShader);	
			_addBrightStuffBlendOneOneMaterial.hideFlags = HideFlags.HideAndDontSave; 
		} 
		
		if (!_hollywoodFlareBlurMaterial) {
			if(!CheckShader(hollywoodFlareBlurShader)) {
				enabled = false;
				return;
			}
			_hollywoodFlareBlurMaterial = new Material (hollywoodFlareBlurShader);
			_hollywoodFlareBlurMaterial.hideFlags = HideFlags.HideAndDontSave; 
		} 
		
		if (!_hollywoodFlareStretchMaterial) {
			if(!CheckShader(hollywoodFlareStretchShader)) {
				enabled = false;
				return;
			}
			_hollywoodFlareStretchMaterial = new Material (hollywoodFlareStretchShader);
			_hollywoodFlareStretchMaterial.hideFlags = HideFlags.HideAndDontSave; 
		} 		
		
		if (!_brightPassFilterMaterial) {
			if(!CheckShader(brightPassFilterShader)) {
				enabled = false;
				return;
			}
			_brightPassFilterMaterial = new Material (brightPassFilterShader);
			_brightPassFilterMaterial.hideFlags = HideFlags.HideAndDontSave;
		}					
		
		if(!_alphaAddMaterial) {
			if(!CheckShader(addAlphaHackShader)) {
				enabled = false;
				return;				
			}
			_alphaAddMaterial = new Material(addAlphaHackShader);		
			_alphaAddMaterial.hideFlags = HideFlags.HideAndDontSave;		
		}				
	}

	function OnPreCull () 
	{
		if(bloomThisTag && bloomThisTag != "Untagged") {
			
			var gos : GameObject[] = GameObject.FindGameObjectsWithTag(bloomThisTag);
			
			for (var go : GameObject in gos) {
				if(go.GetComponent(MeshFilter)) {
					var mesh : Mesh = (go.GetComponent(MeshFilter) as MeshFilter).sharedMesh;
					Graphics.DrawMesh(mesh,go.transform.localToWorldMatrix,_alphaAddMaterial,0,GetComponent.<Camera>());
				}
			}		
		}		
	}

	
	function OnRenderImage (source : RenderTexture, destination : RenderTexture)
	{			
		CreateMaterials ();	
		
		// needed render targets
		var halfRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 2.0, source.height / 2.0, 0);			
		var quarterRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 4.0, source.height / 4.0, 0);	
		var secondQuarterRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 4.0, source.height / 4.0, 0);	
		var thirdQuarterRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 4.0, source.height / 4.0, 0);	
		
		// at this point, we have massaged the alpha channel enough to start downsampling process for bloom	
		Graphics.Blit (source, halfRezColor);
		Graphics.Blit (halfRezColor, quarterRezColor);		

		// cut colors (threshholding)			
		_brightPassFilterMaterial.SetVector ("threshhold", Vector4 (bloomThreshhold, 1.0/(1.0-bloomThreshhold), 0.0, 0.0));
		_brightPassFilterMaterial.SetFloat ("useSrcAlphaAsMask", useSrcAlphaAsMask);
		Graphics.Blit (quarterRezColor, secondQuarterRezColor, _brightPassFilterMaterial);		
				
		// blurring
		if (bloomBlurIterations < 1)
			bloomBlurIterations = 1;	
				
		// blur the result to get a nicer bloom radius
        Graphics.Blit(secondQuarterRezColor, quarterRezColor);
		for (var iter : int = 0; iter < bloomBlurIterations; iter++ ) {
			_separableBlurMaterial.SetVector ("offsets", Vector4 (0.0, (sepBlurSpread * 1.0) / quarterRezColor.height, 0.0, 0.0));	
			Graphics.Blit (quarterRezColor, thirdQuarterRezColor, _separableBlurMaterial); 
			_separableBlurMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
			Graphics.Blit (thirdQuarterRezColor, quarterRezColor, _separableBlurMaterial);		
		}

		Graphics.Blit (source, destination);

		// operate on lens flares now
		if (lensflares) {
			
			if(lensflareMode == 0) // ghosting
			{
				// lens flare fun: cut some additional values and normalize
				_brightPassFilterMaterial.SetVector ("threshhold", Vector4 (lensflareThreshhold, 1.0 / (1.0-lensflareThreshhold), 0.0, 0.0));
				_brightPassFilterMaterial.SetFloat ("useSrcAlphaAsMask", 0.0);
				Graphics.Blit (quarterRezColor, thirdQuarterRezColor, _brightPassFilterMaterial); 	
		
				_separableBlurMaterial.SetVector ("offsets", Vector4 (0.0, (sepBlurSpread * 1.0) / quarterRezColor.height, 0.0, 0.0));	
				Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _separableBlurMaterial);				
				_separableBlurMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
				Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _separableBlurMaterial); 
				
				// vignette for lens flares
				_vignetteMaterial.SetFloat ("vignetteIntensity", 1.0);
				Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _vignetteMaterial); 
				
				// creating the flares
				// _lensFlareMaterial has One One Blend
				_lensFlareMaterial.SetVector ("color0", Vector4(0.0,0.0,0.0,0.0) * lensflareIntensity);
				_lensFlareMaterial.SetVector ("colorA", Vector4(flareColorA.r,flareColorA.g,flareColorA.b,flareColorA.a) * lensflareIntensity);
				_lensFlareMaterial.SetVector ("colorB", Vector4(flareColorB.r,flareColorB.g,flareColorB.b,flareColorB.a) * lensflareIntensity);
				_lensFlareMaterial.SetVector ("colorC", Vector4(flareColorC.r,flareColorC.g,flareColorC.b,flareColorC.a) * lensflareIntensity);
				_lensFlareMaterial.SetVector ("colorD", Vector4(flareColorD.r,flareColorD.g,flareColorD.b,flareColorD.a) * lensflareIntensity);
				Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _lensFlareMaterial);	
				
				_addBrightStuffBlendOneOneMaterial.SetFloat ("intensity", 1.0);
				Graphics.Blit (thirdQuarterRezColor, quarterRezColor, _addBrightStuffBlendOneOneMaterial); 					
			
			}				
			else if(lensflareMode == 1) // hollywood flares
			{
				// lens flare fun: cut some additional values 
				_brightPassFilterMaterial.SetVector ("threshhold", Vector4 (lensflareThreshhold, 1.0 / (1.0-lensflareThreshhold), 0.0, 0.0));
				_brightPassFilterMaterial.SetFloat ("useSrcAlphaAsMask", 0.0);
				Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _brightPassFilterMaterial); 					
				
				// ole: NEW and AWESOME new feature for hollyflares
				// narrow down the size that creates on of these lines
				_hollywoodFlareBlurMaterial.SetVector ("offsets", Vector4(0.0, (sepBlurSpread * 1.0) / quarterRezColor.height, 0.0, 0.0));	
				_hollywoodFlareBlurMaterial.SetTexture("_NonBlurredTex", quarterRezColor);
				_hollywoodFlareBlurMaterial.SetVector ("tintColor", Vector4(flareColorA.r,flareColorA.g,flareColorA.b,flareColorA.a) * flareColorA.a * lensflareIntensity);
				Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _hollywoodFlareBlurMaterial); 						
						
				_hollywoodFlareStretchMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
				_hollywoodFlareStretchMaterial.SetFloat("stretchWidth", hollyStretchWidth);
				Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _hollywoodFlareStretchMaterial);										
				
				for (var itera : int = 0; itera < hollywoodFlareBlurIterations; itera++ ) {

					_separableBlurMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
					Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _separableBlurMaterial); 
					
					_separableBlurMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
					Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _separableBlurMaterial); 						
						
				}		
								
				_addBrightStuffBlendOneOneMaterial.SetFloat ("intensity", 1.0);
				Graphics.Blit (thirdQuarterRezColor, quarterRezColor, _addBrightStuffBlendOneOneMaterial); 
			}  
			else // 'both' flares :)
			{
				// lens flare fun: cut some additional values 
				_brightPassFilterMaterial.SetVector ("threshhold", Vector4 (lensflareThreshhold, 1.0 / (1.0-lensflareThreshhold), 0.0, 0.0));
				_brightPassFilterMaterial.SetFloat ("useSrcAlphaAsMask", 0.0);
				Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _brightPassFilterMaterial); 	
				
				// ole: NEW and AWESOME new feature for hollyflares
				// narrow down the size that creates on of these lines
				_hollywoodFlareBlurMaterial.SetVector ("offsets", Vector4(0.0, (sepBlurSpread * 1.0) / quarterRezColor.height, 0.0, 0.0));	
				_hollywoodFlareBlurMaterial.SetTexture("_NonBlurredTex", quarterRezColor);
				_hollywoodFlareBlurMaterial.SetVector ("tintColor", Vector4(flareColorA.r,flareColorA.g,flareColorA.b,flareColorA.a) * flareColorA.a * lensflareIntensity);
				Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _hollywoodFlareBlurMaterial); 	
				_hollywoodFlareStretchMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
				_hollywoodFlareStretchMaterial.SetFloat("stretchWidth", hollyStretchWidth);
				Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _hollywoodFlareStretchMaterial);										
				
				for (var ix : int = 0; ix < hollywoodFlareBlurIterations; ix++ ) {

					_separableBlurMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
					Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _separableBlurMaterial); 
					
					_separableBlurMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
					Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _separableBlurMaterial); 							
				}		
				
				// vignette for lens flares
				_vignetteMaterial.SetFloat ("vignetteIntensity", 1.0);
				Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _vignetteMaterial); 
				
				// creating the flares
				// _lensFlareMaterial has One One Blend
				_lensFlareMaterial.SetVector ("color0", Vector4(0.0,0.0,0.0,0.0) * lensflareIntensity);
				_lensFlareMaterial.SetVector ("colorA", Vector4(flareColorA.r,flareColorA.g,flareColorA.b,flareColorA.a) * flareColorA.a * lensflareIntensity);
				_lensFlareMaterial.SetVector ("colorB", Vector4(flareColorB.r,flareColorB.g,flareColorB.b,flareColorB.a) * flareColorB.a * lensflareIntensity);
				_lensFlareMaterial.SetVector ("colorC", Vector4(flareColorC.r,flareColorC.g,flareColorC.b,flareColorC.a) * flareColorC.a * lensflareIntensity);
				_lensFlareMaterial.SetVector ("colorD", Vector4(flareColorD.r,flareColorD.g,flareColorD.b,flareColorD.a) * flareColorD.a * lensflareIntensity);
				Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _lensFlareMaterial);		
				
				_addBrightStuffBlendOneOneMaterial.SetFloat ("intensity", 1.0);
				Graphics.Blit (thirdQuarterRezColor, quarterRezColor, _addBrightStuffBlendOneOneMaterial); 																						
			}
		}		
		
		_addBrightStuffBlendOneOneMaterial.SetFloat("intensity", bloomIntensity);
		Graphics.Blit (quarterRezColor, destination, _addBrightStuffBlendOneOneMaterial);		
		
		RenderTexture.ReleaseTemporary (halfRezColor);	
		RenderTexture.ReleaseTemporary (quarterRezColor);	
		RenderTexture.ReleaseTemporary (secondQuarterRezColor);	
		RenderTexture.ReleaseTemporary (thirdQuarterRezColor);		
	}

}