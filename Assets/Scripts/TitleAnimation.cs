using UnityEngine;
using System.Collections;

/**
 * Autohiding animation for title and author message
 * 
 * by cmdr2 <secondary.cmdr2@gmail.com>
 */
public class TitleAnimation : MonoBehaviour {

	/* globals */
	private const float SCROLL_DOWN_TIMEOUT = 4f; // seconds
	private const float SCROLL_DOWN_DURATION = 1.4f; // seconds


	/* scratchpad */
	private float scrollDownTime = -1; // seconds
	private float distance;
	private Vector3 initPos;


	void Start () {
		scrollDownTime = Time.time + SCROLL_DOWN_TIMEOUT;
		initPos = transform.position;
		distance = transform.position.magnitude;
	}

	void Update() {
		if (Time.time > scrollDownTime && Time.time < (scrollDownTime + SCROLL_DOWN_DURATION)) {
			transform.parent.transform.parent = null;
			float t = (Time.time - scrollDownTime) / SCROLL_DOWN_DURATION;
			transform.position = Vector3.Lerp(initPos, new Vector3(0, -distance, 0), t);
			transform.LookAt(Vector3.zero);
			transform.RotateAround(transform.position, transform.up, 180f);

			if (t >= 0.95) {
				scrollDownTime = Mathf.Infinity;
			}
		}
	}
}
