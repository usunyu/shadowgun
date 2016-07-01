/*
 
 MFGuiRenderer
 
 Pro vsechny widgety jednoho layeru (hloubky v obrazovce) a materialu se vytvori tento renderer.
 Renderer umoznuje registrovat "render sprajty", ktere se budou renderovat.
 Uvnitr je pouzity puvodni "SpriteManager" ktery pro sprajty vytvori mesh a renderuje ho jednim drawcallem.
*/

//#define DEBUG_GUI_RENDERER

//#define ENABLE_UPDATED_MESHES_HIGHLIGHTING

//-----------------------------------------------------------------
//  SpriteManager v0.63 (6-1-2009)
//  Copyright 2009 Brady Wright and Above and Beyond Software
//  All rights reserved
//-----------------------------------------------------------------
// A class to allow the drawing of multiple "quads" as part of a
// single aggregated mesh so as to achieve multiple, independently
// moving objects using a single draw call.
//-----------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

//-----------------------------------------------------------------
// Holds a single mesh object which is composed of an arbitrary
// number of quads that all use the same material, allowing
// multiple, independently moving objects to be drawn on-screen
// while using only a single draw call.
//-----------------------------------------------------------------

[AddComponentMenu("")]
public class MFGuiRenderer : MonoBehaviour
{
	public enum ZeroLocationEnum
	{
		LowerLeft = -1,
		UpperLeft = 1
	};

	public LayerMask UILayer = 0;
	public ZeroLocationEnum ZeroLocation = ZeroLocationEnum.LowerLeft;

	float _xOffset;
	float _yOffset;

	// In which plane should we create the sprites?
	public enum SPRITE_PLANE
	{
		XY,
		XZ,
		YZ
	};

	public SPRITE_PLANE plane
	{
		get { return m_Plane; }
		set { m_Plane = value; }
	}

	public Material material
	{
		get { return m_Material; }
		set { m_Material = meshRenderer.GetComponent<Renderer>().material = value; }
	}

	protected Material m_Material; // The material to use for the sprites
	protected int allocBlockSize = 10;
				  // How many sprites to allocate space for at a time. ex: if set to 10, 10 new sprite blocks will be allocated at a time. Once all of these are used, 10 more will be allocated, and so on...
	protected SPRITE_PLANE m_Plane; // The plane in which to create the sprites

	protected bool countChanged = false;
	protected bool vertsChanged = false; // Have changes been made to the vertices of the mesh since the last frame?
	protected bool uvsChanged = false; // Have changes been made to the UVs of the mesh since the last frame?
	protected bool colorsChanged = false; // Have the colors changed?

	protected MFGuiSprite[] sprites;
							// Array of all sprites (the offset of the vertices corresponding to each sprite should be found simply by taking the sprite's index * 4 (4 verts per sprite).
	protected Queue<int> availableSprites = new Queue<int>();

	protected MFGuiQuad[] quads;
						  // Array of all sprites (the offset of the vertices corresponding to each sprite should be found simply by taking the sprite's index * 4 (4 verts per sprite).
	protected List<int> activeQuads = new List<int>(); // Array of references to all the currently active (non-empty) quads
	protected Queue<int> availableQuads = new Queue<int>(); // Array of references to quads which are currently not in use

	protected float boundUpdateInterval; // Interval, in seconds, to update the mesh bounds

	protected MeshFilter meshFilter;
	protected MeshRenderer meshRenderer;
	protected Mesh mesh; // Reference to our mesh (contained in the MeshFilter)

	protected Vector3[] vertices; // The vertices of our mesh
	protected int[] triIndices; // Indices into the vertex array
	protected Vector2[] UVs; // UV coordinates
	protected Color[] colors; // Color values

#if ENABLE_UPDATED_MESHES_HIGHLIGHTING
	static int s_RenderersCount = 0;				// Pocet rendereru
	static int s_TotalTrisUpdated = 0;
	static int s_TotalVertsUpdated = 0;
	static int s_CurrFrameCnt = -1;
	protected Material m_DbgMaterial;
#endif

