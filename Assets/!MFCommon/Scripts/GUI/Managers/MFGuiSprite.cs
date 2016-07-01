//
// By using or accessing the source codes or any other information of the Game SHADOWGUN: DeadZone ("Game"),
// you ("You" or "Licensee") agree to be bound by all the terms and conditions of SHADOWGUN: DeadZone Public
// License Agreement (the "PLA") starting the day you access the "Game" under the Terms of the "PLA".
//
// You can review the most current version of the "PLA" at any time at: http://madfingergames.com/pla/deadzone
//
// If you don't agree to all the terms and conditions of the "PLA", you shouldn't, and aren't permitted
// to use or access the source codes or any other information of the "Game" supplied by MADFINGER Games, a.s.
//

using UnityEngine;

public struct MFGuiUVCoords
{
	public float U;
	public float V;
	public float Width;
	public float Height;

	public MFGuiUVCoords(float u, float v, float width, float height)
	{
		U = u;
		V = v;
		Width = width;
		Height = height;
	}

	public MFGuiUVCoords(Vector2 coords, Vector2 size)
	{
		U = coords.x;
		V = coords.y;
		Width = size.x;
		Height = size.y;
	}
}

public class MFGuiSprite
{
	protected enum UpdateType
	{
		None = 0,
		Verts = 1,
		UVs = 2,
		Color = 4
	}

	// PRIVATE MEMBERS

	protected Vector3 m_Offset;
	protected Vector2 m_Size;
	protected MFGuiGrid9Cached m_Grid9;

	protected MFGuiUVCoords m_UVCoords;

	protected Color m_Color;

	protected MFGuiRenderer m_GuiRenderer;
	protected MFGuiQuad[] m_Quads;

	protected bool m_Visible;
	protected int m_UpdateType;

	// PUBLIC MEMBERS

	public readonly int index; // Index of this sprite in Renderer's list
	public Matrix4x4 matrix;

	// GETTERS / SETTERS

	public MFGuiQuad[] quads
	{
		get { return m_Quads; }
	}

	public bool visible
	{
		get { return m_Visible; }
		set
		{
			if (m_Visible == value)
				return;
			m_Visible = value;

			if (m_Visible == true)
			{
				UpdateSurface();
			}
			else
			{
				ReleaseResources();
			}
		}
	}

	public Vector3 offset
	{
		get { return m_Offset; }
		set
		{
			m_Offset = value;
			SetUpdateType(UpdateType.Verts);
		}
	}

	public float width
	{
		get { return m_Size.x; }
		set
		{
			m_Size.x = value;
			SetUpdateType(UpdateType.Verts);
		}
	}

	public float height
	{
		get { return m_Size.y; }
		set
		{
			m_Size.y = value;
			SetUpdateType(UpdateType.Verts);
		}
	}

	public Vector2 size
	{
		get { return m_Size; }
		set
		{
			m_Size = value;
			SetUpdateType(UpdateType.Verts);
		}
	}

	public MFGuiGrid9Cached grid9
	{
		get { return m_Grid9; }
		set
		{
			m_Grid9 = value;
			SetUpdateType(UpdateType.Verts);
		}
	}

	public MFGuiUVCoords uvCoords
	{
		get { return m_UVCoords; }
		set
		{
			m_UVCoords = value;
			SetUpdateType(UpdateType.UVs);
		}
	}

	public Color color
	{
		get { return m_Color; }
		set
		{
			m_Color = value;
			SetUpdateType(UpdateType.Color);
		}
	}

	public float alpha
	{
		get { return m_Color.a; }
		set
		{
			m_Color.a = value;
			SetUpdateType(UpdateType.Color);
		}
	}

	// C-TOR / D-TOR

	public MFGuiSprite(MFGuiRenderer renderer, int index)
	{
		this.m_GuiRenderer = renderer;
		this.m_Quads = new MFGuiQuad[0];
		this.index = index;
	}

	~MFGuiSprite()
	{
		m_Grid9 = null;
		m_GuiRenderer = null;
		m_Quads = null;
	}

	// PUBLIC METHODS

	public void UpdateSurface()
	{
		SetUpdateType(UpdateType.Verts);
	}

	public void LateUpdate()
	{
		if (m_Size.x <= 0.0f || m_Size.y <= 0.0f)
			return;
		//if (Mathf.Abs(m_UVCoords.Width) == 0.0f || Mathf.Abs(m_UVCoords.Height) <= 0.0f)
		//	return;

		if (m_UpdateType != (int)UpdateType.None)
		{
			bool updateVerts = (m_UpdateType & (int)UpdateType.Verts) != 0 ? true : false;
			bool updateUVs = (m_UpdateType & (int)UpdateType.UVs) != 0 ? true : false;
			bool updateColor = (m_UpdateType & (int)UpdateType.Color) != 0 ? true : false;

			UpdateQuads(updateVerts, updateUVs, updateColor);

			m_UpdateType = (int)UpdateType.None;
		}
	}

	public void ReleaseResources()
	{
		m_Size = new Vector2();
		m_Grid9 = null;

		foreach (var quad in m_Quads)
		{
			quad.ReleaseResources(m_GuiRenderer);
		}
		m_Quads = new MFGuiQuad[0];
	}

	// PRIVATE METHODS

