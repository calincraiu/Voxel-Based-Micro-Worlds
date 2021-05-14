using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { /* Implementation hidden. */ }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { /* Implementation hidden. */ }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { /* Implementation hidden. */ }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private readonly RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private readonly GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private readonly IGH_Component Component;
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private readonly int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments,
  /// Output parameters as ref arguments. You don't have to assign output parameters,
  /// they will have a default value.
  /// </summary>
  private void RunScript(List<Mesh> inputMesh, List<Mesh> inputTrees, string display, Point3d player, ref object worldMesh, ref object OutputTreeMeshes)
  {

    // RUNTIME CODE:

    WorldProcessing world = new WorldProcessing(inputMesh);

    if(voxelWorld != world)
    {
      voxelWorld = world;
    }


    voxelWorld.arrangeChunks(voxelWorld.chunks[voxelWorld.chunks.Count - 1].Vertices[0] != Point3d.Origin, 16);

    if(inputTrees != storedTreeMeshes)
    {
      storedTreeMeshes = inputTrees;
      treeChunkCorrelation = new Dictionary<int, List<Mesh>>();
    }


    // OUTPUT:

    if(display == "world")
    {
      worldMesh = voxelWorld.chunks;
      OutputTreeMeshes = inputTrees;
    }

    else if(display == "vicinity")
    {
      voxelWorld.createChunkCorrelation();
      voxelWorld.displayChunks(player);

      if(treeChunkCorrelation.Count == 0)
      {
        TreeUtility.AssignChunk(inputTrees);
      }

      worldMesh = voxelWorld.outputChunks;
      OutputTreeMeshes = TreeUtility.DisplayTrees(player);
    }



  }

  // <Custom additional code> 

  // CODE:

  public WorldProcessing voxelWorld;
  public List<Mesh> storedTreeMeshes;

  // This Dict holds a correlation between chunk index and chunk position. Will be used to associate tree meshes with chunks
  public static Dictionary<Point2d, int> chunkIndexCorrelation = new Dictionary<Point2d, int>();
  // This Dict holds a correlation between chunk and tree meshes
  public static Dictionary<int, List<Mesh>> treeChunkCorrelation = new Dictionary<int, List<Mesh>>();

  // Enum holding cardinal directions for determining chunk positions and neighbours
  public enum WorldDirection {South, East, North, West};


  // CLASS 1:
  // This class enables some functionality attributed to the world mesh after it is created
  public class WorldProcessing
  {
    // Fields
    public List<Mesh> chunks;
    public List<Mesh> outputChunks;

    private int storedChunkIndex;

    // Constructor
    public WorldProcessing(List<Mesh> inputMeshes)
    {
      this.chunks = inputMeshes;
    }

    // Methods

    // Move the chunks to position if they are not arranged
    public void arrangeChunks(bool inPosition, int chunkSize)
    {
      int chunkX = 0;
      int chunkY = 0;

      if(!inPosition)
      {
        for (int i = 0; i < chunks.Count; i++)
        {
          chunks[i].Transform(Transform.Translation(new Vector3d(chunkX * chunkSize, chunkY * chunkSize, 0)));

          chunkY += 1;

          if (chunkY >= Math.Sqrt(chunks.Count))
          {
            chunkY = 0;
            chunkX += 1;
          }
        }
      }
    }

    // Determine a correlation between chunk index and chunk position.
    public void createChunkCorrelation()
    {
      for (int i = 0; i < chunks.Count; i++)
      {
        chunkIndexCorrelation[new Point2d(chunks[i].GetBoundingBox(true).Center)] = i;
      }
    }

    // Control what chunks are displayed based on the player's position.
    public void displayChunks(Point3d player)
    {
      if(outputChunks == null)
      {
        outputChunks = new List<Mesh>();
      }

      // Clamp player position input
      if (player.X < 0) player = new Point3d(0, player.Y, 0);
      if (player.X > Math.Sqrt(chunks.Count) * 16) player = new Point3d(Math.Sqrt(chunks.Count) * 16, player.Y, 0);
      if (player.Y < 0) player = new Point3d(player.X, 0, 0);
      if (player.Y > Math.Sqrt(chunks.Count) * 16) player = new Point3d(player.X, Math.Sqrt(chunks.Count) * 16, 0);

      // Estimate the origin position of the chunk that the player is currently in
      Point3d currentChunkPosition = new Point3d(player.X - (player.X % 16), player.Y - (player.Y % 16), 0);

      // Determine the index of the current chunk based on its position
      int currentChunkIndex = (int) ((currentChunkPosition.X / 16) * Math.Sqrt(chunks.Count) + currentChunkPosition.Y / 16);

      if(currentChunkIndex != storedChunkIndex)
      {
        storedChunkIndex = currentChunkIndex;
        outputChunks.Clear();

        // Find all the neighbouring chunks of the current chunk
        List<int> neighbourIndices = ChunkUtility.neigbourChunks(currentChunkIndex, chunks.Count);

        foreach(int index in neighbourIndices)
        {
          outputChunks.Add(chunks[index]);
        }

        outputChunks.Add(chunks[currentChunkIndex]);
      }

    }

  }



  // CLASS 2:
  // Static class that enables checking chunk indices and neighbours
  public static class ChunkUtility
  {
    // Method that determines whether a chunk is at the edge of the world or not
    public static bool isAtEdge(int index, int listLength, WorldDirection  direction)
    {
      if(index < 0 || index > listLength) return true;

      int cutoff = (int) Math.Sqrt(listLength);

      if(direction == WorldDirection.South)
      {
        return (index - cutoff < 0);
      }

      else if(direction == WorldDirection.East)
      {
        return (index % cutoff == cutoff - 1);
      }

      else if(direction == WorldDirection.North)
      {
        return (index + cutoff > listLength);
      }

      else if(direction == WorldDirection.West)
      {
        return (index % cutoff == 0);
      }

      return false;
    }

    // Method that returns the indices of the neighbouring chunks
    public static List<int> neigbourChunks(int currentChunk, int listLength)
    {
      List<int> neighbours;

      int cutoff = (int) Math.Sqrt(listLength);

      // Based on a chunk's position, determine what neighbours it has
      // Positions include: on the sides of the world, in the corners of the world, in the middle of the world

      if(isAtEdge(currentChunk, listLength, WorldDirection.South))
      {
        if(isAtEdge(currentChunk, listLength, WorldDirection.West))
        {
          neighbours = new List<int>() {currentChunk + cutoff, currentChunk + 1, currentChunk + cutoff + 1};
        }

        else if(isAtEdge(currentChunk, listLength, WorldDirection.East))
        {
          neighbours = new List<int>() {currentChunk - 1, currentChunk + cutoff, currentChunk + cutoff - 1};
        }

        else
        {
          neighbours = new List<int>() {currentChunk - 1, currentChunk + 1, currentChunk + cutoff, currentChunk + cutoff + 1, currentChunk + cutoff - 1};
        }
      }

      else if(isAtEdge(currentChunk, listLength, WorldDirection.North))
      {
        if(isAtEdge(currentChunk, listLength, WorldDirection.West))
        {
          neighbours = new List<int>() {currentChunk - cutoff, currentChunk + 1, currentChunk + cutoff + 1};
        }

        else if(isAtEdge(currentChunk, listLength, WorldDirection.East))
        {
          neighbours = new List<int>() {currentChunk - 1, currentChunk - cutoff, currentChunk - cutoff - 1};
        }

        else
        {
          neighbours = new List<int>() {currentChunk - 1, currentChunk + 1, currentChunk - cutoff, currentChunk - cutoff + 1, currentChunk - cutoff - 1};
        }
      }

      else if(isAtEdge(currentChunk, listLength, WorldDirection.West))
      {
        neighbours = new List<int>() {currentChunk + cutoff, currentChunk - cutoff, currentChunk + 1, currentChunk - cutoff + 1, currentChunk + cutoff + 1};
      }

      else if(isAtEdge(currentChunk, listLength, WorldDirection.East))
      {
        neighbours = new List<int>() {currentChunk + cutoff, currentChunk - cutoff, currentChunk - 1, currentChunk - cutoff - 1, currentChunk + cutoff - 1};
      }

      else
      {
        neighbours = new List<int>() {currentChunk - 1, currentChunk + 1, currentChunk - cutoff, currentChunk + cutoff, currentChunk - cutoff - 1, currentChunk - cutoff + 1, currentChunk + cutoff - 1, currentChunk + cutoff + 1};
      }

      return neighbours;
    }
  }



  // CLASS 2: TREE UTILITY
  // This class assigns the input tree meshes to a chunk using indices and Point2d positions
  // stored in the Dicts at the start of "Custom additional code"
  public static class TreeUtility
  {
    // Methods

    // This method iterates through input trees and determines their associated chunks
    public static void AssignChunk(List<Mesh> trees)
    {
      for (int i = 0; i < trees.Count; i++)
      {
        Point2d currentTreePosition = new Point2d(trees[i].GetBoundingBox(true).Center);

        Point2d associatedChunkPosition = new Point2d(currentTreePosition.X - (currentTreePosition.X % 16) + 8, currentTreePosition.Y - (currentTreePosition.Y % 16) + 8);
        int associatedChunkIndex = chunkIndexCorrelation[associatedChunkPosition];


        if(!treeChunkCorrelation.ContainsKey(associatedChunkIndex))
        {
          treeChunkCorrelation.Add(associatedChunkIndex, new List<Mesh>());
        }

        treeChunkCorrelation[associatedChunkIndex].Add(trees[i]);
      }
    }

    // Display the trees in the vicinity of the player
    public static List<Mesh> DisplayTrees(Point3d player)
    {
      List<Mesh> vicinityTrees = new List<Mesh>();

      // Estimate the origin position of the chunk that the player is currently in
      Point3d currentChunkPosition = new Point3d(player.X - (player.X % 16), player.Y - (player.Y % 16), 0);

      // Determine the index of the current chunk based on its position
      int currentChunkIndex = (int) ((currentChunkPosition.X / 16) * 8 + currentChunkPosition.Y / 16);

      List<int> neighbours = ChunkUtility.neigbourChunks(currentChunkIndex, 64);
      neighbours.Add(currentChunkIndex);

      foreach (int index in neighbours)
      {
        if(treeChunkCorrelation.ContainsKey(index))
        {
          vicinityTrees.AddRange(treeChunkCorrelation[index]);
        }
      }

      return vicinityTrees;
    }
  }



  // </Custom additional code> 
}