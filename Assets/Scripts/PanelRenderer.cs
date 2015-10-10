using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

	private TextMesh statusMessage;
	private GameObject titleMessage;


	/* globals */
	private const float PX_TO_METERS = 0.02f;
	private const int MAX_ROWS = 3;
	private const float ROTATION_DURATION = 0.2f; // seconds
	private const float ROTATION_SPEED = 180 / ROTATION_DURATION; // degrees per second
	private const float DOUBLE_TRIGGER_INTERVAL = 1.5f; // seconds
	private const float HEALTH_CHECK_INTERVAL = 5; // seconds

	private const string TUTORIAL_ONE_TXT = "<b>Let's try something!</b>\nPush down and leave " +
		"trigger TWICE\nto load new images anytime.\n<i>(or</i> press Fire1 TWICE)";
	private const string TUTORIAL_ONE_REPEAT_TXT = "<b>Double-trigger again</b>\nevery time you want\na new image";
	private const string TUTORIAL_TWO_TXT = "<b>Pro tip, give it a try!</b>\nPush down and leave " +
		"trigger ONCE and <b>WAIT</b>\nto see behind you.\n<i>(or</i> press Fire1 ONCE and <b>WAIT</b>)";

	private const float TUTORIAL_ONE_REPEAT_DURATION = 4.5f; // seconds

	private const string IMAGES_URL = "http://www.flickriver.com/groups/equirectangular/pool/random/";


	/* scratchpad */
	private List<PanoramaImage> images = new List<PanoramaImage>();

	private Shader PANORAMA_SHADER;
	private float rotation = 0f;
	private Quaternion targetRotation;
	private AudioSource fetchAudio;

	private float lastTriggerTime = -100f; // seconds
	private bool tutorialOneVisible;
	private bool tutorialOneRepeatVisible;
	private bool tutorialTwoVisible;

	private float tutorialOneRepeatEndTime = -1; // seconds

	private GameObject el;
	private List<object> taskQueue;

	private Analytics analytics;
	private float nextHealthCheckTime = -1; // seconds


	void Start () {
		PANORAMA_SHADER = Shader.Find ("InsideVisible");
		statusMessage = GameObject.Find ("StatusMessage").GetComponent<TextMesh>();
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
	}

	void FixedUpdate() {
		transform.rotation = Quaternion.RotateTowards (transform.rotation, targetRotation, ROTATION_SPEED * Time.fixedDeltaTime);
	}

	private IEnumerator Fetch() {
		analytics.LogEvent ("Panorama", "Requested");

		fetchAudio.Play ();

		taskQueue = new List<object> ();
		WWW www;
		ImageInfo flickrImage;

		// first try recommendation
		print ("Fetch " + analytics.viewCount);
		flickrImage = GetRecommendedImage (analytics.viewCount);
		// else get random
		if (flickrImage == null) {
			analytics.LogEvent ("Panorama", "FetchingRandom");
			statusMessage.text = "Waiting for www.flickriver.com...";

			www = new WWW (IMAGES_URL);
			print ("Fetching page: " + IMAGES_URL);
			yield return www;

			flickrImage = ExtractFromFlickriver (www.text);
		} else {
			analytics.LogEvent("Panorama", "FetchingRecommendation" + analytics.viewCount);
		}

		if (flickrImage != null) {
			www = new WWW (flickrImage.url);
			statusMessage.text = "Waiting for www.flickr.com...";
			print ("Fetching page: " + flickrImage.url);
			yield return www;

			images = ExtractFromFlickr (www.text);
			flickrImage.imageId = ExtractIdFromFlickrUrl(flickrImage.url);
			www = null;

			statusMessage.text = "";


			/* draw! */
			DrawPanorama (images);
			ShowInfo (flickrImage);

			/* log */
			analytics.LogViewCount();
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

	private List<PanoramaImage> ExtractFromFlickr(string body) {
		if (body == null) {
			return new List<PanoramaImage>();
		}

		List<PanoramaImage> entries = new List<PanoramaImage>();
		PanoramaImage image = new PanoramaImage ();
		entries.Add (image);

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

		return entries;
	}

	private long ExtractIdFromFlickrUrl(string flickrUrl) {
		string[] s = flickrUrl.Split ('/');
		return long.Parse( s [s.Length - 2] );
	}

	private string MatchUrl(string body, string regex) {
		var match = Regex.Match(body, regex);
		return (match.Success ? match.Groups[1].Value.Replace("\\/", "/").Replace("//", "http://") : "");
	}
	
	// currently only draws one
	private void DrawPanorama(List<PanoramaImage> p){
		taskQueue = new List<object> ();

		if (el == null) {
			el = Instantiate (panoramaImg);
		}
		PanoramaImage image = p[0];

		Material m = new Material(PANORAMA_SHADER);
		el.GetComponent<Renderer> ().sharedMaterial = m;

		foreach (string url in image.url) {
			if (url.Length > 0) {
				taskQueue.Add (new ApplyImageTask (url, m));
			}
		}
		taskQueue.Add (new ShowTutorialTask ());

		el.transform.parent = transform;

		StartCoroutine( ProcessTasks () );
	}

	/**
	 * Renders the image progressively, using the small, medium and large URLs in succession
	 */
	private IEnumerator ProcessTasks() {
		WWW www;
		int i = 0; // single coroutine HACK

		while (taskQueue.Count > 0) {
			object t = taskQueue[0];
			taskQueue.RemoveAt(0);
			if (t is ApplyImageTask) {
				ApplyImageTask task = (ApplyImageTask)t;

				www = new WWW(task.url);
				print ("Fetching image: " + task.url);
				yield return www;

				task.material.mainTexture = www.texture;
				www = null;

				analytics.LogEvent("Panorama", "ImageRendered");
				if (i == 2) {
					analytics.LogEvent("Panorama", "LargeImageRendered");
				}
			} else if (t is ShowTutorialTask) {
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

			i++;
		}

		print ("Done");
		analytics.LogEvent("Panorama", "AllImagesDone");
	}

	private void ShowInfo (ImageInfo image) {
		// clear previous info
		foreach (Transform child in titleMessage.transform) {
			Destroy(child.gameObject);
		}

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

class PanoramaImage {
	public List<string> url = new List<string>();
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
	public Material material;

	public ApplyImageTask(string url, Material m) {
		this.url = url;
		this.material = m;
	}
}