	int m_RegisterdWidgetsCount = 0;

	// UTILITY FUNCTIONS

	// Converts pixel-space values to UV-space scalar values
	// according to the currently assigned material.
	// NOTE: This is for converting widths and heights-not
	// coordinates (which have reversed Y-coordinates).
	// For coordinates, use PixelCoordToUVCoord()!
	public Vector2 PixelSpaceToUVSpace(Vector2 xy)
	{
		//if( destroyed )
		//	UnityEngine.Debug.LogError("--- Renderer destroyed ---");

		if (m_Material == null)
		{
			//UnityEngine.Debug.LogWarning("+++++++++++++++++++++++++ PROBLEM, material not set +++++++++++++++++++++++");
			return Vector2.zero;
		}

		Texture t = m_Material.GetTexture("_MainTex");

		return new Vector2(xy.x/((float)t.width), xy.y/((float)t.height));
	}

	public void UVSpaceToPixelSpace(ref float x, ref float y)
	{
		if (m_Material == null)
		{
			return;
		}

		Texture t = m_Material.GetTexture("_MainTex");

		x *= t.width;
		y *= t.height;
	}

	// Converts pixel-space values to UV-space scalar values
	// according to the currently assigned material.
	// NOTE: This is for converting widths and heights-not
	// coordinates (which have reversed Y-coordinates).
	// For coordinates, use PixelCoordToUVCoord()!
	public Vector2 PixelSpaceToUVSpace(int x, int y)
	{
		return PixelSpaceToUVSpace(new Vector2((float)x, (float)y));
	}

	// Converts pixel coordinates to UV coordinates according to
	// the currently assigned material.
	// NOTE: This is for converting coordinates and will reverse
	// the Y component accordingly.  For converting widths and
	// heights, use PixelSpaceToUVSpace()!
	public Vector2 PixelCoordToUVCoord(Vector2 xy)
	{
		Vector2 p = PixelSpaceToUVSpace(xy);
		p.y = 1.0f - p.y;
		return p;
	}

	// Converts pixel coordinates to UV coordinates according to
	// the currently assigned material.
	// NOTE: This is for converting coordinates and will reverse
	// the Y component accordingly.  For converting widths and
	// heights, use PixelSpaceToUVSpace()!
	public Vector2 PixelCoordToUVCoord(int x, int y)
	{
		return PixelCoordToUVCoord(new Vector2((float)x, (float)y));
	}

	public bool IsAnySpriteActive()
	{
		return activeQuads.Count > 0;
	}

	// MONOBEHAVIOUR INTERFACE

	void Awake()
	{
#if ENABLE_UPDATED_MESHES_HIGHLIGHTING
				//Debug.Log("Vytvarim renderer pro GUI s poradovym cislem #" + s_RenderersCount);
		s_RenderersCount++;
		
		Material mat = AssetBundleManager.Instance.LoadFromResources("MainMenuResources", "effects/m_GUIDebugMat",typeof(Material)) as Material;
		
		if (mat)
		{
			Vector3	rndCol;
			
			rndCol.x = Random.Range(0.0f,1.0f);
			rndCol.y = Random.Range(0.0f,1.0f);
			rndCol.z = Random.Range(0.0f,1.0f);
			
			rndCol.Normalize();
			
			m_DbgMaterial = Instantiate(mat) as Material;
			m_DbgMaterial.SetColor("_Color",new Color(rndCol.x,rndCol.y,rndCol.z,1));
		}
		else
		{
			Debug.LogError("Cannot load GUI debug material");
		}
#endif

		GameObject go = gameObject;
		go.AddComponent<MeshFilter>();
		go.AddComponent<MeshRenderer>();

		meshFilter = (MeshFilter)GetComponent(typeof (MeshFilter));
		meshRenderer = (MeshRenderer)GetComponent(typeof (MeshRenderer));

		meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		meshRenderer.receiveShadows = false;

		meshRenderer.GetComponent<Renderer>().material = m_Material;
		mesh = meshFilter.mesh;

		// Create our first batch of sprites:
		EnlargeSpriteArrays(allocBlockSize);
		EnlargeQuadArrays(allocBlockSize);

		// Move the object to the origin so the objects drawn will not
		// be offset from the objects they are intended to represent.
		Transform trans = transform;
		trans.position = Vector3.zero;
		trans.rotation = Quaternion.identity;

		UpdateUISize();
	}

