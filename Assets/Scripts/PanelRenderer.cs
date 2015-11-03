using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;


/**
 * Main class for fetching and rendering panoramas
 * 
 * GAH, MY EYES! (THIS CLASS BADLY NEEDS TO BE REFACTORED)
 * 
 * Controls:
 *  - Single 'Ctrl' press = turn around 180 degrees
 *  - Double 'Ctrl' press = load new images
 * 
 * by cmdr2 <secondary.cmdr2@gmail.com>
 */
public class PanelRenderer : MonoBehaviour {

	/* dependencies */
	public GameObject span;
	public GameObject img;
	public GameObject panoramaImg;

	public Material monoPanoramaMat;
	public Material leftSBSPanoramaMat;
	public Material rightSBSPanoramaMat;
	public Material leftOUPanoramaMat;
	public Material rightOUPanoramaMat;
	public Material leftOUInvPanoramaMat;
	public Material rightOUInvPanoramaMat;
	public Material leftCrossImgMat;
	public Material rightCrossImgMat;

	private TextMesh statusMessage;
	private GameObject titleMessage;

	private GameObject monoEyePano;
	private GameObject leftEyePano;
	private GameObject rightEyePano;
	private GameObject leftEyeImg;
	private GameObject rightEyeImg;
	private GameObject imgCaption;
	private GameObject reportImage;
	private GameObject saveFavorite;


	/* globals */
	private const float PX_TO_METERS = 0.065f;
	private const int MAX_ROWS = 3;
	private const float ROTATION_DURATION = 0.2f; // seconds
	private const float ROTATION_SPEED = 180 / ROTATION_DURATION; // degrees per second
	private const float DOUBLE_TRIGGER_INTERVAL = 1.5f; // seconds
	private const float HEALTH_CHECK_INTERVAL = 5; // seconds
	private const float FIRST_IMAGE_LOAD_TIMEOUT_DURATION = 6; // seconds
	private const float IMAGE_VIEWING_TIMEOUT_DURATION = 3; // seconds

	private const string TUTORIAL_ONE_TXT = "<b>Let's try something!</b>\nPush down and leave " +
		"trigger TWICE\nto load new images anytime.\n<i>(or</i> press Fire1 TWICE)";
	private const string TUTORIAL_ONE_REPEAT_TXT = "<b>Double-trigger again</b>\nevery time you want\na new image";
	private const string TUTORIAL_TWO_TXT = "<b>Pro tip, give it a try!</b>\nPush down and leave " +
		"trigger ONCE and <b>WAIT</b>\nto see behind you.\n<i>(or</i> press Fire1 ONCE and <b>WAIT</b>)";

	private const string FEEDBACK_TXT = "I'd love to hear\nyour suggestions";

	private const float TUTORIAL_ONE_REPEAT_DURATION = 4.5f; // seconds
	private const float FEEDBACK_DURATION = 4.5f; // seconds

	private const string IMAGES_URL = "http://www.flickriver.com/groups/equirectangular/pool/random/";
	private const string STEREO_IMAGES_URL = "http://www.flickriver.com/groups/3d-cross-view/pool/random/";


	/* scratchpad */
	private RenderMode renderMode = RenderMode.MONO_PANORAMA;

	private Shader PANORAMA_SHADER;
	private float rotation = 0f;
	private Quaternion targetRotation;
	private AudioSource fetchAudio;
	private float firstImageLoadTimeout = -1; // seconds

	private float lastTriggerTime = -100f; // seconds
	private bool tutorialOneVisible;
	private bool tutorialOneRepeatVisible;
	private bool tutorialTwoVisible;
	private bool feedbackVisible;

	private float tutorialOneRepeatEndTime = -1; // seconds
	private float feedbackEndTime = -1; // seconds
	private Stopwatch ctf;

	private List<object> taskQueue = new List<object>();
	private List<object> tasksToGCNext = new List<object>();
	private List<WWW> networkConns = new List<WWW>();

