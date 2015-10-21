using UnityEngine;
using System.Collections;

public class ReportImageButton : MonoBehaviour {
	/* scratchpad */
	private GameObject camera;
	private TextMesh captionEl;
	private Analytics analytics;
	private PanelRenderer panelRenderer;


	void Start () {
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
				analytics.gav3.LogEvent("Blacklist", "3dImg-" + panelRenderer.currentImageId, "foo", 1);
			}
		} else {
			captionEl.fontStyle = FontStyle.Normal;
		}
	}
}
