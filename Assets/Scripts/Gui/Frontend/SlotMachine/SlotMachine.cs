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
using System.Collections;
using System.Collections.Generic;

public class SlotMachine : MonoBehaviour
{
	readonly static string START_ANIM = "start";
	readonly static string SPIN_ANIM = "roll";
	readonly static string STOP_ANIM = "end";

	enum SlotColumn : short
	{
		A,
		B,
		C
	}

	enum SlotRow : short
	{
		Top,
		Middle,
		Bottom
	}

	// PRIVATE MEMBERS

	[SerializeField] Camera m_Camera;
	[SerializeField] AudioClip m_OpenSound;
	[SerializeField] Animation m_SpinAnim;
	[SerializeField] AudioClip m_StartSound;
	[SerializeField] AudioClip m_SpinSound;
	[SerializeField] AudioClip m_StopSound;
	[SerializeField] Animation m_WinAnim;
	[SerializeField] AudioClip m_WinSound;

	Slot[][] m_Slots = new Slot[3][];
	Camera m_PrevCamera;
	GameObject[] m_BlurSlots = new GameObject[3];

	// GETTERS/SETTERS

	public bool IsInitalized { get; private set; }
	public bool IsActive { get; private set; }
	public bool IsBusy { get; private set; }
	public int Reward { get; private set; }

	// PUBLIC METHODS

	public void Initialize()
	{
		if (IsInitalized == true)
			return;

		m_Slots[(int)SlotColumn.A] = PrepareSlots("slot_machine_SYMBOLS/A_0{0}");
		m_Slots[(int)SlotColumn.B] = PrepareSlots("slot_machine_SYMBOLS/B_0{0}");
		m_Slots[(int)SlotColumn.C] = PrepareSlots("slot_machine_SYMBOLS/C_0{0}");

		Transform trans = transform;
		m_BlurSlots[(int)SlotColumn.A] = trans.Find("slot_machine_SYMBOLS/X_BLUR_A").gameObject;
		m_BlurSlots[(int)SlotColumn.B] = trans.Find("slot_machine_SYMBOLS/X_BLUR_B").gameObject;
		m_BlurSlots[(int)SlotColumn.C] = trans.Find("slot_machine_SYMBOLS/X_BLUR_C").gameObject;

		IsInitalized = true;
	}

	public void Activate()
	{
		if (IsActive == true)
			return;
		IsActive = true;

		Reset();

		GetComponent<AudioSource>().PlayOneShot(m_OpenSound, AudioListener.volume); //Added volume, otherwise in webplayer it was always playing on max

		EnableCamera(true);
	}

	public void Deactivate()
	{
		if (IsActive == false)
			return;
		IsActive = false;

		Reset();

		EnableCamera(false);
	}

	public void Reset()
	{
		StopAllCoroutines();
		m_SpinAnim.Stop();
		m_WinAnim.Stop();
		GetComponent<AudioSource>().Stop();
		SetRandomMachineState();
		IsBusy = false;
		Reward = 0;

		ShowSpinAnimation(false);
	}

	public bool Spin()
	{
		if (IsBusy == true)
			return false;

		StartCoroutine(Spin_Coroutine());

		return true;
	}

	/*public void Spin(E_SlotMachineSymbol symbol, bool shouldWin)
	{
		m_BusyTime   = Time.timeSinceLevelLoad;
		m_RewardTime = 0.0f;
		
		if (shouldWin == true)
		{	
			m_BusyTime   += m_SpinWinSound.length;
			m_WinAnimTime = Time.timeSinceLevelLoad + m_SlotMachineAnim.clip.length;
			m_RewardTime  = m_WinAnimTime + 0.5f;
		}	
		else
		{	
			m_BusyTime   += m_SpinLostSound.length;
			m_WinAnimTime = 0;
		}	
		
		m_WinAnim.Stop();
		m_SlotMachineAnim.Rewind();
		m_SlotMachineAnim.Play();
		
		if (shouldWin == true)
		{
			audio.PlayOneShot(m_SpinWinSound, AudioListener.volume); //Added volume, otherwise in webplayer it was always playing on max
		}
		else
		{
			audio.PlayOneShot(m_SpinLostSound, AudioListener.volume); //Added volume, otherwise in webplayer it was always playing on max
		}

		StartCoroutine(SpinLocal_Coroutine(symbol, shouldWin));
	}*/