	private Analytics analytics;
	private float nextHealthCheckTime = -1; // seconds

	private float tiltLockTime = -1; // seconds

	public long currentImageId;


	void Start () {
		PANORAMA_SHADER = Shader.Find ("InsideVisible");
		statusMessage = GameObject.Find ("StatusMessage").GetComponent<TextMesh>();
		monoEyePano = GameObject.Find ("monoEyePano");
		leftEyePano = GameObject.Find ("leftEyePano");
		rightEyePano = GameObject.Find ("rightEyePano");
		leftEyeImg = GameObject.Find ("leftEyeImg");
		rightEyeImg = GameObject.Find ("rightEyeImg");
		imgCaption = GameObject.Find ("imgCaption");
		reportImage = GameObject.Find ("reportImage");
		saveFavorite = GameObject.Find ("saveFavorite");

		titleMessage = GameObject.Find ("TitleMessage");
		fetchAudio = GetComponent<AudioSource> ();
		analytics = GameObject.Find ("Analytics").GetComponent<Analytics>();
		analytics.Init ();

		StartCoroutine (Fetch ());
	}

	void Update () {
		bool triggered = /*Cardboard.SDK.CardboardTriggered || */Input.GetButtonDown ("Fire1");
		if (triggered) {
			if (lastTriggerTime < 0) {
				lastTriggerTime = Time.time;
			} else {
				if (Time.time <= lastTriggerTime + DOUBLE_TRIGGER_INTERVAL) {
					lastTriggerTime = -100f;
					// double trigger stuff
					
					if (tutorialOneVisible) {
						TutorialOneCompleted();
					}


					StartCoroutine (Fetch ());
				}
			}
		}

		if (lastTriggerTime >= 0 && Time.time > lastTriggerTime + DOUBLE_TRIGGER_INTERVAL) {
			lastTriggerTime = -100f;
			// single trigger stuff

			if (renderMode == RenderMode.MONO_PANORAMA || renderMode == RenderMode.STEREO_PANORAMA) {
				rotation += 180f;
				targetRotation = Quaternion.Euler(0, rotation, 0);
			}
			
			if (tutorialTwoVisible) {
				TutorialTwoCompleted();
			}
		}

		if (tutorialOneRepeatVisible && Time.time > tutorialOneRepeatEndTime) {
			tutorialOneRepeatVisible = false;
			statusMessage.text = "";
		}

		if (feedbackVisible && Time.time > feedbackEndTime) {
			feedbackVisible = false;
			statusMessage.text = "";
		}

		if (Cardboard.SDK.Tilted && Time.time > tiltLockTime) {
			tiltLockTime = Time.time + 5;
			switch (renderMode) {
			case RenderMode.MONO_PANORAMA:
				renderMode = RenderMode.STEREO_IMAGE;
				break;
			case RenderMode.STEREO_IMAGE:
				renderMode = RenderMode.STEREO_PANORAMA;
				break;
			case RenderMode.STEREO_PANORAMA:
				renderMode = RenderMode.MONO_PANORAMA;
				break;
			}
			StartCoroutine (Fetch (true));
			Handheld.Vibrate ();
		}
	}

	void FixedUpdate() {
		transform.rotation = Quaternion.RotateTowards (transform.rotation, targetRotation, ROTATION_SPEED * Time.fixedDeltaTime);
	}

	private IEnumerator Fetch(bool force = false) {
		if (!force && Time.time < firstImageLoadTimeout) {
			analytics.LogEvent("Panorama", "ThrottlingFetch");
			yield break;
		}
		
		firstImageLoadTimeout = Time.time + FIRST_IMAGE_LOAD_TIMEOUT_DURATION;

		analytics.gav3.LogEvent ("Panorama:" + analytics.sessionId, "Requested", "foo", 1);

		tasksToGCNext.AddRange (taskQueue);
		taskQueue = new List<object> ();

		networkConns.ForEach(conn => {
			try {
				Destroy(conn.texture);
			} catch (System.Exception e) {
				// commit a crime of swallowing exception
			}
			conn.Dispose();
			conn = null;
		});
		networkConns.Clear ();

//		reportImage.SetActive (false);
//		saveFavorite.SetActive (false);

		switch (renderMode) {
		case RenderMode.MONO_PANORAMA:
			yield return StartCoroutine (FetchMono ());
			break;
		case RenderMode.STEREO_IMAGE:
			yield return StartCoroutine (FetchStereoImg ());
			break;
		case RenderMode.STEREO_PANORAMA:
			yield return StartCoroutine (FetchStereo ());
			break;
		}
	}

