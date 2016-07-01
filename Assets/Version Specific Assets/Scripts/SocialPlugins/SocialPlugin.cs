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
using System;
using System.Collections.Generic;
using System.Collections;



/// <summary> Abstract representation of social plugin. </summary>
// Each social plugin should follow singleton pattern and should be accessed through "SocialPluginClass.Instance"
// where SocialPluginClass is name of the plugin.

// Example code which provides this access:
//-------------------------------------------------
//	private static SocialPluginClass m_Instance;
//	public static SocialPluginClass Instance
//	{
//		get{
//			if (m_Instance == null) {
//				GameObject go = new GameObject("SocialPluginClass");
//				m_Instance = go.AddComponent<SocialPluginClass>();
//				GameObject.DontDestroyOnLoad(m_Instance);
//			}
//		return m_Instance;
//		}
//	}

public abstract class SocialPlugin : MonoBehaviour
{
	/*****  ENUMS  ********************************************************************************/
	/// <summary> State of the finished initialization process. </summary>
	public enum State
	{
		/// <summary> Action had sucessfully finished. </summary>
		SUCCESS,
		/// <summary> Action had failed. </summary>
		FAILED,
		/// <summary> Method is not supported by plugin. </summary>
		NOT_SUPPORTED,
		/// <summary> Pin is required in login process. </summary>
		REQUIRE_PIN,
		/// <summary> Message is too long. </summary>
		MESSAGE_TOO_LONG,
		/// <summary> User cancelled the action. </summary>
		USER_CANCELED
	}

	/*****  CLASSES  ******************************************************************************/

	/// <summary> Describes person in any social plugin. </summary>
	public class Person
	{
		public enum GenderEnum
		{
			MALE, 
			FEMALE,
			NEUTER
		}
		
		public class AgeRange
		{
			public int Min = 0;
			public int Max = int.MaxValue;
			
			public AgeRange(int min = 0, int max = int.MaxValue)
			{
				this.Min = min;
				this.Max = max;
			}
			
			public override string ToString ()
			{
				return string.Format("[SocialPlugin.Person.AgeRange: Min={0}, Max={1}]", Min, Max);
			}
		}

		private string m_Name = null;
		private string m_UniqueIdentifier = null;
		private string m_ProfileImageUrl = null;
		private GenderEnum m_Gender = GenderEnum.NEUTER;
		private AgeRange m_Age = new AgeRange(0, int.MaxValue);
		private bool m_HasInstalledTheApp = false;

		/// <summary> Creates new person.</summary>
		/// <param name="displayName">Name of the person that is displayed to the user. Does not have to be unique or persistent.</param>
		/// <param name="uniqueIdentifier">Unique persistent identifier of the person. </param>
		public Person(string inUniqueIdentifier, string inDisplayName = null, GenderEnum gender = GenderEnum.NEUTER,
					  AgeRange age = null, string inProfileImageUrl = null)
		{
			this.m_Name = inDisplayName;
			this.m_UniqueIdentifier = inUniqueIdentifier;
			this.m_Gender = gender;
			this.m_Age = age;
			this.m_ProfileImageUrl = inProfileImageUrl;
		}

		/// <summary> Get unique persistent identifier of the person. </summary>	
		public string ID
		{
			get {return m_UniqueIdentifier;}
		}
		
		/// <summary> Get name of the person that is displayed to the user. Does not have to be unique or persistent.</summary>
		public string Name
		{
			get {return m_Name;}
			set {m_Name = value;}
		}
		
		public AgeRange Age
		{
			get {return m_Age;}
			set {m_Age = value;}
		}
		
		public GenderEnum Gender
		{
			get {return m_Gender;}
			set {m_Gender = value;}
		}
		
		/// <summary> Get URL to users profile image. </summary>
		public string ProfileImageUrl
		{
			get {return m_ProfileImageUrl;}
			set {m_ProfileImageUrl = value;}
		}
		
		/// <summary> States whether the person has installed (been using, authorized) the application. </summary>
		public bool HasInstalledTheApp
		{
			get {return m_HasInstalledTheApp;}
			set {m_HasInstalledTheApp = value;}			
		}
		