	void LateUpdate()
	{
		// deactivate this renderer if there is nothing to render
		if (IsAnySpriteActive() == false)
		{
			MFGuiManager.Instance.RegisterRendererForActivation(this);
			return;
		}

#if ENABLE_UPDATED_MESHES_HIGHLIGHTING		
		if (s_CurrFrameCnt != Time.frameCount)
		{
			Debug.Log("GUI Renderer mesh update info: " + s_TotalTrisUpdated + " tris, " + s_TotalVertsUpdated + " verts");
			
			s_TotalTrisUpdated = 0;
			s_TotalVertsUpdated = 0;
			
			s_CurrFrameCnt = Time.frameCount;
		}
#endif

		UpdateSprites();

#if !ENABLE_UPDATED_MESHES_HIGHLIGHTING
		UpdateMeshIfNeeded();
#else
		bool meshesUpdated = UpdateMeshIfNeeded();

		renderer.material = meshesUpdated ? m_DbgMaterial : m_Material;
#endif
	}

	void OnDestroy()
	{
		//UnityEngine.Debug.LogWarning("MFGUIRenderer on destroy");

		mesh = null;
		if (m_Material != null)
		{
			//Destroy(m_Material);
			m_Material = null;
		}
		//destroyed = true;
		availableQuads.Clear();
		activeQuads.Clear();
		quads = null;

		availableSprites.Clear();
		sprites = null;

		meshFilter = null;
		meshRenderer = null;

#if ENABLE_UPDATED_MESHES_HIGHLIGHTING
		s_RenderersCount--;
#endif
	}

	void OnEnable()
	{
		UpdateSprites();
		UpdateMeshIfNeeded();
	}

	// PUBLIC METHODS

	public void UpdateUISize()
	{
		_xOffset = -Screen.width/2.0f;
		_yOffset = Screen.height/2.0f;
	}

	public int RegisterWidget(GUIBase_Widget inWidget)
	{
		m_RegisterdWidgetsCount++;
		return m_RegisterdWidgetsCount;
	}

	public int UnRegisterWidget(GUIBase_Widget inWidget)
	{
		m_RegisterdWidgetsCount--;
		return m_RegisterdWidgetsCount;
	}

	// Adds a sprite to the manager at the location and rotation of the client 
	// GameObject and with its transform.  Returns a reference to the new sprite
	// Width and height are in world space units
	// lowerLeftUV - the UV coordinate for the upper-left corner
	// UVDimensions - the distance from lowerLeftUV to place the other UV coords
	public MFGuiSprite AddSprite(Matrix4x4 matrix, Rect rect, MFGuiUVCoords uvCoords, MFGuiGrid9 grid9)
	{
		LogFuncCall("AddSprite", name);

		if (availableSprites.Count < 1)
		{
			EnlargeSpriteArrays(allocBlockSize);
		}

		int index = availableSprites.Dequeue();

		if (sprites[index] == null)
		{
			sprites[index] = new MFGuiSprite(this, index);
		}

		rect = rect.MakePixelPerfect();

		// Assign the new sprite:
		MFGuiSprite sprite = sprites[index];
		sprite.matrix = matrix;
		sprite.size = new Vector2(rect.width, rect.height);
		sprite.uvCoords = uvCoords;
		sprite.grid9 = grid9 != null ? new MFGuiGrid9Cached(grid9) : default(MFGuiGrid9Cached);
		sprite.visible = true;

		// Done
		return sprite;
	}