	private IEnumerator FetchStereo() {
		analytics.gav3.LogEvent ("Panorama:" + analytics.sessionId, "RequestedStereo", "foo", 1);

		PanoramaImage image = null;
		if (analytics.stereoViewCount >= 0 && analytics.stereoViewCount < Recommendations.stereoImages.Count) {
			image = Recommendations.stereoImages[analytics.stereoViewCount];
		}

		if (image != null) {
			string domain = new System.Uri(image.url[0]).Host;
			statusMessage.text = "Waiting for " + domain + "...";
			DrawPanorama (image);

			analytics.LogStereoViewCount ();
		} else {
			statusMessage.text = "Error getting stereoscopic panorama from index";
		}

		// clear previous info
		foreach (Transform child in titleMessage.transform) {
			Destroy(child.gameObject);
		}

		yield break;
	}

	private IEnumerator FetchMono() {
//		analytics.gav3.LogEvent ("Panorama:" + analytics.sessionId, "RequestedMono", "foo", 1);
//		analytics.LogEvent ("Panorama", "Requested");

		WWW www;
//		analytics.LogEvent ("Panorama", "DebugNewWWW");
		ImageInfo flickrImage;
//		analytics.LogEvent ("Panorama", "DebugNewImageInfo");

		// first try recommendation
		flickrImage = GetRecommendedImage (analytics.monoViewCount);

//		analytics.LogEvent ("Panorama", "DebugGotRecommendation");
		analytics.gav3.LogEvent ("Panorama:" + analytics.sessionId, "DebugGotRecommendation", "foo", 1);

		// else get random
		if (flickrImage == null) {
			analytics.LogEvent ("Panorama", "FetchingRandom");
			statusMessage.text = "Waiting for www.flickriver.com...";

			bool censored;
			Stopwatch s = new Stopwatch();
			s.Start();

			do {
				www = new WWW (IMAGES_URL);
				networkConns.Add(www);
				print ("Fetching page: " + IMAGES_URL);
				yield return www;

				flickrImage = ExtractFromFlickriver (www.text);
				if (flickrImage != null) {
					flickrImage.imageId = ExtractIdFromFlickrUrl(flickrImage.url);
				}

				censored = IsCensored(flickrImage);
			} while (censored);

			s.Stop();
			analytics.LogTiming("Loading", s.ElapsedMilliseconds, "Flickriver", "FetchPage");
		} else {
			analytics.LogEvent("Panorama", "FetchingRecommendation" + analytics.monoViewCount);
		}

		if (flickrImage != null) {
			Stopwatch s = new Stopwatch();
			analytics.LogEvent("Panorama", "DebugFetchFlickr");
			s.Start();

			www = new WWW (flickrImage.url);
			networkConns.Add (www);
			statusMessage.text = "Waiting for www.flickr.com...";
			print ("Fetching page: " + flickrImage.url);
			yield return www;

			s.Stop();
			analytics.LogEvent("Panorama", "DebugGotFlickr");
			analytics.LogTiming("Loading", s.ElapsedMilliseconds, "Flickr", "FetchPage");

			PanoramaImage image = ExtractFromFlickr (www.text);
			image.imageInfo = flickrImage;
			www = null;

			statusMessage.text = "";

			if (renderMode == RenderMode.MONO_PANORAMA) {
				/* draw! */
				DrawPanorama (image);
				ShowInfo (flickrImage);

				/* log */
				analytics.LogMonoViewCount();
				analytics.LogEvent ("Panorama", "ImagesLoading");
			}
		} else {
			statusMessage.text = "Failed to find a panorama to show!";
			analytics.LogException("Failed to extract URL from Flickriver", true);
		}
	}

