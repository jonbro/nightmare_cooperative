using UnityEngine;
using System.Collections;

public class MageBoltEffect : MonoBehaviour {
	public static GameObject particles;
	// Use this for initialization
	void Start () {
//		if (particles == null)
//			particles = GameObject.Find ("mageParticles");
//		particles.transform.position = transform.position;
//		particles.particleSystem.Simulate(0.005f, true);
//		particles.particleSystem.Play(true);
	}
	void OnDestroy(){
//		particles.particleSystem.Stop ();
//		particles.particleSystem.enableEmission = false;
	}
	// Update is called once per frame
	void Update () {
//		particles.transform.position = transform.position;
//		particles.particleSystem.enableEmission = true;
//		particles.particleSystem.Emit ();
	}
	void LateUpdate(){
//		particles.particleSystem.enableEmission = true;
	}
}