		public override string ToString ()
		{
			return string.Format ("[SocialPlugin.Person: ID={0}, Name={1}, Age={2}, Gender={3}, Installed={4}, ProfileImageUrl={5}]", 
								  m_UniqueIdentifier, m_Name, m_Age, m_Gender, m_HasInstalledTheApp, m_ProfileImageUrl);
		}
	}

	public class FriendLoader
	{
		private int m_Position;

		int Position { set { m_Position = value; } get { return m_Position; } }

		//public virtual IEnumerator getFriends(int count){}
	}



	/*****  MEMBERS  ******************************************************************************/

	/// <summary> Currently logged in user. </summary>		
	protected Person m_CurrentUser = null;
	protected State m_LastPluginState;
	protected string m_LastPluginError = "";
	
	/*****  PUBLIC METHODS - GENERAL  *************************************************************/

	/// <summary> Get last state of the plugin. </summary>
	public State PluginState
	{
		get {return m_LastPluginState;}
	}

	/// <summary> Get last status/error message of the plugin. </summary>
	public string LastError
	{
		get {return m_LastPluginError;}
	}

	/// <summary> Get currently logged in user or null. </summary>
	public virtual Person CurrentUser
	{
		get {return m_CurrentUser;}
	}
	
	public virtual Person[] Friends
	{
		get {return null;}	
	}

	/*****  PUBLIC METHODS - CALLBACK INTERFACE  **************************************************/

	/// <summary> Initializes social plugin. </summary>
	/// <param name="initFinishedEvent"> Action called when initialization is complete. </param>
	public virtual void Init(Action<State, string> initFinishedEvent)
	{
	}

	/// <summary> Log user in, this method is used when user logs in through third party method like OAuth.</summary>
	/// <param name="loginEvent"> Action called when login is complete or when pin is required. </param>
	public abstract void Login(Action<State, string> loginEvent);

	/// <summary> Method used to enter pin in case that it is required by plugin. </summary>
	/// <param name="inPin"> Pin that user had acquired wrom internet browser etc.</param>
	/// <param name="loginEvent">Action called when login is complete.</param>
	public virtual void EnterPin(string inPin, Action<State, string> loginEvent)
	{
	}

	/// <summary> Logout user.</summary>
	/// <param name="logoutEvent">Action called when logout is complete with stateLogin.SUCCESS or stateLogin.FAILED.</param>
	public abstract void Logout(Action<State, string> logoutEvent);

	/// <summary> Send message or status update from this plugin. </summary>
	/// <param name="inMessage"> Message to send. </param>
	/// <param name="sendMessageEvent"> Action called when message is sent or on error. </param>
	public abstract void PostStatus(string inMessage, Action<State, string> sendMessageEvent);

	/// <summary> Send message or status update with image. </summary>
	/// <param name="inMessage"> Message to send. </param>
	/// <param name="sendImageEvent"> Action called when message is sent or on error. </param>
	public abstract void PostImage(string inMessage, byte[] image, Action<State, string> sendImageEvent);

	/*****  PUBLIC METHODS - ENUMERATOR INTERFACE  ************************************************/

	/// <summary> Initializes social plugin.</summary>
	public virtual IEnumerator Init()
	{
		yield return 0;
	}

	/// <summary> Log user in, this method is used when user logs in through third party method like OAuth.</summary>
	public abstract IEnumerator Login();

	/// <summary> Method used to enter pin in case that it is required by plugin. </summary>
	public virtual IEnumerator EnterPin(string inPin)
	{
		yield return 0;
	}

	/// <summary> Logout user.</summary>
	public abstract IEnumerator Logout();

	/// <summary> Send message or status update from this plugin. </summary>
	public abstract IEnumerator PostStatus(string inMessage);

	/// <summary> Send message or status update with image. </summary>
	public abstract IEnumerator PostImage(string inMessage, byte[] image);
	
	/// <summary> Load user's friends. </summary>
	public abstract void LoadFriends(Action<State, string, Person[]> loadFriendsEvent);
	
	/// <summary> Load user's friends. </summary>
	public abstract IEnumerator LoadFriends();
}
