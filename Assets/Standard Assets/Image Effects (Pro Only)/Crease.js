
public var intensity : float = 0.5;
public var softness : int = 1;
public var spread : float = 1.0;

@script ExecuteInEditMode

@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Crease")

class Crease extends PostEffectsBase {
	
	public var blurShader : Shader;
	private var _blurMaterial : Material = null;	
	
	public var depthFetchShader : Shader;
	private var _depthFetchMaterial : Material = null;
	
	public var creaseApplyShader : Shader;
	private var _creaseApplyMaterial : Material = null;	
	
	function CreateMaterials () {
		if (!_blurMaterial) {
			if(!CheckShader(blurShader)) {
				enabled = false;
				 return;
			}
			_blurMaterial = new Material(blurShader);	
			_blurMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
		
		if (!_depthFetchMaterial) {
			if(!CheckShader(depthFetchShader)) {
				enabled = false;
				 return;
			}
			_depthFetchMaterial = new Material(depthFetchShader);	
			_depthFetchMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
		
		if (!_creaseApplyMaterial) {
			if(!CheckShader(creaseApplyShader)) {
				enabled = false;
				return;
			}
			_creaseApplyMaterial = new Material(creaseApplyShader);	
			_creaseApplyMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
		
		if(!SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.Depth)) {
			enabled = false;
			return;	
		}
	}
	
	function Start () {
		CreateMaterials ();
	}
	
	function OnEnable() {
		GetComponent.<Camera>().depthTextureMode |= DepthTextureMode.Depth;	
	}

	function OnRenderImage (source : RenderTexture, destination : RenderTexture)
	{	
		CreateMaterials ();

		var hrTex : RenderTexture = RenderTexture.GetTemporary (source.width, source.height, 0); 
		var lrTex1 : RenderTexture = RenderTexture.GetTemporary (source.width / 2, source.height / 2, 0); 
		var lrTex2 : RenderTexture = RenderTexture.GetTemporary (source.width / 2, source.height / 2, 0); 
		
		Graphics.Blit(source,hrTex,_depthFetchMaterial);
		
		Graphics.Blit(hrTex,lrTex1);
		
		for(var i : int = 0; i < softness; i++) {
			_blurMaterial.SetVector ("offsets", Vector4 (0.0, (spread) / lrTex1.height, 0.0, 0.0));
			Graphics.Blit (lrTex1, lrTex2, _blurMaterial);
			_blurMaterial.SetVector ("offsets", Vector4 ((spread) / lrTex1.width,  0.0, 0.0, 0.0));		
			Graphics.Blit (lrTex2, lrTex1, _blurMaterial);
		}
		
		_creaseApplyMaterial.SetTexture("_HrDepthTex",hrTex);
		_creaseApplyMaterial.SetTexture("_LrDepthTex",lrTex1);
		_creaseApplyMaterial.SetFloat("intensity",intensity);
		Graphics.Blit(source,destination,_creaseApplyMaterial);	

		RenderTexture.ReleaseTemporary(hrTex);
		RenderTexture.ReleaseTemporary(lrTex1);
		RenderTexture.ReleaseTemporary(lrTex2);
	}	
}