	private IEnumerator FetchStereoImg() {
		WWW www;
		ImageInfo flickrImage;

		analytics.gav3.LogEvent ("Panorama:" + analytics.sessionId, "RequestedStereoImg", "foo", 1);

		statusMessage.text = "Waiting for www.flickriver.com...";

		www = new WWW (STEREO_IMAGES_URL);
		networkConns.Add (www);
		print ("Fetching page: " + STEREO_IMAGES_URL);
		yield return www;

		flickrImage = ExtractFromFlickriver (www.text);
		if (flickrImage != null) {
			flickrImage.imageId = ExtractIdFromFlickrUrl(flickrImage.url);
		}

		if (flickrImage != null) {
			www = new WWW (flickrImage.url);
			networkConns.Add(www);
			statusMessage.text = "Waiting for www.flickr.com...";
			print ("Fetching page: " + flickrImage.url);
			yield return www;

			PanoramaImage image = ExtractFromFlickr (www.text);
			image.stereoType = StereoType.CROSS_EYE;
			flickrImage.width /= 2;
			image.imageInfo = flickrImage;
			www = null;
			
			statusMessage.text = "";

			DrawPanorama (image);
			
			analytics.LogStereoImgViewCount ();
		} else {
			statusMessage.text = "Failed to find a stereo image to show!";
			analytics.LogException("Failed to extract URL from Flickriver", true);
		}

		// clear previous info
		foreach (Transform child in titleMessage.transform) {
			Destroy(child.gameObject);
		}

	}

	private ImageInfo ExtractFromFlickriver (string body) {
		var match = Regex.Match(body, @"<a class=""noborder""  target=""_blank""  href=""(\S+)""><img class=""photo-panel-img"" id.+?width=""(\d+)"" height=""(\d+)"".+?alt=""([^""]+)""");
		var authorMatch = Regex.Match(body, @"by  <a  href="".+?"">([^<]+)</a>");

		if (match.Success) {
			ImageInfo flickrImage = new ImageInfo ();

			flickrImage.url = match.Groups [1].Value;
			flickrImage.width = int.Parse(match.Groups [2].Value);
			flickrImage.height = int.Parse(match.Groups [3].Value);
			flickrImage.title = match.Groups [4].Value;
			flickrImage.author = (authorMatch.Success ? authorMatch.Groups [1].Value : "");

			return flickrImage;
		} else {
			return null;
		}
	}

	private ImageInfo GetRecommendedImage (int idx) {
		return (idx >= 0 && idx < Recommendations.interesting.Length ? Recommendations.interesting [idx] : null);
	}
	
	private bool IsCensored(ImageInfo image) {
		if (image == null) {
			return false;
		}

		return Recommendations.censored.Contains (image.imageId);
	}

	private PanoramaImage ExtractFromFlickr(string body) {
		if (body == null) {
			return new PanoramaImage();
		}

		PanoramaImage image = new PanoramaImage ();

		string[] urls = Regex.Split (body, @"""displayUrl""");

		string smallUrl = "";
		string mediumUrl = "";
		string largeUrl = "";
		string xlargeUrl = "";

		for (int i = 1; i < urls.Length; i++) {
			smallUrl = (smallUrl.Length == 0 ? MatchUrl(urls[i], @":""(\S+?)"",""width"":100") : smallUrl);
			mediumUrl = (mediumUrl.Length == 0 ? MatchUrl(urls[i], @":""(\S+)"",""width"":640") : mediumUrl );
			largeUrl = (largeUrl.Length == 0 ? MatchUrl(urls[i], @":""(\S+)"",""width"":1024") : largeUrl );
			xlargeUrl = (xlargeUrl.Length == 0 ? MatchUrl(urls[i], @":""(\S+)"",""width"":2048") : xlargeUrl );
		}

		image.url.Add (smallUrl);
		image.url.Add (mediumUrl);
		image.url.Add (largeUrl);
		image.url.Add (xlargeUrl);

		return image;
	}

