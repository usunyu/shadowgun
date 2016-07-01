
public var highQuality : boolean = false;
public var sensitivityDepth : float = 1.0;
public var sensitivityNormals : float = 1.0;
public var spread : float = 1.0;

public var edgesIntensity : float = 1.0;
public var edgesOnly : float = 0.0;
public var edgesOnlyBgColor : Color = Color.white;

public var edgeBlur : boolean = false;
public var blurSpread : float = 0.75;
public var blurIterations : int = 1;

@script ExecuteInEditMode

@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Edge Detection (Depth-Normals)")

class EdgeDetectEffectNormals extends PostEffectsBase {
	
	public var edgeDetectHqShader : Shader;
	private var _edgeDetectHqMaterial : Material = null;	
	
	public var edgeDetectShader : Shader;
	private var _edgeDetectMaterial : Material = null;
	
	public var sepBlurShader : Shader;
	private var _sepBlurMaterial : Material = null;
	
	public var edgeApplyShader : Shader;
	private var _edgeApplyMaterial : Material = null;

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
		
		if (!_edgeDetectMaterial) {
			if(!CheckShader(edgeDetectShader)) {
				enabled = false;
				return;
			}
			_edgeDetectMaterial = new Material(edgeDetectShader);	
			_edgeDetectMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
		
		if (!_sepBlurMaterial) {
			if(!CheckShader(sepBlurShader)) {
				enabled = false;
				return;
			}
			_sepBlurMaterial = new Material (sepBlurShader);
			_sepBlurMaterial.hideFlags = HideFlags.HideAndDontSave;	
		}
		
		if (!_edgeApplyMaterial) {
			if(!CheckShader(edgeApplyShader)) {
				enabled = false;
				return;
			}
			_edgeApplyMaterial = new Material (edgeApplyShader);
			_edgeApplyMaterial.hideFlags = HideFlags.HideAndDontSave;	
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
	
		if (highQuality) {
			var lrTex1 : RenderTexture = RenderTexture.GetTemporary (source.width/2, source.height/2, 0); 
			var lrTex2 : RenderTexture = RenderTexture.GetTemporary (source.width/2, source.height/2, 0); 
			
			_edgeDetectHqMaterial.SetVector ("sensitivity", Vector4 (sensitivity.x, sensitivity.y, Mathf.Max(0.1,spread), sensitivity.y));		
			_edgeDetectHqMaterial.SetFloat("edgesOnly", edgesOnly);	
			var vecCol : Vector4 = edgesOnlyBgColor;
			_edgeDetectHqMaterial.SetVector("edgesOnlyBgColor", vecCol);		
			
			Graphics.Blit (source, source, _edgeDetectHqMaterial); // writes edges into .a
			if(edgeBlur) {
				Graphics.Blit (source, lrTex1);
				
				for (var i : int = 0; i < blurIterations; i++) {
					_sepBlurMaterial.SetVector ("offsets", Vector4 (0.0, (blurSpread) / lrTex1.height, 0.0, 0.0));
					Graphics.Blit (lrTex1, lrTex2, _sepBlurMaterial);
					_sepBlurMaterial.SetVector ("offsets", Vector4 ((blurSpread) / lrTex1.width,  0.0, 0.0, 0.0));		
					Graphics.Blit (lrTex2, lrTex1, _sepBlurMaterial);	
				}
				
				_edgeApplyMaterial.SetTexture ("_EdgeTex", lrTex1);
				_edgeApplyMaterial.SetFloat("edgesIntensity", edgesIntensity);
				Graphics.Blit (source, destination, _edgeApplyMaterial);
			} else {
				_edgeApplyMaterial.SetTexture ("_EdgeTex", source);
				_edgeApplyMaterial.SetFloat("edgesIntensity", edgesIntensity);
				Graphics.Blit (source, destination, _edgeApplyMaterial);				
			}
			
			RenderTexture.ReleaseTemporary(lrTex1);
			RenderTexture.ReleaseTemporary(lrTex2);
		}
		else {
			Graphics.Blit (source, destination, _edgeDetectMaterial);
		}
	}
}