	public void SetRandomMachineState()
	{
		SetMachineState((E_SlotMachineSymbol)Random.Range((int)E_SlotMachineSymbol.First, (int)E_SlotMachineSymbol.Last + 1), false);
	}

	// PRIVATE METHODS

	void EnableCamera(bool state)
	{
		if (state == true)
		{
			if (Camera.current != m_Camera)
			{
				m_PrevCamera = Camera.main;
				if (m_PrevCamera != null)
				{
					m_PrevCamera.enabled = false;
				}
			}

			m_Camera.enabled = true;
			m_Camera.GetComponent<Animation>().Rewind();
			m_Camera.GetComponent<Animation>().Play();
		}
		else
		{
			m_Camera.GetComponent<Animation>().Stop();
			m_Camera.enabled = false;

			if (m_PrevCamera != null)
			{
				m_PrevCamera.enabled = true;
				m_PrevCamera = null;
			}
		}
	}

	IEnumerator Spin_Coroutine()
	{
		IsBusy = true;
		Reward = 0;

		int reward;
		E_SlotMachineSymbol[] matrix;

		// create cloud action
		SlotMachineSpin action = new SlotMachineSpin(CloudUser.instance.authenticatedUserID);
		GameCloudManager.AddAction(action);

		// spin symbols
		{
			m_WinAnim.Stop();
			m_SpinAnim.Stop();

			ShowSpinAnimation(true);

			// play spin sound
			GetComponent<AudioSource>().loop = true;
			GetComponent<AudioSource>().clip = m_SpinSound;
			GetComponent<AudioSource>().Play();

			// play start sound
			GetComponent<AudioSource>().PlayOneShot(m_StartSound, AudioListener.volume); //Added volume, otherwise in webplayer it was always playing on max

			// start slot machine
			m_SpinAnim.Play(START_ANIM);
			while (m_SpinAnim.isPlaying == true)
			{
				yield return new WaitForEndOfFrame();
			}

			m_SpinAnim.Play(SPIN_ANIM);
		}

		// wait for cloud action to finish
		{
			float delay = 0.5f;
			do
			{
				yield return new WaitForSeconds(delay);
				delay = 0.1f;
			} while (action.isDone == false);

			// get result from cloud
			if (action.isSucceeded == true)
			{
				reward = action.Reward;
				matrix = action.Matrix;

				SetMachineState(action.Matrix);
			}
			else
			{
				reward = 0;
				matrix = new E_SlotMachineSymbol[9];

				// update slot machine slots
				SetMachineState(E_SlotMachineSymbol.First, false);
			}
		}

		// stop slot machine
		{
			// stop slots
			m_SpinAnim.Play(STOP_ANIM);

			float oneStopDeuration = m_SpinAnim.GetClip(STOP_ANIM).length/3.0f;
			for (int idx = 0; idx < 3; ++idx)
			{
				GetComponent<AudioSource>().PlayOneShot(m_StopSound, AudioListener.volume); //Added volume, otherwise in webplayer it was always playing on max

				yield return new WaitForSeconds(oneStopDeuration);
			}

			// stop symbol spinning
			ShowSpinAnimation(false);

			// stop spin sound
			GetComponent<AudioSource>().Stop();

			// play win anim if needed
			if (reward > 0)
			{
				int[] indexes = GetWinningSymbols(matrix);

				m_WinAnim.Play();

				GetComponent<AudioSource>().loop = false;

				float step = reward*0.1f > 4.0f ? 4.0f/reward : 0.1f;
				float temp = (float)Reward;
				do
				{
					temp += reward*step;
					Reward = Mathf.Min(reward, (int)temp);

					GetComponent<AudioSource>().clip = m_WinSound;
					GetComponent<AudioSource>().Play();

					if (m_WinAnim.isPlaying == false)
					{
						m_WinAnim.Play();
					}

					foreach (var index in indexes)
					{
						Slot slot = GetSlot(index);
						slot.Shine = !slot.Shine;
					}

					yield return new WaitForSeconds(0.075f);

					foreach (var index in indexes)
					{
						Slot slot = GetSlot(index);
						slot.Shine = !slot.Shine;
					}

					yield return new WaitForSeconds(0.025f);
				} while (Reward < reward);

				m_WinAnim.Stop();

				foreach (var index in indexes)
				{
					Slot slot = GetSlot(index);
					slot.Shine = false;
				}

				Reward = reward;
			}
		}

		// done
		IsBusy = false;
	}

