using System;
using System.Collections;
using System.Collections.Generic;

namespace RL{
	public class Node{
		public int distanceCost = 0;
		public int heuristicCost = 0;
		public Node parent;
		public int estimateCost {
			get{
				return distanceCost+heuristicCost;
			}
		}
		public Vector2i position;
		public static UInt64 KeyFromPostion(Vector2i p){
			return (((UInt64)(UInt32)p.x) << 32) | (UInt64)(UInt32)p.y;
		}
		public UInt64 Key { get { return (((UInt64)(UInt32)position.x) << 32) | (UInt64)(UInt32)position.y; } }
		public Node(int x, int y){
			position = new Vector2i (x, y);
		}
		public Node(Vector2i _p){
			position = _p;
		}
	}
	public class Pathfinder{
		Dictionary<UInt64, Node> openList;
		Dictionary<UInt64, Node> closedList;
		public List<Vector2i> FindPath(Vector2i start, Vector2i end, CostCallback costFn, Map map){
			if (start.Equals (end)) {
				return new List<Vector2i> ();
			}
			// clear the lists
			openList = new Dictionary<ulong, Node> ();
			closedList = new Dictionary<ulong, Node> ();
			Node startNode = new Node (start);
			openList [startNode.Key] = startNode;
			bool pathfound = false;
			int searchCount = 0;
			while (!pathfound && openList.Count > 0 && searchCount < 500) {
				Node lowestCost = findLowestCost ();
				openList.Remove (lowestCost.Key);
				closedList[lowestCost.Key] = lowestCost;

				if (lowestCost.position.Equals (end)) {
					pathfound = true;
					break;
				}
				for (int i = 0; i < 4; i++) {

					Vector2i neighborPosition = new Vector2i (lowestCost.position.x + Map.nDir [i, 0], lowestCost.position.y + Map.nDir [i, 1]);

					int newCost = lowestCost.distanceCost + costFn (neighborPosition.x, neighborPosition.y);

					if (map.IsOpenTile (neighborPosition.x, neighborPosition.y) && !openList.ContainsKey (Node.KeyFromPostion (neighborPosition))) {
						Node newPosition = new Node (neighborPosition);
						newPosition.heuristicCost = (int)Math.Abs (neighborPosition.x - end.x) + (int)Math.Abs (neighborPosition.y - end.y);
						newPosition.distanceCost = newCost;
						newPosition.parent = lowestCost;
						openList[newPosition.Key] = newPosition;
					} else if (openList.ContainsKey (Node.KeyFromPostion (neighborPosition)) && openList[Node.KeyFromPostion (neighborPosition)].distanceCost > newCost) {
						openList [Node.KeyFromPostion (neighborPosition)].distanceCost = newCost;
						openList [Node.KeyFromPostion (neighborPosition)].parent = lowestCost;
					}
				}
				searchCount++;
			}
			Node nextn = null;
			// couldn't find a path to the end
			if (!closedList.ContainsKey (Node.KeyFromPostion (end))) {
				// just build a short list containing the neighbor tiles.
				// eventually this should find "as near as possible" type tiles to move towards
				// I guess it depends on the application though
				startNode = new Node (start);
				Node lowestCost = startNode;
				for (int i = 0; i < 4; i++) {
					Vector2i neighborPosition = new Vector2i (lowestCost.position.x + Map.nDir [i, 0], lowestCost.position.y + Map.nDir [i, 1]);
					int newCost = lowestCost.distanceCost + costFn (neighborPosition.x, neighborPosition.y);
					if (map.IsOpenTile (neighborPosition.x, neighborPosition.y) && !openList.ContainsKey (Node.KeyFromPostion (neighborPosition))) {
						Node newPosition = new Node (neighborPosition);
						newPosition.heuristicCost = (int)Math.Abs (neighborPosition.x - end.x) + (int)Math.Abs (neighborPosition.y - end.y);
						newPosition.distanceCost = newCost;
						newPosition.parent = lowestCost;
						openList [newPosition.Key] = newPosition;
					}
				}
				nextn = findLowestCost ();
			} else {
				nextn = closedList [Node.KeyFromPostion (end)];
			}
			// build the step list
			List<Vector2i> returnList = new List<Vector2i> ();
			while (nextn != null) {
				returnList.Add (nextn.position);
				nextn = nextn.parent;
			}         
			returnList.Reverse ();
			return returnList;

		}
		Node findLowestCost(){
			Node lowestCost = null;
			foreach (Node n in openList.Values) {
				if(lowestCost == null || lowestCost.estimateCost > n.estimateCost){
					lowestCost = n;
				}
			}
			return lowestCost;
		}
	}
}