	void SetUpdateType(UpdateType type)
	{
		m_UpdateType = m_UpdateType | (int)type;

		// force update if there is not any quad created yet
		if (m_Quads.Length == 0)
		{
			LateUpdate();
		}
	}

	void UpdateQuads(bool updateVerts, bool updateUVs, bool updateColor)
	{
		// deduce actual bounds
		float[] xAxis, yAxis;
		byte nonEmpty = ComputeSegments(out xAxis, out yAxis);

		// deduce full update
		bool fullUpdate = nonEmpty != m_Quads.Length;

		// refresh quad list
		if (fullUpdate == true)
		{
			RefreshQuadList(nonEmpty);
		}

		float originWidth = m_UVCoords.Width;
		float originHeight = m_UVCoords.Height;
		m_GuiRenderer.UVSpaceToPixelSpace(ref originWidth, ref originHeight);

		float widthMult = originWidth/m_Size.x;
		float heightMult = originHeight/m_Size.y;

		// udpate quads
		// we are going in bottom-up direction
		// so we need to go from the end of the list
		int quadIdx = 0;
		for (int yIdx = 2; yIdx >= 0; --yIdx)
		{
			float y1 = yAxis[yIdx + 1];
			float y2 = yAxis[yIdx];

			// do not create quads for empty row
			float posY1, posY2;
			ComputeRealValues(2 - yIdx, m_Size.y, y1*heightMult, y2*heightMult, out posY1, out posY2);
			if (posY2 - posY1 <= 0.0f)
				continue;

			for (int xIdx = 0; xIdx < 3; ++xIdx)
			{
				float x1 = xAxis[xIdx];
				float x2 = xAxis[xIdx + 1];

				// do not create quads for empty column
				float posX1, posX2;
				ComputeRealValues(xIdx, m_Size.x, x1*widthMult, x2*widthMult, out posX1, out posX2);
				if (posX2 - posX1 <= 0.0f)
					continue;

				MFGuiQuad quad = m_Quads[quadIdx];
				if (quad == null)
					continue;

				// update screen coords
				if (fullUpdate == true || updateVerts == true)
				{
					Rect rect = new Rect(m_Offset.x + posX1, m_Offset.y + posY1, posX2 - posX1, posY2 - posY1);

					if (nonEmpty > 1)
					{
						rect.x = rect.x - m_Size.x*0.5f + rect.width*0.5f;
						rect.y = rect.y - m_Size.y*0.5f + rect.height*0.5f;
					}

					quad.UpdateVertices(m_GuiRenderer, matrix, rect, 0.0f);
				}

				// update UV coords
				if (fullUpdate == true || updateUVs == true)
				{
					float uvX1, uvX2;
					ComputeRealValues(xIdx, m_UVCoords.Width, x1, x2, out uvX1, out uvX2);

					float uvY1, uvY2;
					ComputeRealValues(2 - yIdx, m_UVCoords.Height, y1, y2, out uvY1, out uvY2);

					Vector2 uvCoords = new Vector2(m_UVCoords.U + uvX1, m_UVCoords.V + uvY1);
					Vector2 uvDimensions = new Vector2(uvX2 - uvX1, uvY2 - uvY1);

					quad.UpdateUVs(m_GuiRenderer, uvCoords, uvDimensions);
				}

				// update color
				if (fullUpdate == true || updateColor == true)
				{
					quad.SetColor(m_GuiRenderer, m_Color);
				}

				// go for new quad
				quadIdx += 1;
			}
		}
	}

	byte ComputeSegments(out float[] xAxis, out float[] yAxis)
	{
		if (m_Grid9 != null)
		{
			xAxis = m_Grid9.x;
			yAxis = m_Grid9.y;
			return m_Grid9.c;
		}
		else
		{
			xAxis = new float[4];
			yAxis = new float[4];
			return 1;
		}
	}

	void ComputeRealValues(int idx, float maxValue, float val1, float val2, out float res1, out float res2)
	{
		val1 *= maxValue;
		val2 *= maxValue;

		switch (idx)
		{
		case 0:
			res1 = val1;
			res2 = val2;
			break;
		case 1:
		default:
			res1 = val1;
			res2 = maxValue - val2;
			break;
		case 2:
			res1 = maxValue - val1;
			res2 = maxValue - val2;
			break;
		}
	}

	void RefreshQuadList(byte maxCount)
	{
		if (m_Quads.Length < maxCount)
		{
			// remove unused quads
			MFGuiQuad[] quads = m_Quads;
			m_Quads = new MFGuiQuad[maxCount];
			quads.CopyTo(m_Quads, 0);

			for (int idx = quads.Length; idx < m_Quads.Length; ++idx)
			{
				m_Quads[idx] = m_GuiRenderer.GetAvailableQuad();
			}
		}
		else if (m_Quads.Length > maxCount)
		{
			// insert additional quads
			MFGuiQuad[] quads = m_Quads;
			m_Quads = new MFGuiQuad[maxCount];

			for (int idx = 0; idx < quads.Length; ++idx)
			{
				if (idx < m_Quads.Length)
				{
					m_Quads[idx] = quads[idx];
				}
				else
				{
					quads[idx].ReleaseResources(m_GuiRenderer);
				}
			}
		}
	}

	public bool GetSpriteSizeAndPosition(out Vector2 size, out Vector3 pos)
	{
		return m_GuiRenderer.GetSpriteSizeAndPosition(this, out size, out pos);
	}
}
