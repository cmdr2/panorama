using UnityEngine;
using System.Collections;
using System.IO;

/**
 * Helper class for boilerplate analytics
 * 
 * by cmdr2 <secondary.cmdr2@gmail.com>
 */
[RequireComponent(typeof(GoogleAnalyticsV3))]
public class Analytics : MonoBehaviour {

	/* extern */
	public GoogleAnalyticsV3 gav3;
	public string sessionId = "";
	public string userId = "";
	public int sessionCount = 0;
	public bool firstTimeUser;
	public bool tutorialOneFinished;
	public bool tutorialTwoFinished;
	public int viewCount = 0;


	/* globals */
	private const float HEALTH_CHECK_INTERVAL = 5; // seconds
	private string PLAYER_FILE;
	private string TUTORIAL_ONE_FILE;
	private string TUTORIAL_TWO_FILE;
	private string VIEW_COUNT_FILE;
	
	
	/* scratchpad */
	private float nextHealthCheckTime = -1; // seconds
	private bool ready;


	void Start () {

	}

	public void Init () {
		sessionId = guid();

		try {
			PLAYER_FILE = Application.persistentDataPath + Path.DirectorySeparatorChar + "User";
			TUTORIAL_ONE_FILE = Application.persistentDataPath + Path.DirectorySeparatorChar + "Tut1";
			TUTORIAL_TWO_FILE = Application.persistentDataPath + Path.DirectorySeparatorChar + "Tut2";
			VIEW_COUNT_FILE = Application.persistentDataPath + Path.DirectorySeparatorChar + "VC";

			gav3 = GetComponent<GoogleAnalyticsV3> ();

			// check and register repeat user
			if (File.Exists(PLAYER_FILE)) {
				var sr = new StreamReader(PLAYER_FILE);
				userId = sr.ReadLine();
				if (userId != null && !userId.Trim().Equals("")) {
					sessionCount = int.Parse(sr.ReadLine());
				}
				sessionCount++;
				sr.Close();

				var sw = new StreamWriter(PLAYER_FILE);
				sw.Write(userId + "\n" + sessionCount);
				sw.Close();

				LogEvent("Application", "ReturnUser");
				LogEvent("Application", "SessionCount:" + sessionCount);
			} else {
				firstTimeUser = true;
			}
			
			// else assign userId (even if file is corrupted)
			if (userId == null || userId.Trim().Equals("")) {
				userId = "U" + guid();
				sessionCount = 1;
				var sw = new StreamWriter(PLAYER_FILE);
				sw.Write(userId + "\n" + sessionCount);
				sw.Close();
				firstTimeUser = true;
			}

			tutorialOneFinished = File.Exists (TUTORIAL_ONE_FILE);
			tutorialTwoFinished = File.Exists (TUTORIAL_TWO_FILE);

			if (File.Exists (VIEW_COUNT_FILE)) {
				var sr = new StreamReader(VIEW_COUNT_FILE);
				viewCount = int.Parse(sr.ReadLine());
				sr.Close();
			}
			print ("ana " + viewCount);
		} catch (System.Exception e) {
			LogException(e.Message, true);
		}
			
		// register session with timestamp
		System.TimeSpan t = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
		double secondsSinceEpoch = t.TotalSeconds;
		if (gav3) {
			gav3.LogEvent( new EventHitBuilder().SetEventCategory("UserSessions").SetEventAction(userId).SetEventValue(1) );
			gav3.LogEvent( new EventHitBuilder().SetEventCategory("Sessions").SetEventAction(secondsSinceEpoch + ":" + gav3.bundleVersion + ":" + userId + ":" + sessionId) );
		}
		LogEvent("Application", "Platform-" + Application.platform);

		ready = true;
	}
	
	
	void Update () {
		if (ready && Time.time >= nextHealthCheckTime) {
			LogEvent("Application", "HealthCheckPing");
			nextHealthCheckTime = Time.time + HEALTH_CHECK_INTERVAL;
		}
	}


	public string s4() {
		return Mathf.Floor((1 + Random.value) * 0x10000)
			.ToString()
				.Substring(1);
	}
	
	public string guid() {
		return s4() + s4() + '-' + s4() + '-' + s4();
	}
	
	public void LogScreen(string screenName) {
		if (gav3) {
			gav3.LogScreen(screenName + "");
			gav3.LogScreen(screenName + ":" + sessionId);
		}
	}
	
	public void LogEvent(string category, string action) {
		if (gav3) {
			gav3.LogEvent( new EventHitBuilder().SetEventCategory(category).SetEventAction(action) );
			gav3.LogEvent( new EventHitBuilder().SetEventCategory(category + ":" + sessionId).SetEventAction(action) );
		}
	}
	
	public void LogException(string message, bool fatal) {
		if (gav3) {
			gav3.LogException(message, fatal);
			gav3.LogException(message + ":" + sessionId, fatal);
		}
	}
	
	public void LogTutorialOneDone() {
		var sw = new StreamWriter(TUTORIAL_ONE_FILE);
		sw.Write(" ");
		sw.Close();
		tutorialOneFinished = true;
		LogEvent ("Tutorial", "OneDone");
	}
	
	public void LogTutorialTwoDone() {
		var sw = new StreamWriter(TUTORIAL_TWO_FILE);
		sw.Write(" ");
		sw.Close();
		tutorialTwoFinished = true;
		LogEvent ("Tutorial", "TwoDone");
	}

	public void LogViewCount() {
		var sw = new StreamWriter(VIEW_COUNT_FILE);
		sw.Write(++viewCount);
		sw.Close();
	}
}