	private long ExtractIdFromFlickrUrl(string flickrUrl) {
		string[] s = flickrUrl.Split ('/');
		return long.Parse( s [s.Length - 2] );
	}

	private string MatchUrl(string body, string regex) {
		var match = Regex.Match(body, regex);
		return (match.Success ? match.Groups[1].Value.Replace("\\/", "/").Replace("//", "http://") : "");
	}

	private void DrawPanorama(PanoramaImage image){
		try {
			taskQueue = new List<object> ();

			List<Material> mats = null;
			Material m1 = null, m2 = null;

			switch (image.stereoType) {
			case StereoType.NONE:
				m1 = new Material(monoPanoramaMat);
				monoEyePano.SetActive(true);
				leftEyePano.SetActive(false);
				rightEyePano.SetActive(false);
				leftEyeImg.SetActive(false);
				rightEyeImg.SetActive(false);
				break;
			case StereoType.SBS:
				m1 = new Material(leftSBSPanoramaMat);
				m2 = new Material(rightSBSPanoramaMat);
				monoEyePano.SetActive(false);
				leftEyePano.SetActive(true);
				rightEyePano.SetActive(true);
				leftEyeImg.SetActive(false);
				rightEyeImg.SetActive(false);
				break;
			case StereoType.OVER_UNDER:
				m1 = new Material(leftOUPanoramaMat);
				m2 = new Material(rightOUPanoramaMat);
				monoEyePano.SetActive(false);
				leftEyePano.SetActive(true);
				rightEyePano.SetActive(true);
				leftEyeImg.SetActive(false);
				rightEyeImg.SetActive(false);
				break;
			case StereoType.OVER_UNDER_INV:
				m1 = new Material(leftOUInvPanoramaMat);
				m2 = new Material(rightOUInvPanoramaMat);
				monoEyePano.SetActive(false);
				leftEyePano.SetActive(true);
				rightEyePano.SetActive(true);
				leftEyeImg.SetActive(false);
				rightEyeImg.SetActive(false);
				break;
			case StereoType.CROSS_EYE:
				m1 = new Material(leftCrossImgMat);
				m2 = new Material(rightCrossImgMat);
				monoEyePano.SetActive(false);
				leftEyePano.SetActive(false);
				rightEyePano.SetActive(false);
				leftEyeImg.SetActive(true);
				rightEyeImg.SetActive(true);
				break;
			}

			if (monoEyePano.activeSelf) {
				monoEyePano.GetComponent<Renderer>().sharedMaterial = m1;
				mats = new List<Material>() {m1};
			}
			if (leftEyePano.activeSelf) {
				leftEyePano.GetComponent<Renderer>().sharedMaterial = m1;
				mats = new List<Material>() {m1, m2};
			}
			if (rightEyePano.activeSelf) {
				rightEyePano.GetComponent<Renderer>().sharedMaterial = m2;
				mats = new List<Material>() {m1, m2};
			}

			if (leftEyeImg.activeSelf) {
				leftEyeImg.GetComponent<Renderer>().sharedMaterial = m1;
				leftEyeImg.transform.localScale = new Vector3(image.imageInfo.width * PX_TO_METERS, image.imageInfo.height * PX_TO_METERS, 1);
				leftEyeImg.transform.position = 133.4f * statusMessage.gameObject.transform.parent.transform.forward;
				leftEyeImg.transform.LookAt(Vector3.zero);
				leftEyeImg.transform.RotateAround(leftEyeImg.transform.position, leftEyeImg.transform.up, 180f);
				mats = new List<Material>() {m1, m2};

				imgCaption.transform.localPosition = new Vector3(0, -0.55f, 0);
				imgCaption.transform.localRotation = Quaternion.identity;
				TextMesh caption = imgCaption.GetComponent<TextMesh>();
				caption.anchor = TextAnchor.MiddleCenter;
				caption.color = Color.black;
				string shortUrl = "flic.kr/p/" + Base58.Encode(image.imageInfo.imageId);
				caption.text = "by: " + image.imageInfo.author + " (" + shortUrl + ")";
				caption.characterSize = 0.3f;

				/*reportImage.transform.localPosition = new Vector3(0, -0.63f, 0);
				reportImage.transform.localRotation = Quaternion.identity;
				TextMesh reportText = reportImage.GetComponent<TextMesh>();
				reportText.characterSize = 0.3f;
				reportImage.SetActive(true);

				saveFavorite.transform.localPosition = new Vector3(0, -0.74f, 0);
				saveFavorite.transform.localRotation = Quaternion.identity;
				TextMesh saveFavoriteText = saveFavorite.GetComponent<TextMesh>();
				saveFavoriteText.characterSize = 0.3f;
				saveFavorite.SetActive(true);*/
			}
			if (rightEyeImg.activeSelf) {
				rightEyeImg.GetComponent<Renderer>().sharedMaterial = m2;
				rightEyeImg.transform.localScale = new Vector3(image.imageInfo.width * PX_TO_METERS, image.imageInfo.height * PX_TO_METERS, 1);
				rightEyeImg.transform.position = 133.4f * statusMessage.gameObject.transform.parent.transform.forward;
				rightEyeImg.transform.LookAt(Vector3.zero);
				rightEyeImg.transform.RotateAround(rightEyeImg.transform.position, rightEyeImg.transform.up, 180f);
				mats = new List<Material>() {m1, m2};
			}

			foreach (string url in image.url) {
				if (url.Length > 0) {
					taskQueue.Add (new ApplyImageTask (url, mats));
				}
			}
			taskQueue.Add (new ShowTutorialTask ());

			if (image.imageInfo != null) {
				currentImageId = image.imageInfo.imageId;
			}

			StartCoroutine( ProcessTasks () );
		} catch (System.Exception e) {
			analytics.LogException("Error drawing panorama: " + e.Message + "; " + e.StackTrace, true);
		}
	}

