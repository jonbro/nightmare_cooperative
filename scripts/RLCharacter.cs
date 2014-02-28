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
		MONSTER_DISTANCE_FIRE,
		MONSTER_SEE_KEEP_MOVING,
		MONSTER_TURN_LEFT,

		WARRIOR,
		ARCHER,
		PRIEST,
		MINER,
		MAGE,

		PLAYER,
		WAITING_PLAYER,

		ACTION_PICKUP,
		HEALTH_PICKUP,
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

	// Use this for initialization
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
		updateTime = Time.time;

	}
	// Update is called once per frame
	void Update () {
		if ((Time.time - updateTime) * 3 < 1.0f) {
			transform.position = Vector3.Lerp (transform.position, targetPosition, (Time.time-updateTime)*3);
		}else
			transform.position = targetPosition;
	}
	public void AddType(RLTypes t){
		hasTypes.Add (t);
	}
	public void Kill(){
		Destroy (gameObject);
	}
}
