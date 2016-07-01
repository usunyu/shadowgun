
using UnityEngine;
using System.Collections;

[ExecuteInEditMode()]
[RequireComponent (typeof(Camera))]
[AddComponentMenu ("Image Effects/Vignette")]

// NEEDED SHADERS
//  NOTE:
//  usually hidden in the inspector by proper editor scripts
		
public class Vignetting : MonoBehaviour {
	
    // needed shaders & materials
	public Shader vignetteShader;
	private Material _vignetteMaterial;
	
	public Shader separableBlurShader;
	private Material _separableBlurMaterial;
	
	public Shader chromAberrationShader;
	private Material _chromAberrationMaterial;

    public float vignetteIntensity = 0.375f;
    public float chromaticAberrationIntensity = 0.0f;
    public float blurVignette = 0.0f;

	void Start () 
    {
		CreateMaterials ();	
	}
	
	bool CheckShader(Shader s )
    {
		if(!s.isSupported) {
			ReportNotSupported ();
			return false;
		}
		else
			return true;
	}
	
	void ReportNotSupported () 
    {
		Debug.LogError ("The image effect is not supported on this platform!");
		enabled = false;
	}
    
    void CreateMaterials () 
    {			

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
		
		if (!_chromAberrationMaterial) {
			if(!CheckShader(chromAberrationShader)) {
				enabled = false;
				return;
			}
			_chromAberrationMaterial = new Material (chromAberrationShader);
			_chromAberrationMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
	}
	

	void OnRenderImage (RenderTexture source,  RenderTexture destination)
	{	
		// needed for most of the new and improved image FX
		CreateMaterials ();	
		
		// get render targets	
		RenderTexture color = RenderTexture.GetTemporary(source.width, source.height, 0);	
		RenderTexture halfRezColor = RenderTexture.GetTemporary(source.width / 2, source.height / 2, 0);		
		RenderTexture quarterRezColor = RenderTexture.GetTemporary(source.width / 4, source.height / 4, 0);	
		RenderTexture secondQuarterRezColor = RenderTexture.GetTemporary(source.width / 4, source.height / 4, 0);	
		
		// do the downsample and blur
		Graphics.Blit (source, halfRezColor);
		Graphics.Blit (halfRezColor, quarterRezColor);	
				
		// blur the result to get a nicer bloom radius
		for (int it = 0; it < 2; it++ ) {
			_separableBlurMaterial.SetVector ("offsets", new Vector4 (0.0f, (1.5f * 1.0f) / quarterRezColor.height, 0.0f, 0.0f));	
			Graphics.Blit (quarterRezColor, secondQuarterRezColor, _separableBlurMaterial); 
			_separableBlurMaterial.SetVector ("offsets", new Vector4 ((1.5f * 1.0f) / quarterRezColor.width, 0.0f, 0.0f, 0.0f));	
			Graphics.Blit (secondQuarterRezColor, quarterRezColor, _separableBlurMaterial);		
		}		
		
		_vignetteMaterial.SetFloat ("vignetteIntensity", vignetteIntensity);
		_vignetteMaterial.SetFloat ("blurVignette", blurVignette);
		_vignetteMaterial.SetTexture ("_VignetteTex", quarterRezColor);
		Graphics.Blit(source, color,_vignetteMaterial); 				
		
		_chromAberrationMaterial.SetFloat ("chromaticAberrationIntensity", chromaticAberrationIntensity);
		Graphics.Blit (color, destination, _chromAberrationMaterial);	
		
		RenderTexture.ReleaseTemporary (color);
		RenderTexture.ReleaseTemporary (halfRezColor);			
		RenderTexture.ReleaseTemporary (quarterRezColor);	
		RenderTexture.ReleaseTemporary (secondQuarterRezColor);	
	
	}

}