
public var sensitivityDepth : float = 1.0;
public var sensitivityNormals : float = 1.0;
public var edgeDetectSpread : float = 0.9;
public var filterRadius : float = 0.8;

public var showEdges : boolean = false;
public var iterations : int = 1;

@script ExecuteInEditMode

@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Edge Blur")

class EdgeBlurEffectNormals extends PostEffectsBase {
	
	public var edgeDetectHqShader : Shader;
	private var _edgeDetectHqMaterial : Material = null;	
	
	public var edgeBlurApplyShader : Shader;
	private var _edgeBlurApplyMaterial : Material = null;
	
	public var showAlphaChannelShader : Shader;
	private var _showAlphaChannelMaterial : Material = null;

	function CreateMaterials () 
	{
		if (!_edgeDetectHqMaterial) {
			if(!CheckShader(edgeDetectHqShader)) {
				enabled = false;
				return;
			}
			_edgeDetectHqMaterial = new Material(edgeDetectHqShader);	
			_edgeDetectHqMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
		
		if (!_edgeBlurApplyMaterial) {
			if(!CheckShader(edgeBlurApplyShader)) {
				enabled = false;
				return;
			}
			_edgeBlurApplyMaterial = new Material (edgeBlurApplyShader);
			_edgeBlurApplyMaterial.hideFlags = HideFlags.HideAndDontSave;	
		}
		
		if (!_showAlphaChannelMaterial) {
			if(!CheckShader(showAlphaChannelShader)) {
				enabled = false;
				return;
			}
			_showAlphaChannelMaterial = new Material(showAlphaChannelShader);
			_showAlphaChannelMaterial.hideFlags = HideFlags.HideAndDontSave;	
		}
		
		if(!SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.Depth)) {
			enabled = false;
			return;	
		}
	}
	
	function Start () {
		CreateMaterials ();
	}
	
	function OnEnable () {
		GetComponent.<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals;	
	}

	function OnRenderImage (source : RenderTexture, destination : RenderTexture)
	{	
		CreateMaterials ();
		
		var sensitivity : Vector2;
		sensitivity.x = sensitivityDepth;
		sensitivity.y = sensitivityNormals;
			
		_edgeDetectHqMaterial.SetVector ("sensitivity", Vector4 (sensitivity.x, sensitivity.y, Mathf.Max(0.1,edgeDetectSpread), sensitivity.y));		
		_edgeDetectHqMaterial.SetFloat("edgesOnly", 0.0);	
		_edgeDetectHqMaterial.SetVector("edgesOnlyBgColor", Vector4.zero);		
		Graphics.Blit (source, source, _edgeDetectHqMaterial);
		
		if (showEdges) {
			Graphics.Blit (source, destination, _showAlphaChannelMaterial);							
		} 
		else 
		{		
			_edgeBlurApplyMaterial.SetTexture ("_EdgeTex", source);
			_edgeBlurApplyMaterial.SetFloat("filterRadius", filterRadius);
			Graphics.Blit (source, destination, _edgeBlurApplyMaterial);		
			
			var its : int = iterations-1;
			if(its<0) its = 0;
			if(its>5) its = 5;
			while(its>0) {
				Graphics.Blit (destination, source, _edgeBlurApplyMaterial);		
				_edgeBlurApplyMaterial.SetTexture ("_EdgeTex", source);
				_edgeBlurApplyMaterial.SetFloat("filterRadius", filterRadius);
				Graphics.Blit (source, destination, _edgeBlurApplyMaterial);			
				its--;
			}
		}
	}
}