	/**
	 * Renders the image progressively, using the small, medium and large URLs in succession
	 */
	private IEnumerator ProcessTasks() {
		int i = 0; // single coroutine HACK
		ctf = new Stopwatch ();
		Stopwatch total = new Stopwatch();
		ctf.Start ();
		total.Start ();

		while (taskQueue.Count > 0) {
			object t = null;
			try {
				t = taskQueue[0];
				taskQueue.RemoveAt(0);
			} catch (System.Exception e) {
				analytics.LogException("Error pulling task: " + e.Message + "; " + e.StackTrace, true);
				continue;
			}

			if (t is ApplyImageTask) {
				ApplyImageTask task = (ApplyImageTask)t;
				yield return StartCoroutine(ShowImage(task, i));
			} else if (t is ShowTutorialTask) {
				ShowTutorial();
			}

			i++;
		}

		print ("Done");
		total.Stop ();
		analytics.LogEvent("Panorama", "AllImagesDone");
		analytics.LogTiming ("Loading", total.ElapsedMilliseconds, "Flickr", "TotalTime");
	}

	private IEnumerator ShowImage(ApplyImageTask task, int taskIndex) {
		Stopwatch s = new Stopwatch();
		analytics.LogEvent("Panorama", "DebugFetchFlickrImage");
		s.Start();
		
		WWW www;
		try {
			www = new WWW(task.url);
			networkConns.Add (www);
		} catch (System.Exception e) {
			analytics.LogException("Error fetching image url: " + e.Message + "; " + e.StackTrace, true);
			www = null;
			yield break;
		}
		print ("Fetching image: " + task.url);
		yield return www;

		if (!networkConns.Contains (www)) { // connection was canceled
			www = null;
			yield break;
		}
		
		s.Stop();
		analytics.LogEvent ("Panorama", "DebugGotFlickrImage");
		analytics.LogTiming("Loading", s.ElapsedMilliseconds, "Flickr", "Image" + taskIndex + "Fetch");

		try {
			foreach (Material m in task.mats) {
				m.mainTexture = www.texture;
			}
		} catch (System.Exception e) {
			analytics.LogException("Error applying new texture: " + e.Message + "; " + e.StackTrace, true);
			www = null;
			yield break;
		}
		www = null;
		
		analytics.LogEvent("Panorama", "ImageRendered");
		if (renderMode == RenderMode.STEREO_PANORAMA) statusMessage.text = "";
		if (taskIndex == 0) {
			firstImageLoadTimeout = Time.time + IMAGE_VIEWING_TIMEOUT_DURATION;
			analytics.gav3.LogEvent ("Panorama:" + analytics.sessionId, "RenderedBasicImage", "foo", 1);
			fetchAudio.Play();

			GCOldTasks();
		} else if (taskIndex == 2) {
			analytics.LogEvent("Panorama", "LargeImageRendered");
			ctf.Stop();
			analytics.LogTiming("Loading", ctf.ElapsedMilliseconds, "Flickr", "LargeImageCTCF");
		}

		tasksToGCNext.Add(task);
	}
	
