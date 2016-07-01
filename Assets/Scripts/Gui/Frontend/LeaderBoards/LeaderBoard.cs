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
using System.Linq;
using LitJson;

// =====================================================================================================================
// =====================================================================================================================
public class QueryLeaderBoardRank : DefaultCloudAction
{
	public string leaderBoardName { get; private set; }
	public string[] users { get; private set; }

	public int[] retRanks { get; private set; }

	public QueryLeaderBoardRank(UnigueUserID inUserID, string inLeaderBoardName, string inPrimaryKey, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		users = new string[] {inPrimaryKey};
		leaderBoardName = inLeaderBoardName;
	}

	public QueryLeaderBoardRank(UnigueUserID inUserID, string inLeaderBoardName, string[] inPrimaryKeys, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		users = inPrimaryKeys;
		leaderBoardName = inLeaderBoardName;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().LeaderboardGetRanks(userID.primaryKey, userID.productID, leaderBoardName, userID.passwordHash, users);
	}

	protected override void OnSuccess()
	{
		try
		{
			retRanks = JsonMapper.ToObject<int[]>(result);
		}
		catch
		{
			SetStatus(BaseCloudAction.E_Status.Failed);
			failInfo = "Result of LeaderBoard get ranks is invalid";
		}
	}
}

// =====================================================================================================================
public class QueryLeaderBoardRankAndScores : DefaultCloudAction
{
	public struct UserRecord
	{
		public int rank;
		public int score;
		public string userName;
		public string displayName;
		public int experience;
	}

	public string leaderBoardName { get; private set; }
	public string[] users { get; private set; }

	public UserRecord[] retRecords { get; private set; }

	public QueryLeaderBoardRankAndScores(UnigueUserID inUserID, string inLeaderBoardName, string inPrimaryKey, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		users = new string[] {inPrimaryKey};
		leaderBoardName = inLeaderBoardName;
	}

	public QueryLeaderBoardRankAndScores(UnigueUserID inUserID, string inLeaderBoardName, string[] inPrimaryKeys, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		users = inPrimaryKeys;
		leaderBoardName = inLeaderBoardName;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance()
							.LeaderboardGetRanksAndScores(userID.primaryKey, userID.productID, leaderBoardName, userID.passwordHash, users);
	}

	protected override void OnSuccess()
	{
		try
		{
			//Debug.Log(result);
			retRecords = JsonMapper.ToObject<UserRecord[]>(result);
			//Debug.Log(retRecords);
		}
		catch
		{
			SetStatus(BaseCloudAction.E_Status.Failed);
			failInfo = "Result of Leaderboard Record Query is invalid";
		}
	}
}

// =====================================================================================================================
public class QueryLeaderBoardScore : DefaultCloudAction
{
	public struct ScoreInfo
	{
		public int score;
		public string userName;
		public string displayName;
		public int experience;
	}

	public string leaderBoardName { get; private set; }
	public int startRankIndex { get; private set; }
	public int count { get; private set; }

	public ScoreInfo[] retScores { get; private set; }

	public QueryLeaderBoardScore(UnigueUserID inUserID,
								 string inLeaderBoardName,
								 int inStartIndex,
								 int inCount = 1,
								 float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		startRankIndex = inStartIndex;
		leaderBoardName = inLeaderBoardName;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance()
							.LeaderboardQuery(userID.primaryKey, userID.productID, leaderBoardName, userID.passwordHash, startRankIndex);
	}