	int[] GetWinningSymbols(E_SlotMachineSymbol[] matrix)
	{
		List<int> indexes = new List<int>();

		// check lines
		for (int idx = 0; idx < 3; ++idx)
		{
			CheckSymbols(
						 matrix,
						 0 + idx,
						 3 + idx,
						 6 + idx,
						 ref indexes);
		}

		// top-left -> bottom-right
		{
			CheckSymbols(
						 matrix,
						 0 + 0,
						 3 + 1,
						 6 + 2,
						 ref indexes);
		}

		// bottom-left -> top-right
		{
			CheckSymbols(
						 matrix,
						 6 + 0,
						 3 + 1,
						 0 + 2,
						 ref indexes);
		}

		return indexes.ToArray();
	}

	void CheckSymbols(E_SlotMachineSymbol[] matrix, int aIdx, int bIdx, int cIdx, ref List<int> indexes)
	{
		E_SlotMachineSymbol a = matrix[aIdx];
		E_SlotMachineSymbol b = matrix[bIdx];
		E_SlotMachineSymbol c = matrix[cIdx];
		if (IsWinningCombination(a, b) && IsWinningCombination(a, c) && IsWinningCombination(b, c))
		{
			if (indexes.Contains(aIdx) == false)
			{
				indexes.Add(aIdx);
			}
			if (indexes.Contains(bIdx) == false)
			{
				indexes.Add(bIdx);
			}
			if (indexes.Contains(cIdx) == false)
			{
				indexes.Add(cIdx);
			}
		}
	}

	bool IsWinningCombination(E_SlotMachineSymbol left, E_SlotMachineSymbol right)
	{
		if (left == right)
			return true;
		if (left == E_SlotMachineSymbol.Jackpot)
			return true;
		if (right == E_SlotMachineSymbol.Jackpot)
			return true;
		return false;
	}

	void SetMachineState(E_SlotMachineSymbol[] matrix)
	{
		// column A
		SetSymbol(SlotColumn.A, SlotRow.Top, matrix[0]);
		SetSymbol(SlotColumn.A, SlotRow.Middle, matrix[1]);
		SetSymbol(SlotColumn.A, SlotRow.Bottom, matrix[2]);

		// collumn B
		SetSymbol(SlotColumn.B, SlotRow.Top, matrix[3]);
		SetSymbol(SlotColumn.B, SlotRow.Middle, matrix[4]);
		SetSymbol(SlotColumn.B, SlotRow.Bottom, matrix[5]);

		// collumn C
		SetSymbol(SlotColumn.C, SlotRow.Top, matrix[6]);
		SetSymbol(SlotColumn.C, SlotRow.Middle, matrix[7]);
		SetSymbol(SlotColumn.C, SlotRow.Bottom, matrix[8]);
	}

