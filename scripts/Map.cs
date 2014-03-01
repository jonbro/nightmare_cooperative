using UnityEngine;
using System;
using System.Collections.Generic;

// finally getting around to making a reusable mapping system
// just because I write this fucking boilerplate over and over again

public static class Vector2Extensions {
	public static Vector2i ToVector2i (this Vector2 vector2) {
		int[] intVector2 = new int[2];
		for (int i = 0; i < 2; ++i) intVector2[i] = Mathf.RoundToInt(vector2[i]);
		return new Vector2i (intVector2);
	}
}

public struct Vector2i  {
	public int x, y;
	public Vector2i(int _x, int _y){
		x = _x;
		y = _y;
	}
	public Vector2i (int[] xy) {
		x = xy[0];
		y = xy[1];
	}
	public bool Equals(Vector2i v){
		return v.x == x && v.y == y;
	}
	static public Vector2i operator +(Vector2i a, Vector2i v){
		a.x += v.x;
		a.y += v.y;
		return a;
	}
	public float Distance(Vector2i a){
		return Mathf.Sqrt (Mathf.Pow (x - a.x, 2) + Mathf.Pow (y - a.y, 2));
	}
}

namespace RL{
	public enum TileType{
		OPEN,
		WALL,
		HARD_WALL,
		LAVA,
		CHANGE_LEFT,
		CHANGE_RIGHT,
		CHANGE_UP,
		CHANGE_DOWN,
		STAIRS_DOWN
	}
	public delegate int CostCallback(int x, int y);
	public class Map
	{
		public int sx, sy;
		TileType[,] layout;
		int[,] buffer; // used for floodfills
		public static int[,] nDir = { {0, -1}, {-1, 0}, {1, 0}, {0, 1},
			{-1, -1}, {1, -1},
			{-1, 1}, {1, 1} };

		public static int[,] nDirOrdered = { {0, -1}, {-1, 0}, {0, 1}, {1, 0} };

		public Map (int _sx, int _sy)
		{
			sx = _sx;
			sy = _sy;
			layout = new TileType[sx,sy];
			buffer = new int[sx,sy];
			InitMap();
		}
		public void InitMap(){
			// just fill this in with the map carving method you want
			// put in walls around the outer for now, and empty the internals
//			for (int x = 0; x < sx; x++) {
//				for (int y = 0; y < sy; y++) {
//					layout [x, y] = TileType.OPEN;
//				}
//			}
			for (int x = 0; x < sx; x++) {
				for (int y = 0; y < sy; y++) {
					if (x == 0 || x == sx - 1 || y == 0 || y == sy - 1) {
						layout [x, y] = TileType.HARD_WALL;
					} else {
						layout [x, y] = TileType.OPEN;
					}
				}
			}
		}
		public TileType GetTile(int x, int y){
			return layout [x, y];
		}
		public void SetTile(int x, int y, TileType t){
			layout [x, y] = t;
		}
		public bool IsValidTile(int x, int y){
			if (x < 0 || y < 0 || y >= sy || x >= sx)
				return false;
			return true;		
		}
		public bool IsOpenTile(int x, int y){
			return IsValidTile(x, y) && layout [x, y] != TileType.WALL && layout [x, y] != TileType.HARD_WALL;
		}
		public Vector2i GetPath(int startX, int startY, int endX, int endY){
			return GetPath(startX, startY, endX, endY, ((x, y) => { return 1; }));
		}

		// maybe should just return the next position
		// this method is super slow, need to rebuild so it isn't
		public Vector2i GetPath(int startX, int startY, int endX, int endY, CostCallback costFn){
			clearBuffer();
			floodHeight(endX, endY, 1, ref buffer, costFn);
			int lowestNeighbor = -1;
			int lowestNeighborDir = 0;
			for(int i=0;i<4;i++){
				int tx = startX+nDir[i,0];
				int ty = startY+nDir[i,1];
				if(tx>=0&&tx<buffer.GetLength(0)&&ty>=0&&ty<buffer.GetLength(1) &&
					IsOpenTile(tx, ty) &&
					//!ContainsSnake(tx, ty) && // need to have a delegate for calculating tile movement costs, perhaps pass into the flood height as well
					(lowestNeighbor == -1 || lowestNeighbor > buffer[tx, ty])
				){
					lowestNeighbor = buffer[tx, ty];
					lowestNeighborDir = i;
				}
			}
			if (lowestNeighbor == -1) {
				// return out of map for invalid tile
				new Vector2i(new int[]{-1, -1});
			}
			return new Vector2i (startX + nDir [lowestNeighborDir, 0], startY + nDir [lowestNeighborDir, 1]);
		}
		// an a* pathfinder
		public void Pathfind(Vector2i start, Vector2i end){
		
		}
		void floodHeight(int x, int y, int marker, ref int[,] buffer, CostCallback fn){
			// make sure to only visit each cell once
			buffer[x,y] = marker;		
			for(int i=0;i<4;i++){
				int tx = x+nDir[i,0];
				int ty = y+nDir[i,1];
				if(tx>=0&&tx<buffer.GetLength(0)&&ty>=0&&ty<buffer.GetLength(1)
					&& layout[tx,ty] == TileType.OPEN
					&& (buffer[tx, ty]==0 || (marker+fn(tx, ty)) < buffer[tx,ty]) )
				{
					floodHeight(tx, ty, marker+fn(tx, ty), ref buffer, fn);
				}
			}
		}
		void clearBuffer(){
			for(int x=0;x<sx;x++){
				for(int y=0;y<sy;y++){
					buffer[x,y] = 0;
				}
			}
		}
		static List<Vector2i> points = new List<Vector2i>();
		public static Vector2i[] Line(Vector2i s, Vector2i e)
		{
			int x0 = s.x;
			int y0 = s.y;
			int x1 = e.x;
			int y1 = e.y;

			points.Clear();
			bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
			bool reverse = false;
			if (steep) {
				int tx = x0;
				x0 = y0;
				y0 = tx;

				tx = x1;
				x1 = y1;
				y1 = tx;

			}
			if (x0 > x1) {
				int tx = x1; x1 = x0; x0 = tx;
				tx = y1; y1 = y0; y0 = tx;
				reverse = true;
			}

			int dX = (x1 - x0);
			int dY = Mathf.Abs(y1 - y0);
			int err = (dX / 2);
			int ystep = (y0 < y1 ? 1 : -1);
			int y = y0;

			for (int x = x0; x <= x1; ++x)
			{
				if(steep){
					points.Add(new Vector2i(y,x));
				}else{
					points.Add(new Vector2i(x,y));
				}
				err = err - dY;
				if (err < 0) { y += ystep;  err += dX; }
			}
			if(reverse){
				points.Reverse();
			}
			Vector2i[] _points = new Vector2i[points.Count];
			points.CopyTo(_points);
			return _points;
		}

	}
}