	public void RemoveSprite(MFGuiSprite sprite)
	{
		LogFuncCall("RemoveSprite", name);

		// Clean the sprite's settings:
		sprite.matrix = Matrix4x4.identity;
		sprite.color = Color.white;
		sprite.offset = Vector3.zero;
		sprite.visible = false;

		// force release resources
		sprite.ReleaseResources();

		// store available slot
		availableSprites.Enqueue(sprite.index);
	}

	public MFGuiQuad GetAvailableQuad()
	{
		LogFuncCall("GetAvailableQuad", name);

		// Get an available sprite:
		if (availableQuads.Count < 1)
		{
			EnlargeQuadArrays(allocBlockSize); // If we're out of available sprites, allocate some more:
		}

		// Use a sprite from the list of available blocks:
		int index = availableQuads.Dequeue();

		// Assign the new sprite:
		MFGuiQuad quad = quads[index];

		// Save this to an active list now that it is in-use:
		if (activeQuads.Count == activeQuads.Capacity)
		{
			activeQuads.Capacity += 10;
		}
		activeQuads.Add(quad.index);

		// activate this renderer
		if (MFGuiManager.Instance != null)
		{
			MFGuiManager.Instance.RegisterRendererForActivation(this);
		}

		// Done
		return quad;
	}

	public void FreeQuad(MFGuiQuad quad)
	{
		LogFuncCall("RemoveQuad", name);

		int vidx = 4*quad.index;

		vertices[vidx + 0] = Vector3.zero;
		vertices[vidx + 1] = Vector3.zero;
		vertices[vidx + 2] = Vector3.zero;
		vertices[vidx + 3] = Vector3.zero;

		activeQuads.Remove(quad.index);
		availableQuads.Enqueue(quad.index);

		vertsChanged = true;
	}

	// Updates the vertices of a sprite based on the transform
	// of its client GameObject
	public void Transform(MFGuiSprite sprite)
	{
		LogFuncCall("Transform", name);

		sprite.UpdateSurface();
	}

	// Informs the SpriteManager that some vertices have changed position
	// and the mesh needs to be reconstructed accordingly
	public void UpdateVertices()
	{
		LogFuncCall("UpdateVertices", name);

		vertsChanged = true;
	}

