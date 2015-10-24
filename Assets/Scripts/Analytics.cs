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
	public string preProdTrackingCode;

	[HideInInspector]
	public GoogleAnalyticsV3 gav3;
	[HideInInspector]
	public string sessionId = "";
	[HideInInspector]
	public string userId = "";
	[HideInInspector]
	public int sessionCount = 0;
	[HideInInspector]
	public bool firstTimeUser;
	[HideInInspector]
	public bool tutorialOneFinished;
	[HideInInspector]
	public bool tutorialOneRepeatFinished;
	[HideInInspector]
	public bool tutorialTwoFinished;
	[HideInInspector]
	public bool feedbackFinished;
	[HideInInspector]
	public int monoViewCount = 0;
	[HideInInspector]
	public int stereoViewCount = 0;
	[HideInInspector]
	public int stereoImgViewCount = 0;


	/* globals */
	private const float HEALTH_CHECK_INTERVAL = 5; // seconds
	private string PLAYER_FILE;
	private string TUTORIAL_ONE_FILE;
	private string TUTORIAL_ONE_REPEAT_FILE;
	private string TUTORIAL_TWO_FILE;
	private string FEEDBACK_FILE;
	private string VIEW_COUNT_FILE;
	
	
	/* scratchpad */
	private float nextHealthCheckTime = -1; // seconds
	private bool ready;


	void Start () {

	}

	public void Init () {
		sessionId = guid();

		PLAYER_FILE = Application.persistentDataPath + Path.DirectorySeparatorChar + "User";
		TUTORIAL_ONE_FILE = Application.persistentDataPath + Path.DirectorySeparatorChar + "Tut1";
		TUTORIAL_ONE_REPEAT_FILE = Application.persistentDataPath + Path.DirectorySeparatorChar + "Tut1Repeat";
		TUTORIAL_TWO_FILE = Application.persistentDataPath + Path.DirectorySeparatorChar + "Tut2";
		FEEDBACK_FILE = Application.persistentDataPath + Path.DirectorySeparatorChar + "Fdbk";
		VIEW_COUNT_FILE = Application.persistentDataPath + Path.DirectorySeparatorChar + "VC";

		try {
			gav3 = GetComponent<GoogleAnalyticsV3> ();

			if (Application.isEditor) {
				gav3.androidTrackingCode = gav3.otherTrackingCode = preProdTrackingCode;
			}
		} catch (System.Exception e) {
			LogException("Error setting up gav3", false);
		}

		// check and register repeat user
		if (File.Exists(PLAYER_FILE)) {
			try {
				var sr = new StreamReader(PLAYER_FILE);
				userId = sr.ReadLine();
				if (userId != null && !userId.Trim().Equals("")) {
					sessionCount = int.Parse(sr.ReadLine());
				}
				sessionCount++;
				sr.Close();
			} catch (System.Exception e) {
				LogException("Error reading player file", false);
			}

			try {
				var sw = new StreamWriter(PLAYER_FILE);
				sw.Write(userId + "\n" + sessionCount);
				sw.Close();
			} catch (System.Exception e) {
				LogException("Error writing player file for new user", false);
			}

			LogEvent("Application", "ReturnUser");
//			LogEvent("Application", "SessionCount:" + sessionCount);
			gav3.LogEvent( new EventHitBuilder().SetEventCategory("Application:"+sessionId).SetEventAction("SessionCount:" + sessionCount) );
		} else {
			firstTimeUser = true;
		}
			
		// else assign userId (even if file is corrupted)
		if (userId == null || userId.Trim().Equals("")) {
			userId = "U" + guid();
			sessionCount = 1;

			try {
				var sw = new StreamWriter(PLAYER_FILE);
				sw.Write(userId + "\n" + sessionCount);
				sw.Close();
			} catch (System.Exception e) {
				LogException("Error writing player file for new/null user", false);
			}
			firstTimeUser = true;
		}

		tutorialOneFinished = File.Exists (TUTORIAL_ONE_FILE);
		tutorialOneRepeatFinished = File.Exists (TUTORIAL_ONE_REPEAT_FILE);
		tutorialTwoFinished = File.Exists (TUTORIAL_TWO_FILE);
		feedbackFinished = File.Exists (FEEDBACK_FILE);

		if (File.Exists (VIEW_COUNT_FILE)) {
			try {
				var sr = new StreamReader(VIEW_COUNT_FILE);
				monoViewCount = int.Parse(sr.ReadLine());
				string svc = sr.ReadLine();
				stereoViewCount = (svc != null && !svc.Trim().Equals("") ? int.Parse(svc) : 0);
				string sivc = sr.ReadLine();
				stereoImgViewCount = (sivc != null && !sivc.Trim().Equals("") ? int.Parse(sivc) : 0);
				sr.Close();
			} catch (System.Exception e) {
				LogException("Error reading view count file", false);
			}
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
//			gav3.LogScreen(screenName + "");
//			gav3.LogScreen(screenName + ":" + sessionId);
		}
	}
	
	public void LogEvent(string category, string action) {
		if (gav3) {
//			gav3.LogEvent( new EventHitBuilder().SetEventCategory(category).SetEventAction(action) );
//			gav3.LogEvent( new EventHitBuilder().SetEventCategory(category + ":" + sessionId).SetEventAction(action) );
		}
	}

	public void LogTiming(string category, long interval, string name, string label) {
		if (gav3) {
//			gav3.LogTiming(category, interval, name, label);
//			gav3.LogTiming(category + ":" + sessionId, interval, name, label);
		}
	}
	
	public void LogException(string message, bool fatal) {
		if (gav3) {
//			gav3.LogException(message, fatal);
//			gav3.LogException(message + ":" + sessionId, fatal);
		}
	}
	
	public void LogTutorialOneDone() {
		var sw = new StreamWriter(TUTORIAL_ONE_FILE);
		sw.Write(" ");
		sw.Close();
		tutorialOneFinished = true;
		LogEvent ("Tutorial", "OneDone");
	}

	public void LogTutorialOneRepeatDone() {
		var sw = new StreamWriter(TUTORIAL_ONE_REPEAT_FILE);
		sw.Write(" ");
		sw.Close();
		tutorialOneRepeatFinished = true;
		LogEvent ("Tutorial", "OneRepeatDone");
	}
	
	public void LogTutorialTwoDone() {
		var sw = new StreamWriter(TUTORIAL_TWO_FILE);
		sw.Write(" ");
		sw.Close();
		tutorialTwoFinished = true;
		LogEvent ("Tutorial", "TwoDone");
	}
	
	public void LogFeedbackDone() {
		var sw = new StreamWriter(FEEDBACK_FILE);
		sw.Write(" ");
		sw.Close();
		feedbackFinished = true;
		LogEvent ("Tutorial", "FeedbackDone");
	}

	public void LogMonoViewCount() {
		var sw = new StreamWriter(VIEW_COUNT_FILE);
		sw.Write((++monoViewCount) + "\n" + stereoViewCount + "\n" + stereoImgViewCount);
		sw.Close();
	}

	public void LogStereoViewCount() {
		stereoViewCount = (stereoViewCount + 1) % Recommendations.stereoImages.Count;

		var sw = new StreamWriter(VIEW_COUNT_FILE);
		sw.Write(monoViewCount + "\n" + stereoViewCount + "\n" + stereoImgViewCount);
		sw.Close();
	}
	
	public void LogStereoImgViewCount() {
		var sw = new StreamWriter(VIEW_COUNT_FILE);
		sw.Write(monoViewCount + "\n" + stereoViewCount + "\n" + (++stereoImgViewCount));
		sw.Close();
	}
}
