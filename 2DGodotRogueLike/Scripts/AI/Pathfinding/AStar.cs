using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;


//General AStar stuff
public class AStar
{
  public enum NodeState
  {
    InvalidState,
    ClosedList,
    OpenList,
    WallNode,
    NotChecked
  }

  //A single node in the AStar Map
  public class AStarNode
  {
    public Vector2 pos = new Vector2(0,0);
    public AStarNode parent;
    public NodeState state = NodeState.NotChecked;

    public void UpdateHeuristic(Vector2 goal)
    {
      Vector2 diff = new Vector2(Mathf.Abs(goal.X - pos.X), Mathf.Abs(goal.Y - pos.Y));

      // 1.41421356237 is sqrt(2). Use consistent float ops.
      heuristic = (Mathf.Min(diff.X, diff.Y) * 1.41421356237f) + Mathf.Max(diff.X, diff.Y) - Math.Min(diff.X, diff.Y);
    }

    //Doing all nodes weighted the same, will potentially have a dict of nodes to their weights if later add different weight nodes
    public static float weight = 1;
    public float givenCost;
    public float heuristic {get; private set;}
  }

  public class AStarNodeComparer : Comparer<AStarNode>
  {
    //Sort so the last node is the lowest heuristic 
    public override int Compare(AStarNode x, AStarNode y)
    {
      //Comparator defines the values based on heuristic then x/y
      int totalCostCompare = (x.heuristic + x.givenCost).CompareTo(y.heuristic + y.givenCost);
      if (totalCostCompare != 0)
      {
        return totalCostCompare;
      }

      if(x.pos.X.CompareTo(y.pos.X) != 0)
      {
        return x.pos.X.CompareTo(y.pos.X);
      }

      if(x.pos.Y.CompareTo(y.pos.Y) != 0)
      {
        return x.pos.Y.CompareTo(y.pos.Y);
      }

      return 0;
    }
  }

  //Contains data for AStar map
  public class AStarMap
  {
    public int [,] terrainMap;
    public int width;
    public int height;
    AStarNode [,] nodeMap;

    //Sets internal map and populates the AStar node map
    public AStarMap(int [,] newTerrainMap, int _width, int _height)
    {
      terrainMap = newTerrainMap;
      width = _width;
      height = _height;
      nodeMap = new AStarNode[width, height];

      for (int i = 0; i < width; i++)
      {
        for (int j = 0; j < height; j++)
        {
          //Initialize nodes 
          AStarNode newNode = new AStarNode();
          newNode.pos = new Vector2(i,j);
          if(terrainMap[i,j] == 1)
          {
            newNode.state = NodeState.WallNode;
          }
          nodeMap[i,j] = newNode;
        }
      }
    }
    //Accessors for the node map
    public AStarNode GetNodeAt(Vector2 pos)
    {
      return nodeMap[(int)pos.X, (int)pos.Y];
    }
    public void SetNodeAt(Vector2 pos, AStarNode newNode)
    {
      nodeMap[(int)pos.X, (int)pos.Y] = newNode;
    }
  }
  public enum PathState
  {
    None,
    Found,
    Searching,
    DoesNotExist
  }
  
  //Paths through an AStarMap
  public class AStarPather
  {
    SortedSet<AStarNode> openList;
    Vector2 startPos;
    Vector2 goalPos;
    PathState state;
    public AStarMap map {get; private set;}
    public List<Vector2> path {get; private set;}

    TileMap AStarVisMap = new TileMap();


    public void SetAStarVisualizationMap(ref Dictionary<string,TileMap> _mapIDVisualization, string map)
    {
      AStarVisMap = _mapIDVisualization[map];
    }

    //Update the A* map visual
    public void UpdateMapVisual()
    {
      //Reset map
      AStarVisMap.Clear();

      if(state == PathState.Searching)
      {
        for (int i = 0; i < map.width; i++)
        {
          for (int j = 0; j < map.height; j++)
          {
            if(map.GetNodeAt(new Vector2(i,j)).state == NodeState.ClosedList)
            {
              AStarVisMap.SetCell(0, new Vector2I(i,j), 10);
            }
            else if(map.GetNodeAt(new Vector2(i,j)).state == NodeState.OpenList)
            {
              AStarVisMap.SetCell(0, new Vector2I(i,j), 9);
            }
          }
        }
      }
      else if (state == PathState.Found)
      {
        foreach (var item in path)
        {
          AStarVisMap.SetCell(0, (Vector2I)item, 10);
        }
      }

      AStarVisMap.SetCell(0, new Vector2I((int)startPos.X,(int)startPos.Y), 4);
      AStarVisMap.SetCell(0, new Vector2I((int)goalPos.X,(int)goalPos.Y), 11);
    }