	protected override void OnSuccess()
	{
		try
		{
			//Debug.Log(result);
			retScores = JsonMapper.ToObject<ScoreInfo[]>(result);
			//Debug.Log(retScores);
		}
		catch
		{
			SetStatus(BaseCloudAction.E_Status.Failed);
			failInfo = "Result of Leaderboard Query is invalid";
		}
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class LeaderBoard
{
	// .................................................................................................................
	protected const int TEXT_FETCHING = 2040614; //    Fetching leaderboard...
	protected const int TEXT_ERROR = 2040615; //    Can't retrive data from leadeboard!

	// .................................................................................................................
	public delegate void FetchFinished();

	// .................................................................................................................
	public class Row
	{
		public int Order { private set; get; }
		public string PrimaryKey { private set; get; }
		public string DisplayName { private set; get; }
		public int Score { private set; get; }
		public int Experience { private set; get; }
		public bool LocalUser { private set; get; }

		public Row(int inOrder, string inPrimaryKey, string inDisplayName, int inScore, int inExperience, bool inLocalUser)
		{
			Order = inOrder;
			PrimaryKey = inPrimaryKey;
			DisplayName = inDisplayName;
			Score = inScore;
			Experience = inExperience;
			LocalUser = inLocalUser;
		}

		public Row(int inMessage)
		{
			Order = -1;
			PrimaryKey = TextDatabase.instance[inMessage];
			Score = -1;
			LocalUser = false;
		}
	}

	// .................................................................................................................
	public string leaderBoardName { private set; get; }
	public int maxRows { private set; get; }

	// .................................................................................................................
	protected List<Row> m_Rows = null;

	// -----------------------------------------------------------------------------------------------------------------
	protected const int SKIP_UPDATE_TIMEOUT = 1; // in minutes...
	protected System.DateTime m_LastSyncTime;

	// =================================================================================================================
	// === C# special functions ========================================================================================
	public LeaderBoard(string inLeaderBoardName, int inMaxRows)
	{
		leaderBoardName = inLeaderBoardName;
		maxRows = inMaxRows;

		m_Rows = new List<Row>(inMaxRows);

		CloudUser.authenticationChanged += OnUserAuthenticationChanged;
	}

	~LeaderBoard()
	{
		CloudUser.authenticationChanged -= OnUserAuthenticationChanged;
	}

	public Row this[uint inIndex]
	{
		get { return inIndex < m_Rows.Count ? m_Rows[(int)inIndex] : null; }
	}

	public int RowCount()
	{
		if (m_Rows == null)
			return 0;
		return m_Rows.Count;
	}

	// =================================================================================================================
	// === public interface ============================================================================================
	public virtual void FetchAndUpdate(string inPrimaryKey, MonoBehaviour inCorutineOwner, FetchFinished inFinishDelegate)
	{
		// Fet score info from cloud.
		// ...................................................................................

		if (Mathf.Abs((float)(m_LastSyncTime - CloudDateTime.UtcNow).TotalMinutes) < SKIP_UPDATE_TIMEOUT)
			return; // don't update mailbox from cloud

		m_LastSyncTime = CloudDateTime.UtcNow;

		m_Rows = new List<Row>();
		m_Rows.Add(new Row(TEXT_FETCHING));

		inCorutineOwner.StartCoroutine(FetchLeaderBoard_Corutine(leaderBoardName, inPrimaryKey, maxRows, inFinishDelegate));
	}

	// =================================================================================================================
	// === internal ====================================================================================================
	IEnumerator FetchLeaderBoard_Corutine(string inLeaderBoardName, string inPrimaryKey, int inMaxRecords, FetchFinished inFinishDelegate)
	{
		// show first 3 users, then two before, local user, two after. In total 8 people...
		// ...................................................................................

		bool leaderboardError = false;
		int localUserRank = -1;
		{
			string[] names = new string[] {inPrimaryKey};
			QueryLeaderBoardRank action = new QueryLeaderBoardRank(CloudUser.instance.authenticatedUserID, inLeaderBoardName, names);
			GameCloudManager.AddAction(action);

			// wait for authentication...
			while (action.isDone == false)
				yield return new WaitForSeconds(0.2f);

			if (action.isFailed == true)
			{
				Debug.LogWarning("Can't retrive rank of user: " + inPrimaryKey);
				leaderboardError |= true;
			}
			else
			{
				/*for(int i = 0; i < action.retRanks.Length; i++)
				{
					Debug.Log(" Name: " + names[i] + " Score: " + action.retRanks[i]);
				}*/

				localUserRank = action.retRanks[0];
				//Debug.Log("Rank: " + localUserRank);
			}
		}

		List<Row> rows = new List<Row>();

		if (leaderboardError != true)
		{
			QueryLeaderBoardScore action = new QueryLeaderBoardScore(CloudUser.instance.authenticatedUserID, inLeaderBoardName, 0);
			GameCloudManager.AddAction(action);

			// wait for authentication...
			while (action.isDone == false)
				yield return new WaitForSeconds(0.2f);

			if (action.isFailed == true)
			{
				Debug.LogWarning("Can't retrive score info from leaderboard: " + inLeaderBoardName);
				leaderboardError |= true;
			}
			else
			{
				for (int i = 0; i < action.retScores.Length && i < inMaxRecords; i++)
				{
					QueryLeaderBoardScore.ScoreInfo score = action.retScores[i];

					//Debug.Log("ID: " + (i+1) + " Name: " + action.retScores[i].userName + " Score: " + action.retScores[i].score);
					rows.Add(new Row(i + 1, score.userName, score.displayName, score.score, score.experience, score.userName == inPrimaryKey));
				}
			}
		}

		if (leaderboardError != true && localUserRank > 5)
		{
			//static
			int cfg_startOffset = 4;

			// wa want users which are near, so we set
			// start index little smaller...
			int startRankIndex = localUserRank - cfg_startOffset;

			// request for leaderbord records...
			QueryLeaderBoardScore action = new QueryLeaderBoardScore(CloudUser.instance.authenticatedUserID, inLeaderBoardName, startRankIndex);
			GameCloudManager.AddAction(action);

			// wait for finish...
			while (action.isDone == false)
				yield return new WaitForSeconds(0.2f);

			// processing result...
			if (action.isFailed == true)
			{
				Debug.LogWarning("Can't retrive score info from leaderboard: " + inLeaderBoardName);
				// leaderboardError |= true; ALEX::  we ignore this error. At least first users will be shown...
			}
			else
			{
				int startScoreIndex = Mathf.Clamp(((action.retScores.Length - 1) - cfg_startOffset), 0, 2);

				for (int rowIndex = 3, userIndex = startScoreIndex;
					 rowIndex < inMaxRecords && userIndex < action.retScores.Length;
					 rowIndex++, userIndex++)
				{
					//Debug.Log("ID: " + (i) + " Name: " + action.retScores[i].userName + " Score: " + action.retScores[i].score);

					QueryLeaderBoardScore.ScoreInfo scoreInfo = action.retScores[userIndex];

					int userOrder = startRankIndex + userIndex + 1;

					// Create leaderbord row with user score info
					Row userRow = new Row(userOrder,
										  scoreInfo.userName,
										  scoreInfo.displayName,
										  scoreInfo.score,
										  scoreInfo.experience,
										  scoreInfo.userName == inPrimaryKey);

					// set user row into leaderboard rows...
					if (rowIndex < rows.Count)
					{
						rows[rowIndex] = userRow;
					}
					else
					{
						rows.Add(userRow);
					}
				}
			}
		}

		if (leaderboardError == true)
		{
			m_Rows = new List<Row>();
			m_Rows.Add(new Row(TEXT_ERROR));
		}
		else
		{
			m_Rows = rows;
		}

		if (inFinishDelegate != null)
		{
			inFinishDelegate();
		}
	}

	void OnUserAuthenticationChanged(bool state)
	{
		if (state == false)
			return;

		m_LastSyncTime = new System.DateTime(0);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class LeaderBoardFriends : LeaderBoard
{
	// =================================================================================================================
	// === C# special functions ========================================================================================
	public LeaderBoardFriends(string inLeaderBoardName, int inMaxRows)
					: base(inLeaderBoardName, inMaxRows)
	{
	}

	// =================================================================================================================
	// === public interface ============================================================================================
	public override void FetchAndUpdate(string inLocalUserName, MonoBehaviour inCorutineOwner, FetchFinished inFinishDelegate)
	{
		// Fetch score info from cloud.
		// ...................................................................................

		if (Mathf.Abs((float)(m_LastSyncTime - CloudDateTime.UtcNow).TotalMinutes) < SKIP_UPDATE_TIMEOUT)
			return; // don't update mailbox from cloud

		m_LastSyncTime = CloudDateTime.UtcNow;

		m_Rows = new List<Row>();
		m_Rows.Add(new Row(TEXT_FETCHING));

		inCorutineOwner.StartCoroutine(FetchFriendsLeaderBoard_Corutine(leaderBoardName, inLocalUserName, maxRows, inFinishDelegate));
		// debug...
		//inCorutineOwner.StartCoroutine( FetchFriendsLeaderBoard_Corutine("Default", "xxx", 8, inFinishDelegate) );
	}

	// =================================================================================================================
	// === internal ====================================================================================================
	IEnumerator FetchFriendsLeaderBoard_Corutine(string inLeaderBoardName,
												 string inLocalPrimaryKey,
												 int inMaxRecords,
												 FetchFinished inFinishDelegate)
	{
		// show first 3 users, then two before, local user, two after. In total 8 people...
		// ...................................................................................

		bool leaderboardError = false;

		List<Row> rows = new List<Row>();

		{
			// Get friends from friend list...
			string[] names = new string[GameCloudManager.friendList.friends.Count + 1];
			names[0] = inLocalPrimaryKey;
			for (int index = 0; index < GameCloudManager.friendList.friends.Count; index++)
				names[index + 1] = GameCloudManager.friendList.friends[index].PrimaryKey;

			// debug...
			//string[] names = new string[] {inLocalUserName, "User_2141855", "User_1139861", "User_780626", "User_1232916", "Vykuk", "alexdebug", "janko-hrasko", "xxx", "yyy", "01", "02"};
			//string[] names = new string[] {inLocalUserName, "User_2141855", "User_1139861", "User_780626", "User_1232916", "Vykuk", "alex", "janko-hrasko", "xxx", "yyy", "01", "02"};
			//names = names.Distinct().ToArray();

			QueryLeaderBoardRankAndScores action = new QueryLeaderBoardRankAndScores(CloudUser.instance.authenticatedUserID, inLeaderBoardName, names);
			GameCloudManager.AddAction(action);

			// wait for authentication...
			while (action.isDone == false)
				yield return new WaitForSeconds(0.2f);

			if (action.isFailed == true)
			{
				Debug.LogWarning("Can't retrive friends record: " + inLocalPrimaryKey);
				leaderboardError |= true;
			}
			else
			{
				for (int i = 0; i < action.retRecords.Length; i++)
				{
					QueryLeaderBoardRankAndScores.UserRecord record = action.retRecords[i];

					int userOrder = record.rank < 0 ? record.rank : record.rank + 1;
					//Debug.Log(" Name: " + names[i] + " Score: " + action.retRanks[i]);
					rows.Add(new Row(userOrder, record.userName, record.displayName, record.score, record.experience, record.userName == inLocalPrimaryKey));
				}
			}
		}

		if (leaderboardError == true)
		{
			m_Rows = new List<Row>();
			m_Rows.Add(new Row(TEXT_ERROR));
		}
		else
		{
			// sort by rank...
			rows.Sort((x, y) =>
					  {
						  if (x == y)
						  {
							  return 0;
						  }
						  else if (x.Order < 0 && y.Order < 0)
						  {
							  if (x.PrimaryKey == inLocalPrimaryKey)
								  return -1;
							  if (y.PrimaryKey == inLocalPrimaryKey)
								  return 1;

							  return 1;
						  }
						  else
						  {
							  return ((x.Order < 0) ? 1 : (y.Order < 0) ? -1 : x.Order.CompareTo(y.Order));
						  }
					  });

			int localUserIndex = rows.FindIndex(x => x.PrimaryKey == inLocalPrimaryKey);
			if (localUserIndex >= 5)
			{
				// check if there are at least two other friend behind me...
				int rest = Mathf.Clamp((rows.Count - 1) - localUserIndex, 0, 2);
				int userViewIndex = localUserIndex - (4 - rest);

				m_Rows = rows.GetRange(0, 3);
				m_Rows.AddRange(rows.GetRange(userViewIndex, 5));
			}
			else
			{
				// nothing...
				m_Rows = rows;
			}
		}

		if (inFinishDelegate != null)
		{
			inFinishDelegate();
		}
	}
}

public class LeaderBoardFacebook : LeaderBoard
{
	// =================================================================================================================
	// === C# special functions ========================================================================================
	public LeaderBoardFacebook(string inLeaderBoardName, int inMaxRows)
					: base(inLeaderBoardName, inMaxRows)
	{
	}

	// =================================================================================================================
	// === public interface ============================================================================================
	public override void FetchAndUpdate(string inLocalPrimaryKey, MonoBehaviour inCorutineOwner, FetchFinished inFinishDelegate)
	{
		if (Mathf.Abs((float)(m_LastSyncTime - CloudDateTime.UtcNow).TotalMinutes) < SKIP_UPDATE_TIMEOUT)
			return; // don't update mailbox from cloud

		m_LastSyncTime = CloudDateTime.UtcNow;

		m_Rows = new List<Row>();
		m_Rows.Add(new Row(TEXT_FETCHING));

		inCorutineOwner.StartCoroutine(FetchFacebookFriendsLeaderBoard_Corutine(leaderBoardName,
																				inCorutineOwner,
																				inLocalPrimaryKey,
																				maxRows,
																				inFinishDelegate));
	}

	IEnumerator FetchFacebookFriendsLeaderBoard_Corutine(string inLeaderBoardName,
														 MonoBehaviour inCorutineOwner,
														 string inLocalPrimaryKey,
														 int inMaxRecords,
														 FetchFinished inFinishDelegate)
	{
		yield return inCorutineOwner.StartCoroutine(GameCloudManager.facebookFriendList.WaitForLoading());

		bool leaderboardError = false;

		List<Row> rows = new List<Row>();
		{
			List<string> names = new List<string>();
			names.Add(inLocalPrimaryKey);

			if (GameCloudManager.facebookFriendList.Friends != null)
			{
				foreach (FacebookFriendList.FacebookFriend friend in GameCloudManager.facebookFriendList.Friends)
				{
					foreach (string primaryKey in friend.PrimaryKeys)
					{
						if (inLocalPrimaryKey != primaryKey)
							names.Add(primaryKey);
					}
				}
			}

			QueryLeaderBoardRankAndScores action = new QueryLeaderBoardRankAndScores(CloudUser.instance.authenticatedUserID,
																					 inLeaderBoardName,
																					 names.ToArray());
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForSeconds(0.2f);

			if (action.isFailed == true)
			{
				Debug.LogWarning("Can't retrive friends record: " + inLocalPrimaryKey);
				leaderboardError |= true;
			}
			else
			{
				for (int i = 0; i < action.retRecords.Length; i++)
				{
					QueryLeaderBoardRankAndScores.UserRecord record = action.retRecords[i];

					int userOrder = record.rank < 0 ? record.rank : record.rank + 1;
					rows.Add(new Row(userOrder, record.userName, record.displayName, record.score, record.experience, record.userName == inLocalPrimaryKey));
				}
			}
		}

		if (leaderboardError == true)
		{
			m_Rows = new List<Row>();
			m_Rows.Add(new Row(TEXT_ERROR));
		}
		else
		{
			// sort by rank...
			rows.Sort((x, y) =>
					  {
						  if (x == y)
						  {
							  return 0;
						  }
						  else if (x.Order < 0 && y.Order < 0)
						  {
							  if (x.PrimaryKey == inLocalPrimaryKey)
								  return -1;
							  if (y.PrimaryKey == inLocalPrimaryKey)
								  return 1;

							  return 1;
						  }
						  else
						  {
							  return ((x.Order < 0) ? 1 : (y.Order < 0) ? -1 : x.Order.CompareTo(y.Order));
						  }
					  });

			int localUserIndex = rows.FindIndex(x => x.PrimaryKey == inLocalPrimaryKey);

			if (rows.Count <= maxRows)
				m_Rows = rows;
			else
			{
				if (localUserIndex < maxRows)
					m_Rows = rows.GetRange(0, maxRows);
				else //local user didn't get into the list
				{
					m_Rows = rows.GetRange(0, maxRows - 1);
					m_Rows.Add(rows[localUserIndex]);
				}
			}
		}

		if (inFinishDelegate != null)
		{
			inFinishDelegate();
		}
	}
}