	public void UpdateVertices(MFGuiQuad quad, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
	{
		LogFuncCall("UpdateVertices", name);

		int vidx = 4*quad.index;

		vertices[vidx + 0] = v1;
		vertices[vidx + 1] = v2;
		vertices[vidx + 2] = v3;
		vertices[vidx + 3] = v4;

		vertsChanged = true;
	}

	// Updates the UVs of the specified sprite and copies the new values
	// into the mesh object.
	public void UpdateUV(MFGuiQuad quad, Vector2 uvCoords, Vector2 uvDimensions)
	{
		LogFuncCall("UpdateUV", name);

		int vidx = 4*quad.index;

		UVs[vidx + 0] = uvCoords + Vector2.up*uvDimensions.y; // Upper-left
		UVs[vidx + 1] = uvCoords; // Lower-left
		UVs[vidx + 2] = uvCoords + Vector2.right*uvDimensions.x; // Lower-right
		UVs[vidx + 3] = uvCoords + uvDimensions; // Upper-right

		uvsChanged = true;
	}

	// Updates the color values of the specified sprite and copies the
	// new values into the mesh object.
	public void UpdateColors(MFGuiQuad quad, Color color)
	{
		LogFuncCall("UpdateColors", name);

		int vidx = 4*quad.index;

		colors[vidx + 0] = color;
		colors[vidx + 1] = color;
		colors[vidx + 2] = color;
		colors[vidx + 3] = color;

		colorsChanged = true;
	}

	public MFGuiSprite AddElement(Rect rect,
								  float angle,
								  float depth,
								  int leftPixelX,
								  int bottomPixelY,
								  int pixelWidth,
								  int pixelHeight,
								  MFGuiGrid9 grid9 = null)
	{
		LogFuncCall("AddElement", name);

		rect = rect.MakePixelPerfect();

		Vector2 uvCoords = PixelCoordToUVCoord(leftPixelX, bottomPixelY);
		Vector2 uvSizes = PixelSpaceToUVSpace(pixelWidth, pixelHeight);
		return AddElement(rect, angle, depth, new MFGuiUVCoords(uvCoords, uvSizes), grid9);
	}

	public MFGuiSprite AddElement(Rect rect, float angle, float depth, MFGuiUVCoords uvCoords, MFGuiGrid9 grid9)
	{
		LogFuncCall("AddElement", name);

		UpdateUISize();

		// create matrix
		Matrix4x4 matrix = Matrix4x4.identity;
		UpdateMatrix(ref matrix, rect, angle, Vector2.one, depth);

		rect = rect.MakePixelPerfect();

		// create sprite
		MFGuiSprite sprite = AddSprite(matrix, rect, uvCoords, grid9);

		return sprite;
	}

	public void UpdateSprite(MFGuiSprite sprite, Rect rect, float angle, Vector2 scale, float depth, MFGuiGrid9 grid9)
	{
		LogFuncCall("UpdateSpritePosSize", name);

		// update matrix
		UpdateMatrix(ref sprite.matrix, rect, angle, scale, depth);

		rect = rect.MakePixelPerfect();

		// setup surface
		sprite.size = new Vector2(rect.width, rect.height);

		// we can't allow to change grid9 once it has been assigned
		// but we can assign one if there is not any yet
		if (sprite.grid9 == null && grid9 != null)
		{
			sprite.grid9 = new MFGuiGrid9Cached(grid9);
		}

		// we can transform sprite now
		Transform(sprite);
	}

	// PRIVATE METHODS

	void UpdateMatrix(ref Matrix4x4 matrix, Rect rect, float rotation, Vector2 scale, float depth)
	{
		// update rotation
		Vector3 r = Mathfx.Matrix_GetEulerAngles(matrix);
		r.z = rotation*Mathf.Deg2Rad;
		Mathfx.Matrix_SetEulerAngles(ref matrix, r);

		// update position
		Vector3 p = new Vector3(0.0f, 0.0f, depth);
		p.x = rect.x + _xOffset;
		p.y = (int)ZeroLocation*(-(rect.y - rect.height*0.5f) + (_yOffset - rect.height*0.5f));
		Mathfx.Matrix_SetPos(ref matrix, p);

		// update scale
		Vector3 s = Mathfx.Matrix_GetScale(matrix);
		s.x *= scale.x;
		s.y *= scale.y;
		Mathfx.Matrix_SetScale(ref matrix, s);
	}

	void UpdateSprites()
	{
		//FIXME: This is a hotfix for the situation when a player just got returned to the main menu after finishing a game
		// and the OnEnable() event is called but the sprite array is not yet initialized
		if (sprites == null)
		{
			Debug.LogError("MFGuiRenderer.UpdateSprites() :: Sprite array is not initialized!", gameObject);
			return;
		}

		foreach (var sprite in sprites)
		{
			if (sprite != null)
			{
				sprite.LateUpdate();
			}
		}
	}

	bool UpdateMeshIfNeeded()
	{
		if (vertsChanged == true || colorsChanged == true || uvsChanged == true)
		{
			if (countChanged == true)
			{
				mesh.Clear();
			}

			if (vertsChanged == true)
			{
				mesh.vertices = vertices;
			}

			if (uvsChanged == true)
			{
				mesh.uv = UVs;
			}

			if (colorsChanged == true)
			{
				mesh.colors = colors;
			}

			if (countChanged == true)
			{
				mesh.triangles = triIndices;
			}

			countChanged = false;
			colorsChanged = false;
			vertsChanged = false;
			uvsChanged = false;

#if ENABLE_UPDATED_MESHES_HIGHLIGHTING
				//UnityEngine.Debug.Log("Mesh rebuild: num verts = " + mesh.vertices.Length + ", num tris = " + mesh.triangles.Length + " " + name + "frame " + Time.frameCount);

			s_TotalVertsUpdated += mesh.vertices.Length;
			s_TotalTrisUpdated += mesh.triangles.Length;
#endif
			return true;
		}

		return false;
	}

	// Allocates initial arrays
	void InitArrays()
	{
		sprites = new MFGuiSprite[0];
		quads = new MFGuiQuad[0];
		vertices = new Vector3[4];
		UVs = new Vector2[4];
		colors = new Color[4];
		triIndices = new int[6];
	}

	void EnlargeSpriteArrays(int count)
	{
		if (sprites == null)
		{
			InitArrays();
		}

		// Resize sprite array:
		MFGuiSprite[] tempSprites = sprites;
		sprites = new MFGuiSprite[tempSprites.Length + count];
		tempSprites.CopyTo(sprites, 0);

		for (int idx = tempSprites.Length; idx < sprites.Length; ++idx)
		{
			availableSprites.Enqueue(idx);
		}
	}

	// Enlarges the sprite array by the specified count and also resizes
	// the UV and vertex arrays by the necessary corresponding amount.
	// Returns the index of the first newly allocated element
	// (ex: if the sprite array was already 10 elements long and is 
	// enlarged by 10 elements resulting in a total length of 20, 
	// EnlargeArrays() will return 10, indicating that element 10 is the 
	// first of the newly allocated elements.)
	void EnlargeQuadArrays(int count)
	{
		if (quads == null)
		{
			InitArrays();
		}

		// Resize sprite array:
		MFGuiQuad[] tempQuads = quads;
		quads = new MFGuiQuad[tempQuads.Length + count];
		tempQuads.CopyTo(quads, 0);

		// Vertices:
		Vector3[] tempVerts = vertices;
		vertices = new Vector3[vertices.Length + count*4];
		tempVerts.CopyTo(vertices, 0);

		// UVs:
		Vector2[] tempUVs = UVs;
		UVs = new Vector2[UVs.Length + count*4];
		tempUVs.CopyTo(UVs, 0);

		// Colors:
		Color[] tempColors = colors;
		colors = new Color[colors.Length + count*4];
		tempColors.CopyTo(colors, 0);

		// Triangle indices:
		int[] tempTris = triIndices;
		triIndices = new int[triIndices.Length + count*6];
		tempTris.CopyTo(triIndices, 0);

		// Setup the newly-added sprites and Add them to the list of available 
		// sprite blocks. Also initialize the triangle indices while we're at it:
		for (int idx = tempQuads.Length; idx < quads.Length; ++idx)
		{
			// Create and setup quad
			quads[idx] = new MFGuiQuad(idx);

			// Add as an available quad
			availableQuads.Enqueue(idx);

			// Init triangle indices:
			// Clockwise winding
			triIndices[idx*6 + 0] = idx*4 + 0; //  0_ 1            0 ___ 3
			triIndices[idx*6 + 1] = idx*4 + 3; //  | /      Verts:  | / |
			triIndices[idx*6 + 2] = idx*4 + 1; // 2|/              1|/__|2

			triIndices[idx*6 + 3] = idx*4 + 3; //    3
			triIndices[idx*6 + 4] = idx*4 + 2; //   /|
			triIndices[idx*6 + 5] = idx*4 + 1; // 5/_|4
		}

		countChanged = true;
	}

	[System.Diagnostics.Conditional("DEBUG_GUI_RENDERER")]
	public static void LogFuncCall(string funcName, string rendererId)
	{
		//UnityEngine.Debug.Log(funcName + " " + rendererId);
	}

	public bool GetSpriteSizeAndPosition(MFGuiSprite sprite, out Vector2 size, out Vector3 pos)
	{
		pos = new Vector2(0, 0);
		size = new Vector2(0, 0);

		if (sprite == null || sprite.index >= sprites.Length ||
			sprites[sprite.index] != sprite || sprite.quads.Length < 1)
			return false;

		int id = sprite.quads[0].index*4;

		Vector3 v0 = vertices[id + 0];
		Vector3 v2 = vertices[id + 2];

		pos = v0;
		size = new Vector2(Mathf.Abs(v2.x - v0.x), Mathf.Abs(v2.y - v0.y));

		return true;
	}
}