	private void ShowTutorial() {
		if (!analytics.tutorialOneFinished) {
			statusMessage.text = TUTORIAL_ONE_TXT;
			tutorialOneVisible = true;
			tutorialOneRepeatVisible = false;
			tutorialTwoVisible = false;
			feedbackVisible = false;
		} else if (!analytics.tutorialOneRepeatFinished) {
			statusMessage.text = TUTORIAL_ONE_REPEAT_TXT;
			tutorialOneVisible = false;
			tutorialOneRepeatVisible = true;
			tutorialTwoVisible = false;
			feedbackVisible = false;
			
			tutorialOneRepeatEndTime = Time.time + TUTORIAL_ONE_REPEAT_DURATION;
			
			TutorialOneRepeatCompleted ();
		} else if (!analytics.tutorialTwoFinished && analytics.sessionCount >= 3 && (renderMode == RenderMode.MONO_PANORAMA || renderMode == RenderMode.STEREO_PANORAMA)) {
			statusMessage.text = TUTORIAL_TWO_TXT;
			tutorialOneVisible = false;
			tutorialOneRepeatVisible = false;
			tutorialTwoVisible = true;
			feedbackVisible = false;
		} else if (analytics.tutorialTwoFinished && !analytics.feedbackFinished && analytics.sessionCount >= 5) {
			statusMessage.text = FEEDBACK_TXT;
			tutorialOneVisible = false;
			tutorialOneRepeatVisible = false;
			tutorialTwoVisible = false;
			feedbackVisible = true;

			feedbackEndTime = Time.time + FEEDBACK_DURATION;
			
			FeedbackCompleted ();
		}
	}
	
