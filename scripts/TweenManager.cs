using UnityEngine;
using System.Collections;

public class TweenManager : MonoBehaviour {
	// handles displaying and cleaning up all the tweens for each turn
	int currentTurn = 1;
	int displayingTurn = 0;

	static TweenManager _instance;

	public static TweenManager instance(){
		if (_instance == null)
			_instance = GameObject.Find ("AllTogetherGame").GetComponent<TweenManager> ();
		return _instance;
	}
	public static GoTweenConfig TweenConfigCurrent(){
		return new GoTweenConfig ().setId (instance().currentTurn).startPaused();
	}
	public void FinishAll(){
		while (displayingTurn < currentTurn) {
			if (Go.tweensWithId (displayingTurn) != null) {
				foreach (AbstractGoTween t in Go.tweensWithId(displayingTurn)) {
					t.complete ();
				}
			}
			displayingTurn++;
		}
	}
	public void StartPlay(){
		if (Go.tweensWithId (displayingTurn) == null)
			return;
		foreach(AbstractGoTween t in Go.tweensWithId(displayingTurn)){
			t.play ();
		}
	}
	public void NextTurn(){
		currentTurn++;
	}
	void Update(){
		if (Go.tweensWithId (displayingTurn) == null || Go.tweensWithId (displayingTurn).Count == 0) {
			if (displayingTurn < currentTurn) {
				displayingTurn++;
				if (Go.tweensWithId (displayingTurn) == null)
					return;
				foreach(AbstractGoTween t in Go.tweensWithId(displayingTurn)){
					t.play ();
				}
			}
		}
	}
}
