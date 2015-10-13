using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

/**
 * Main class for fetching and rendering panoramas
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

	private TextMesh statusMessage;
	private GameObject titleMessage;

	private GameObject monoEyePano;
	private GameObject leftEyePano;
	private GameObject rightEyePano;


	/* globals */
	private const float PX_TO_METERS = 0.02f;
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

	private const float TUTORIAL_ONE_REPEAT_DURATION = 4.5f; // seconds

	private const string IMAGES_URL = "http://www.flickriver.com/groups/equirectangular/pool/random/";


	/* scratchpad */
	private bool isStereoMode = false;

	private Shader PANORAMA_SHADER;
	private float rotation = 0f;
	private Quaternion targetRotation;
	private AudioSource fetchAudio;
	private float firstImageLoadTimeout = -1; // seconds

	private float lastTriggerTime = -100f; // seconds
	private bool tutorialOneVisible;
	private bool tutorialOneRepeatVisible;
	private bool tutorialTwoVisible;

	private float tutorialOneRepeatEndTime = -1; // seconds
	private Stopwatch ctf;

	private List<object> taskQueue;

	private Analytics analytics;
	private float nextHealthCheckTime = -1; // seconds


	void Start () {
		PANORAMA_SHADER = Shader.Find ("InsideVisible");
		statusMessage = GameObject.Find ("StatusMessage").GetComponent<TextMesh>();
		monoEyePano = GameObject.Find ("monoEyePano");
		leftEyePano = GameObject.Find ("leftEyePano");
		rightEyePano = GameObject.Find ("rightEyePano");

		titleMessage = GameObject.Find ("TitleMessage");
		fetchAudio = GetComponent<AudioSource> ();
		analytics = GameObject.Find ("Analytics").GetComponent<Analytics>();
		analytics.Init ();

		StartCoroutine (Fetch ());
	}

	void Update () {
		bool triggered = Cardboard.SDK.CardboardTriggered || Input.GetButtonDown ("Fire1");
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
			
			rotation += 180f;
			targetRotation = Quaternion.Euler(0, rotation, 0);
			
			if (tutorialTwoVisible) {
				TutorialTwoCompleted();
			}
		}

		if (tutorialOneRepeatVisible && Time.time > tutorialOneRepeatEndTime) {
			tutorialOneRepeatVisible = false;
			statusMessage.text = "";
		}

		if (!isStereoMode && Cardboard.SDK.Tilted) {
			isStereoMode = true;
			StartCoroutine (Fetch ());
			Handheld.Vibrate ();
		}
	}

	void FixedUpdate() {
		transform.rotation = Quaternion.RotateTowards (transform.rotation, targetRotation, ROTATION_SPEED * Time.fixedDeltaTime);
	}

	private IEnumerator Fetch() {
		if (Time.time < firstImageLoadTimeout) {
			analytics.LogEvent("Panorama", "ThrottlingFetch");
			yield break;
		}
		
		firstImageLoadTimeout = Time.time + FIRST_IMAGE_LOAD_TIMEOUT_DURATION;

		analytics.gav3.LogEvent ("Panorama:" + analytics.sessionId, "Requested", "foo", 1);

		taskQueue = new List<object> ();

		yield return ( isStereoMode ? StartCoroutine (FetchStereo ()) : StartCoroutine (FetchMono ()) );
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
			statusMessage.text = "Waiting for www.flickr.com...";
			print ("Fetching page: " + flickrImage.url);
			yield return www;

			s.Stop();
			analytics.LogEvent("Panorama", "DebugGotFlickr");
			analytics.LogTiming("Loading", s.ElapsedMilliseconds, "Flickr", "FetchPage");

			PanoramaImage image = ExtractFromFlickr (www.text);
			www = null;

			statusMessage.text = "";


			/* draw! */
			DrawPanorama (image);
			ShowInfo (flickrImage);

			/* log */
			analytics.LogMonoViewCount();
			analytics.LogEvent ("Panorama", "ImagesLoading");
		} else {
			statusMessage.text = "Failed to find a panorama to show!";
			analytics.LogException("Failed to extract URL from Flickriver", true);
		}
	}

	private ImageInfo ExtractFromFlickriver (string body) {
		var match = Regex.Match(body, @"<a class=""noborder""  target=""_blank""  href=""(\S+)""><img class=""photo-panel-img"" id.+?alt=""([^""]+)""");
		var authorMatch = Regex.Match(body, @"by  <a  href="".+?"">([^<]+)</a>");

		if (match.Success) {
			ImageInfo flickrImage = new ImageInfo ();

			flickrImage.url = match.Groups [1].Value;
			flickrImage.title = match.Groups [2].Value;
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
				break;
			case StereoType.SBS:
				m1 = new Material(leftSBSPanoramaMat);
				m2 = new Material(rightSBSPanoramaMat);
				monoEyePano.SetActive(false);
				leftEyePano.SetActive(true);
				rightEyePano.SetActive(true);
				break;
			case StereoType.OVER_UNDER:
				m1 = new Material(leftOUPanoramaMat);
				m2 = new Material(rightOUPanoramaMat);
				monoEyePano.SetActive(false);
				leftEyePano.SetActive(true);
				rightEyePano.SetActive(true);
				break;
			case StereoType.OVER_UNDER_INV:
				m1 = new Material(leftOUInvPanoramaMat);
				m2 = new Material(rightOUInvPanoramaMat);
				monoEyePano.SetActive(false);
				leftEyePano.SetActive(true);
				rightEyePano.SetActive(true);
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

			foreach (string url in image.url) {
				if (url.Length > 0) {
					taskQueue.Add (new ApplyImageTask (url, mats));
				}
			}
			taskQueue.Add (new ShowTutorialTask ());

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
		} catch (System.Exception e) {
			analytics.LogException("Error fetching image url: " + e.Message + "; " + e.StackTrace, true);
			www = null;
			yield break;
		}
		print ("Fetching image: " + task.url);
		yield return www;
		
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
		if (isStereoMode) statusMessage.text = "";
		if (taskIndex == 0) {
			firstImageLoadTimeout = Time.time + IMAGE_VIEWING_TIMEOUT_DURATION;
			analytics.gav3.LogEvent ("Panorama:" + analytics.sessionId, "RenderedBasicImage", "foo", 1);
			fetchAudio.Play();
		} else if (taskIndex == 2) {
			analytics.LogEvent("Panorama", "LargeImageRendered");
			ctf.Stop();
			analytics.LogTiming("Loading", ctf.ElapsedMilliseconds, "Flickr", "LargeImageCTCF");
		}
	}
	
	private void ShowTutorial() {
		if (!analytics.tutorialOneFinished) {
			statusMessage.text = TUTORIAL_ONE_TXT;
			tutorialOneVisible = true;
			tutorialOneRepeatVisible = false;
			tutorialTwoVisible = false;
		} else if (!analytics.tutorialOneRepeatFinished) {
			statusMessage.text = TUTORIAL_ONE_REPEAT_TXT;
			tutorialOneVisible = false;
			tutorialOneRepeatVisible = true;
			tutorialTwoVisible = false;
			
			tutorialOneRepeatEndTime = Time.time + TUTORIAL_ONE_REPEAT_DURATION;
			
			TutorialOneRepeatCompleted();
		} else if (!analytics.tutorialTwoFinished && analytics.sessionCount >= 3) {
			statusMessage.text = TUTORIAL_TWO_TXT;
			tutorialOneVisible = false;
			tutorialOneRepeatVisible = false;
			tutorialTwoVisible = true;
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
	}
}

public enum StereoType { // stereotypical enum
	SBS, OVER_UNDER, OVER_UNDER_INV, NONE
};

public class PanoramaImage {
	public List<string> url = new List<string>();
	public StereoType stereoType = StereoType.NONE;

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
	public string author;
	public string title;
	public long imageId;

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

class ApplyImageTask {
	public string url;
	public List<Material> mats;

	public ApplyImageTask(string url, List<Material> mats) {
		this.url = url;
		this.mats = mats;
	}
}