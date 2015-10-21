using UnityEngine;
using System.Collections;
using System.IO;

public class SaveFavoriteButton : MonoBehaviour {
	/* globals */
	private static string FAVORITES_FILE;


	/* scratchpad */
	private GameObject camera;
	private TextMesh captionEl;
	private Analytics analytics;
	private PanelRenderer panelRenderer;


	void Start () {
		FAVORITES_FILE = Application.persistentDataPath + Path.DirectorySeparatorChar + "Favorites";

		camera = GameObject.Find ("Main Camera Right");
		captionEl = GetComponent<TextMesh> ();
		analytics = GameObject.Find ("Analytics").GetComponent<Analytics>();
		panelRenderer = GameObject.Find ("Root").GetComponent<PanelRenderer> ();
	}
	
	void Update () {
		if (camera == null) {
			return;
		}
		
		Ray ray = new Ray (camera.transform.position, camera.transform.forward);
		RaycastHit hit;

		bool gazingAt = (Physics.Raycast (ray, out hit) && hit.transform == transform);
		bool trigger = Input.GetButtonDown ("Fire1");

		if (gazingAt) {
			captionEl.fontStyle = FontStyle.Bold;

			if (trigger) {
				captionEl.fontStyle = FontStyle.BoldAndItalic;
				analytics.gav3.LogEvent("Favorite", "3dImg-" + panelRenderer.currentImageId, "foo", 1);
				var sw = new StreamWriter(FAVORITES_FILE, true);
				sw.Write(panelRenderer.currentImageId + "\n");
				sw.Close();
			}
		} else {
			captionEl.fontStyle = FontStyle.Normal;
		}
	}
}
