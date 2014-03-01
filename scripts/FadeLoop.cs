using UnityEngine;
using System.Collections;

public class FadeLoop : MonoBehaviour {
	float fadeTime, fadeTarget, startTime, startVolume;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	public void FadeTo(float value, float time){
		fadeTime = time;
		startTime = Time.time;
		fadeTarget = value;
		startVolume = audio.volume;
		StartCoroutine (FadeTitleMusic());
	}
	IEnumerator FadeTitleMusic(){
		while ((Time.time-startTime) <= fadeTime) {
			audio.volume = Mathf.Lerp(startVolume, fadeTarget, (Time.time-startTime)/fadeTime);
			yield return new WaitForEndOfFrame ();
		}
	}
}