	void SetMachineState(E_SlotMachineSymbol symbol, bool shouldWin)
	{
		E_SlotMachineSymbol symbolA;
		E_SlotMachineSymbol symbolB;
		E_SlotMachineSymbol symbolC;

		if (shouldWin == true)
		{
			symbolA = symbol;
			symbolB = symbol;
			symbolC = symbol;
		}
		else
		{
			symbolA = (E_SlotMachineSymbol)Random.Range((int)E_SlotMachineSymbol.First, (int)E_SlotMachineSymbol.Last + 1);
			do
			{
				symbolB = (E_SlotMachineSymbol)Random.Range((int)E_SlotMachineSymbol.First, (int)E_SlotMachineSymbol.Last + 1);
				symbolC = (E_SlotMachineSymbol)Random.Range((int)E_SlotMachineSymbol.First, (int)E_SlotMachineSymbol.Last + 1);
			} while ((symbolA == symbolB) && (symbolB == symbolC));
		}

		// column A
		SetSymbol(SlotColumn.A, SlotRow.Top, (E_SlotMachineSymbol)((int)symbolA - 1));
		SetSymbol(SlotColumn.A, SlotRow.Middle, symbolA);
		SetSymbol(SlotColumn.A, SlotRow.Bottom, (E_SlotMachineSymbol)((int)symbolA + 1));

		// collumn B
		SetSymbol(SlotColumn.B, SlotRow.Top, (E_SlotMachineSymbol)((int)symbolB - 4));
		SetSymbol(SlotColumn.B, SlotRow.Middle, symbolB);
		SetSymbol(SlotColumn.B, SlotRow.Bottom, (E_SlotMachineSymbol)((int)symbolB + 2));

		// collumn C
		SetSymbol(SlotColumn.C, SlotRow.Top, (E_SlotMachineSymbol)((int)symbolC + 3));
		SetSymbol(SlotColumn.C, SlotRow.Middle, symbolC);
		SetSymbol(SlotColumn.C, SlotRow.Bottom, (E_SlotMachineSymbol)((int)symbolC - 3));
	}

	void SetSymbol(SlotColumn col, SlotRow row, E_SlotMachineSymbol symbol)
	{
		if ((int)symbol < (int)E_SlotMachineSymbol.First)
		{
			symbol = (E_SlotMachineSymbol)((int)symbol + (int)E_SlotMachineSymbol.Last);
		}
		if ((int)symbol > (int)E_SlotMachineSymbol.Last)
		{
			symbol = (E_SlotMachineSymbol)((int)symbol - (int)E_SlotMachineSymbol.Last);
		}
		m_Slots[(int)col][(int)row].SetSymbol(symbol);
	}

	Slot[] PrepareSlots(string ident)
	{
		Transform trans = transform;
		Slot[] slots = new Slot[3];

		for (int idx = 0; idx < slots.Length; ++idx)
		{
			slots[idx] = new Slot(trans.Find(string.Format(ident, idx + 1)).gameObject);
		}

		return slots;
	}

	Slot GetSlot(int index)
	{
		int col = index/3;
		int row = index%3;

		//Debug.Log(index+", "+col+", "+row);

		return m_Slots[col][row];
	}

	void ShowSpinAnimation(bool state)
	{
		foreach (GameObject obj in m_BlurSlots)
		{
			obj.SetActive(state);
		}
	}

	// --------

	class Slot
	{
		readonly static int m_Rows = 3;
		readonly static int m_Columns = 3;

		MeshRenderer m_Mesh;
		GameObject m_Glow;

		public bool Shine
		{
			get { return m_Glow.activeSelf; }
			set { m_Glow.SetActive(value); }
		}

		public Slot(GameObject obj)
		{
			m_Mesh = obj.GetComponent<MeshRenderer>();
			m_Glow = obj.transform.FindChild("glow").gameObject;

			Shine = false;
		}

		public void SetSymbol(E_SlotMachineSymbol symbol)
		{
			int idx = (int)symbol;

			// compute size of cell
			Vector2 size = new Vector2(1.0f/m_Columns, 1.0f/m_Rows);

			// split into coords
			int uIndex = idx/m_Rows;
			int vIndex = idx%m_Rows;

			// set new texture coords
			Material mat = m_Mesh.material;
			Vector2 offset = mat.mainTextureOffset;
			offset.x = (-1.0f + size.x) + uIndex*size.x;
			offset.y = (+1.0f - size.y) - vIndex*size.y;
			mat.mainTextureOffset = offset;
		}
	}
}
