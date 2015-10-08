using UnityEngine;
using System.Collections;

/**
 * Throbbing animation for status messages
 * 
 * by cmdr2 <secondary.cmdr2@gmail.com>
 */
public class LoadingAnimation : MonoBehaviour {

	/* globals */
	private const float FLASH_SPEED = 0.3f;


	/* scratchpad */
	private float origZ;

	void Start () {
		origZ = transform.localPosition.z;
	}


	void FixedUpdate () {
//		Color c = this.GetComponent<Renderer> ().sharedMaterial.color;
//		c.a = 0.8f + Mathf.PingPong (Time.time * FLASH_SPEED, 0.2f);
//		this.GetComponent<Renderer> ().sharedMaterial.color = c;
		transform.localPosition = new Vector3(
			transform.localPosition.x,
			transform.localPosition.y,
			origZ - 0.05f + Mathf.PingPong (Time.time * FLASH_SPEED, 0.1f)
		);
	}
}
