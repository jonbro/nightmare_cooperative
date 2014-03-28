using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RLCharacter : MonoBehaviour {

	// a big set of tags, and unique names for all of the objects in the world
	// so that we can just search on a single list

	public enum RLTypes
	{
		EMPTY,
		STAIRS_UP,
		STAIRS_DOWN,

		MONSTER,
		MONSTER_WANDER,
		MONSTER_CHASE,
		MONSTER_WANDER_LOS,
		MONSTER_WANDER_CHASE,
		MONSTER_WALL_TURN_CHASE,
		MONSTER_DISTANCE_FIRE,
		MONSTER_DISTANCE_FIRE_DIAGONAL,
		MONSTER_SEE_KEEP_MOVING,
		MONSTER_TURN_LEFT,
		MONSTER_WALL_BOUNCE,
		MONSTER_WALL_TURN,
		MONSTER_WAIT_CHASE,
		MONSTER_NO_MOVE,
		MONSTER_PLAYER_COPY,

		WARRIOR,
		ARCHER,
		PRIEST,
		MINER,
		MAGE,

		PLAYER,
		WAITING_PLAYER,

		ACTION_PICKUP,
		HEALTH_PICKUP,
		GOLD_PICKUP,
		BULLET,
		BULLET_PICKUP
	}

	public Vector2i positionI;
	Vector3 targetPosition;
	public Facing direction;
	public List<RLTypes> hasTypes = new List<RLTypes>();
	float updateTime;
	public char display;
	public int health;
	public Color color;
	public Color tintColor = Color.white;
	public string name;
	public Vector3 offset;
	public float mapScale;
	GameObject hitAni;
	public MonsterDef def;
	// Use this for initialization
	void Start(){
		hitAni = (GameObject)Instantiate (Resources.Load ("hitAnimation"), transform.position, Quaternion.identity);;
		hitAni.transform.parent = transform;
		hitAni.GetComponent<SpriteRenderer> ().sortingOrder = 4;
	}
	void Awake () {
		hasTypes.Add (RLTypes.EMPTY);
		positionI.x = Mathf.RoundToInt(transform.position.x);
		positionI.y = Mathf.RoundToInt(transform.position.y);
		targetPosition = transform.position;

	}
	public void SetPosition(int x, int y){
		positionI.x = x;
		positionI.y = y;
		targetPosition = new Vector3 (x, y, 0);
		Go.defaultEaseType = GoEaseType.Linear;
		Go.to (gameObject.transform, 0.1f, TweenManager.TweenConfigCurrent().localPosition(targetPosition));
	}
	public void SetPositionImmediate(int x, int y){
		positionI.x = x;
		positionI.y = y;
		targetPosition = new Vector3 (x, y, 0);
		transform.position = targetPosition;
		Go.killAllTweensWithTarget (gameObject.transform);
	}
	public void SetWating(){
		transform.rotation = Quaternion.Euler (0, 0, 90);
		tintColor = GetComponent<SpriteRenderer> ().color = Color.Lerp (Color.white, Color.black, 0.5f);
	}
	public void WakeUp(){
		transform.rotation = Quaternion.Euler (0, 0, 0);
		tintColor = GetComponent<SpriteRenderer> ().color = Color.white;
	}
	Sprite[] sprites;
	public void AddSprite(string spriteName){
		if(!GetComponent<SpriteRenderer>())
			gameObject.AddComponent<SpriteRenderer> ();
		if(sprites == null)
			sprites = Resources.LoadAll<Sprite>("sprites");
		foreach (Sprite s in sprites) {
			if (s.name == spriteName) {
				gameObject.GetComponent<SpriteRenderer> ().sprite = s;
				if(hasTypes.Contains(RLTypes.PLAYER) || hasTypes.Contains(RLTypes.MONSTER))
					GetComponent<SpriteRenderer> ().sortingOrder = 3;
				else
					GetComponent<SpriteRenderer> ().sortingOrder = 2;
			}
		} 
		renderer.material = (Material)Resources.Load ("SpriteBasic");
		if (hasTypes.Contains (RLTypes.MONSTER)) {
			if (health == 2) {
				tintColor = GetComponent<SpriteRenderer> ().color = new Color (186 / 255f, 12 / 255f, 250 / 255f);
			} else {
				tintColor = GetComponent<SpriteRenderer> ().color = Color.red;
			}
		}
	}
	// Update is called once per frame
	void Update () {
		// copy the sprite color to the correct place

	}
	public void AddType(RLTypes t){
		hasTypes.Add (t);
	}
	public void Attack(Vector2i target){
		Vector3 tPos = transform.position + (new Vector3 (target.x, target.y) - transform.position) * 0.5f;
		Go.to (gameObject.transform, 0.5f, TweenManager.TweenConfigCurrent().position(tPos).setEaseType(GoEaseType.Punch).onBegin(PlayHit));
	}
	public void HideImmediate(){
		if (gameObject.GetComponent<SpriteRenderer> ())
			gameObject.GetComponent<SpriteRenderer> ().color = Color.clear;
	}
	public void Hide(){
		Go.killAllTweensWithTarget (gameObject.GetComponent<SpriteRenderer>());
		Go.to (gameObject.GetComponent<SpriteRenderer>(), 0.5f, new GoTweenConfig().colorProp("color", Color.clear));
	}
	public void Show(){
		Go.killAllTweensWithTarget (gameObject.GetComponent<SpriteRenderer>());
		Go.to (gameObject.GetComponent<SpriteRenderer>(), 0.5f, new GoTweenConfig().colorProp("color", tintColor));
	}
	public void Hit(int hitAmount = 1){
		health -= hitAmount;
		if(hasTypes.Contains(RLTypes.MONSTER))
			tintColor = GetComponent<SpriteRenderer>().color = Color.red;
		Go.to (gameObject.transform, 0.2f, TweenManager.TweenConfigCurrent ().onBegin (PlayHitAnimation));
	}
	public void PlayHitAnimation(AbstractGoTween t){
		hitAni.transform.rotation = Quaternion.Euler (0, 0, Random.Range (0, 3) * 90);
		hitAni.GetComponent<SpriteRenderer> ().enabled = true;
		hitAni.GetComponent<Animator> ().Play ("hitAnimation");
	}
	public void PlayHitAnimationHard(AbstractGoTween t){
		hitAni.transform.rotation = Quaternion.Euler (0, 0, 0);
		hitAni.GetComponent<SpriteRenderer> ().enabled = true;
		hitAni.GetComponent<Animator> ().Play ("hitAnimationHard");
	}
	public void PlayHit(AbstractGoTween t){
		if(hasTypes.Contains(RLTypes.MONSTER))
			Camera.main.audio.PlayOneShot (AllTogether.instance().playerHurtAudio);
	}
	public void Kill(){
//		Go.to (gameObject.transform, 0.2f, TweenManager.TweenConfigCurrent().scale(2).onIterationEnd(FinalKill));
		Go.to (gameObject.transform, 0.2f, TweenManager.TweenConfigCurrent().scale(2).materialColor(Color.clear).onIterationEnd(FinalKill));
	}
	public void FinalKill(AbstractGoTween t = null){
		if(!hasTypes.Contains(RLTypes.MONSTER)
			&& !hasTypes.Contains(RLTypes.GOLD_PICKUP)
			&& !hasTypes.Contains(RLTypes.HEALTH_PICKUP)
			&& !hasTypes.Contains(RLTypes.ACTION_PICKUP)
		)
			Camera.main.audio.PlayOneShot (AllTogether.instance().playerDeathAudio);
		Destroy (gameObject);
	}
	public void FinalKillNoAudio(){
		Go.killAllTweensWithTarget (gameObject.transform);
		Destroy (gameObject);
	}
}
