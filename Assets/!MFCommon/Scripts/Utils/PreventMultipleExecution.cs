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

//
//	File:	PreventMultipleExecution.cs
//	Desc:	This component check whether another instance of this application is already running and quits the current one if so to prevent multiple instances being executed.
//	Note:	Windows only (other platforms are handled by the respective OS)
//

using UnityEngine;

#if UNITY_STANDALONE_WIN
  using System;
  using System.Diagnostics;
  using System.Threading;
  using System.Runtime.InteropServices;
#endif

//
class PreventMultipleExecution : MonoBehaviour
{
	//editable props
	public string m_WindowCaption = string.Empty;
	public string m_WindowClass = "UnityWndClass";

#if UNITY_STANDALONE_WIN
				//use old good Win32 API, C# doesn't have an alternative
	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
	private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")]
	private static extern IntPtr FindWindow(string className, string windowName);

	//
	private static PreventMultipleExecution		m_Instance = null;
	private System.Threading.Mutex				m_Mutex = null;

	
	//
	void Awake()
	{
		if (m_Instance != null)
		{
			Destroy(this.gameObject);
			return;
		}
		
		m_Instance = this;
		
		UnityEngine.Object.DontDestroyOnLoad(this);
		
		if ( IsAlreadyRunning() )
		{
			//bring the original instance to front
			BringToFront();
			
			Application.Quit();
		}
	}

	//
	void OnApplicationQuit()
	{
		if (m_Mutex != null)
			m_Mutex.ReleaseMutex();
	}
	
	//is another instance of the application already running?
	private bool IsAlreadyRunning()
	{
		//create our mutex
		bool createdNew;
		m_Mutex = new System.Threading.Mutex(true, "Global\\" + "MADFINGER_GAMES_" + m_WindowCaption, out createdNew);
		
		if (!createdNew)
			m_Mutex = null;				//we do not own the mutex in this case, so we should not Release it in OnApplicationQuit()
		
		return (createdNew == false);	//if we didn't have to create a new mutex, it means there is already one so the app is already running
	}

	//bring the window of the original instance of this application to front
	private void BringToFront()
	{
		if ( string.IsNullOrEmpty(m_WindowCaption) )
			return;
		
		const int 	SW_RESTORE = 9;
		IntPtr 		INVALID_HANDLE_VALUE = new IntPtr(-1);
		
		IntPtr	hWnd = FindWindow( string.IsNullOrEmpty(m_WindowClass) ? null : m_WindowClass, m_WindowCaption);
		
		if (hWnd == INVALID_HANDLE_VALUE)
			return;

		if ( IsIconic(hWnd) )
			ShowWindow(hWnd, SW_RESTORE);
		
		SetForegroundWindow(hWnd);
	}
	
	
	
/*	//NOTE: This doesn't work in Unity. Always getting a "InvalidOperationException: Process has exited, so the requested information is not available."
	private bool IsAlreadyRunning()
	{
		Process ourProcess = Process.GetCurrentProcess();
		
		if (ourProcess == null)
		{
			UnityEngine.Debug.Log("PreventMultipleExecution: !ourProcess");
			return false;
		}
		
		string ourProcessName	= ourProcess.ProcessName;			//InvalidOperationException: Process has exited, so the requested information is not available.
		Process[] processes		= Process.GetProcessesByName(ourProcessName);
//		Process[] processes		= Process.GetProcesses();
//		
//		for (int i = 0; i < processes.Length; i++)
//		{
//			if (processes[i] == null || processes[i].HasExited)		//HasExited doesn't do the job - returns false and than we get the InvalidOperationException. :-(
//				continue;
//			
//			UnityEngine.Debug.Log("PreventMultipleExecution: processName=" + processes[i].ProcessName + ", index=" + i);
//		}
		
		return (processes.Length > 1) ? true : false;
	}
*/
	
#endif
}