    //Initialize the openList and path
    public void InitPather(Vector2 _startPos, Vector2 _goalPos, AStarMap _map)
    {
      path = new List<Vector2>();
      openList = new SortedSet<AStarNode>(new AStarNodeComparer());

      map = _map;
      startPos = _startPos;
      goalPos = _goalPos;
      
      //Start with first node and update its state
      AStarNode startingNode = map.GetNodeAt(startPos);
      startingNode.state = NodeState.OpenList;
      startingNode.UpdateHeuristic(goalPos);
      openList.Add(startingNode);
      state = PathState.Searching;
    }

    //StartPos, EndPos, and path are relative to the terrain map
    //Generates a simple path, no smoothing or spine
    //Returns if found path
    public PathState GeneratePath(bool realtimeAStar = false)
    {
      int openQueueMaxSize = 0;
      Stopwatch timer = new Stopwatch();
      timer.Start();
      
      while(openList.Count > 0 && state == PathState.Searching)
      {
        //Comparator defines the values based on heuristic then x/y
        AStarNode currentNode = openList.Min;
        if(openList.Count > openQueueMaxSize)
          openQueueMaxSize = openList.Count;
  
        openList.Remove(currentNode);
        currentNode.state = NodeState.ClosedList;

        //If we are at the goal
        if(currentNode.pos == goalPos)
        {
          //Backtrack adding each node to the path from the start to the end
          AStarNode iterNode = currentNode;
          while(iterNode.pos != startPos)
          {
            path.Add(iterNode.pos);
            iterNode = iterNode.parent;
          }
          //breaks when iterNode.pos == startPos

          //Add the last node, the start pos
          path.Add(startPos);
          
          //Path is backwards so invert it
          path.Reverse();
          state = PathState.Found;
          break;
        }

        //Add neighbors to queue
        for (int i = -1; i <= 1; i++)
        {
          for (int j = -1; j <= 1; j++)
          {
            //Ignore center (this node)
            if((i == 0 && j == 0))
              continue;
            float cost = 1;

            if (Mathf.Abs(i) == 1 && Mathf.Abs(j) == 1)
            {
              //Sqrt 2
              cost = 1.41421356237f;
              //If we are diagonal then we want to make sure there is a way for the agent to fit through, dont want to pass through
              //# x
              //x #
              if(map.GetNodeAt(new Vector2(currentNode.pos.X + i,currentNode.pos.Y)).state == NodeState.WallNode
                && map.GetNodeAt(new Vector2(currentNode.pos.X,currentNode.pos.Y + j)).state == NodeState.WallNode )
              continue;
            }

            AStarNode iterNode = map.GetNodeAt(new Vector2(currentNode.pos.X + i,currentNode.pos.Y + j));
            if(iterNode.state == NodeState.WallNode || iterNode.state == NodeState.ClosedList)
              continue;

            //if not checked then update the heuristic
            if(iterNode.state == NodeState.NotChecked)
            {
              iterNode.UpdateHeuristic(goalPos);
              map.SetNodeAt(iterNode.pos, iterNode);
            }

            //If node is already on the open list,
            if(iterNode.state == NodeState.OpenList)
            {
              //Compare current node cost vs potential new parent cost
              if(iterNode.givenCost > currentNode.givenCost + cost)
              {
                //Need to reinsert the node because the sorted value will change, sometimes results in infinite loop
                openList.Remove(iterNode);
                
                //If the old node's parent is more expensive than the current node's parent then swap the parent thus updating the node to the cheapest path to the start
                iterNode.parent = currentNode;
                iterNode.givenCost = currentNode.givenCost + cost;
                map.SetNodeAt(iterNode.pos, iterNode);
                openList.Add(iterNode);
              }
            }
            else  //Not on open list then add to open list
            {
              iterNode.state = NodeState.OpenList;
              iterNode.parent = currentNode;
              iterNode.givenCost = currentNode.givenCost + cost;
              map.SetNodeAt(iterNode.pos, iterNode);
              openList.Add(iterNode);
            }
          }
        }

        //We only want to run 1 iteration
        if(realtimeAStar)
          break;
      }

      // if we exhausted the open list and havent found the path
      if(state == PathState.Searching && openList.Count == 0)
        state = PathState.DoesNotExist;

      timer.Stop();
      
      //More accurate to use total ms cause double
      //https://stackoverflow.com/questions/2329079/how-do-you-convert-stopwatch-ticks-to-nanoseconds-milliseconds-and-seconds
      if(!realtimeAStar)
        Console.WriteLine("A* pathfinding took {0:000000} ms with an Open Queue of max size {1}", timer.Elapsed.TotalMilliseconds, openQueueMaxSize);
      //If we get here then there is no path 
      return state;
    }
  }
}
