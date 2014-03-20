using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrapDisplay : MonoBehaviour
{
	GameObject[] lineRenderers;
	RL.Map map;
	int[] directions;
	public void Setup(RL.Map _map, int[] _dir){
		map = _map;
		directions = _dir;
		lineRenderers = new GameObject[directions.Length];
		for (int i = 0; i < directions.Length; i++) {
			lineRenderers [i] = new GameObject ();
			lineRenderers [i].transform.parent = transform;
			lineRenderers [i].transform.position = transform.position;
			LineRenderer lr = lineRenderers [i].AddComponent<LineRenderer> ();
			lr.renderer.sortingOrder = 2;
			lr.renderer.sortingLayerName = "Default";
			lr.SetColors(new Color(74/255f,63/255f,148/255f, 0.5f), new Color(113/255f,138/255f,145/255f, 0.5f));
			lr.SetWidth (0.05f, 0.05f);
			lr.material = (Material)Resources.Load ("trapParticle");
			lr.useWorldSpace = false;
			// move out on the direction until we hit a wall, this is our endpoint
			Vector2i ndir = new Vector2i (RL.Map.nDir[directions[i],0], RL.Map.nDir[directions[i],1]);
			Vector2i np = GetComponent<RLCharacter>().positionI;
			int distanceCount = 1;
			np += ndir;
			while (map.IsOpenTile (np.x, np.y)) {
				np += ndir;
			}
//			np -= ndir*0.5f;
			lr.SetPosition (0, Vector3.zero);
			lr.SetPosition (1, new Vector3(np.x-ndir.x*0.5f, np.y-ndir.y*0.5f, 0)-transform.position);
		}
	}
	public void EnableChildren(){
		for (int i = 0; i < directions.Length; i++) {
			lineRenderers [i].SetActive (true);
		}
	}
	public void DisableChildren(){
		for (int i = 0; i < directions.Length; i++) {
			lineRenderers [i].SetActive (false);
		}
	}
}