	private void ShowInfo (ImageInfo image) {
		try {
			// clear previous info
			foreach (Transform child in titleMessage.transform) {
				Destroy(child.gameObject);
			}
		} catch (System.Exception e) {
			analytics.LogException("Error destrying old info children: " + e.Message + "; " + e.StackTrace, false);
		}

		try {
			titleMessage.transform.parent = statusMessage.gameObject.transform.parent;

			// show new info
			GameObject titleObj = new GameObject();
			float distance = statusMessage.gameObject.transform.position.magnitude;
			titleObj.transform.parent = titleMessage.transform;
			titleObj.transform.localScale = Vector3.one;
			TextMesh titleText = titleObj.AddComponent<TextMesh>();
			string shortUrl = "flic.kr/p/" + Base58.Encode (image.imageId);
			titleText.text = image.title + " (" + shortUrl + ")\n" + "by: " + image.author;
			titleText.color = Color.white;
			titleText.characterSize = 1.2f;
			titleText.alignment = TextAlignment.Right;
			titleText.anchor = TextAnchor.MiddleCenter;
			TitleAnimation anim = titleObj.AddComponent<TitleAnimation> ();

			// set background color for info
			GameObject bgQuad = GameObject.CreatePrimitive (PrimitiveType.Quad);
			bgQuad.transform.parent = titleObj.transform;
			bgQuad.transform.localPosition = new Vector3 (0, 0, 0.2f);
			bgQuad.transform.localRotation = Quaternion.identity;
			Renderer titleTextRenderer = titleObj.GetComponent<Renderer> ();
			Bounds titleTextBounds = titleTextRenderer.bounds;
			bgQuad.transform.localScale = titleTextBounds.extents * 18f;
			MeshRenderer quadRenderer = bgQuad.GetComponent<MeshRenderer> ();
			quadRenderer.sharedMaterial = new Material (Shader.Find ("Standard"));
			quadRenderer.sharedMaterial.color = Color.black;

			// reposition
			titleMessage.transform.localPosition = statusMessage.transform.localPosition;
			titleMessage.transform.localRotation = Quaternion.identity;
			titleObj.transform.localPosition = Vector3.zero;
			titleObj.transform.localRotation = Quaternion.identity;

			// detach from parent
			titleMessage.transform.parent = null;
		} catch (System.Exception e) {
			analytics.LogException("Error showing new info: " + e.Message + "; " + e.StackTrace, false);
		}
	}
	
	private void TutorialOneCompleted() {
		analytics.LogTutorialOneDone();
		tutorialOneVisible = false;
	}

	private void TutorialOneRepeatCompleted() {
		analytics.LogTutorialOneRepeatDone();
	}
	
	private void TutorialTwoCompleted() {
		analytics.LogTutorialTwoDone ();
		statusMessage.text = "";
		tutorialOneVisible = false;
		tutorialOneRepeatVisible = false;
		tutorialTwoVisible = false;
		feedbackVisible = false;
	}
	
	private void FeedbackCompleted() {
		analytics.LogFeedbackDone ();
	}

	private void GCOldTasks() {
		foreach (object o in tasksToGCNext) {
			if (o is ApplyImageTask) {
				ApplyImageTask t = (ApplyImageTask)o;
				DisposeApplyImageTask(t);
			}
		}
		tasksToGCNext.Clear ();
	}

	private void DisposeApplyImageTask(ApplyImageTask task) {
		task.mats.ForEach(material => {
			Destroy(material.mainTexture);
			material.mainTexture = null;
			Destroy (material);
			material = null;
		});
		task.Dispose ();
	}
}

public enum StereoType { // stereotypical enum
	SBS, OVER_UNDER, OVER_UNDER_INV, CROSS_EYE, NONE
};

public enum RenderMode {
	MONO_PANORAMA, STEREO_PANORAMA, STEREO_IMAGE
};

public class PanoramaImage {
	public List<string> url = new List<string>();
	public StereoType stereoType = StereoType.NONE;
	public ImageInfo imageInfo;

	public PanoramaImage() {}

	public PanoramaImage(List<string> url) {
		this.url = url;
	}

	public PanoramaImage(List<string> url, StereoType stereoType) {
		this.url = url;
		this.stereoType = stereoType;
	}
}

public class ImageInfo {
	public string url;
	public int width;
	public int height;
	public string author;
	public string title;
	public long imageId = 0;

	public ImageInfo() {}

	public ImageInfo(string url, string author, string title) {
		this.url = url;
		this.author = author;
		this.title = title;
	}

	public ImageInfo(string url, string author, string title, long imageId) {
		this.url = url;
		this.author = author;
		this.title = title;
		this.imageId = imageId;
	}
}

class ShowTutorialTask {}

class ApplyImageTask : System.IDisposable {
	public string url;
	public List<Material> mats;

	public ApplyImageTask(string url, List<Material> mats) {
		this.url = url;
		this.mats = mats;
	}

	public void Dispose() {
		mats = null;
	}
}