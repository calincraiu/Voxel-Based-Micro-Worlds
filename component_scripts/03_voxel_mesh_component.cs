using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.IO;
using System.Drawing;
using Rhino.Geometry.Intersect;

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
  private void RunScript(double terrainHeightScaling, string path, ref object VoxelWorldMesh, ref object ForestMeshes)
  {

    // TERMINOLOGY:

    // "VOXEL" in this project is the base building block. It holds information, such as its associated mesh, its neighbours, etc.
    // "CHUNK" in this project is a group of voxels (either present or absent) that for a larger sector of the world. Chunk size is 16 x 16 x 128 voxels
    // "WORLD" in this project is the group of all chunks that have been created.

    // RUNTIME CODE:

    voxelBinaryData.Clear();
    worldData.Clear();

    HeightMap heightMap = new HeightMap(path);
    // Get voxel binary arrays from generated heightmap
    voxelBinaryData = heightMap.generateWorldData(16, terrainHeightScaling);
    // Generate world data
    for (int i = 0; i < voxelBinaryData.Count; i++)
    {
      worldData.Add(new VoxelChunkData(voxelBinaryData[i]));
    }

    // Create the world from provided world data
    World voxelWorld = new World(worldData, terrainHeightScaling);

    // Populate the world with assets
    generatedForest = new Forest(heightMap.finalMap, terrainHeightScaling);

    // OUTPUT
    VoxelWorldMesh = voxelWorld.chunkMehses;
    ForestMeshes = generatedForest.trees;

  }

  // <Custom additional code> 

  // REFERENCES:
  // Initial inspiration for data structuring used in the VoxelChunkData class came from Board To Bits Games.
  // Source: https://www.youtube.com/watch?v=b_1ZlHrJZc4

  // FIELDS:

  public static Random rnd = new Random();
  public Forest generatedForest;

  // Store the voxel data
  public List<int[,,]> voxelBinaryData = new List<int[,,]>();
  public List<VoxelChunkData> worldData = new List<VoxelChunkData>();


  // METHODS:

  // METHOD 1: REMAP VALUE
  // Static method used to remap image brightness value to voxel chunk height data
  public static int Remap(float brightness, int newCap, double scalingFactor)
  {
    return (int) (0 + (brightness - 0) * (newCap * scalingFactor - 0) / (1 - 0));
  }


  // METHOD 2: RESIZE IMAGE
  // Static method that resizes an image to a new specified size
  public static Bitmap ResizeImage(Bitmap image, int newSize)
  {
    Bitmap resizedImage = new Bitmap(image, new Size(newSize, newSize));

    return resizedImage;
  }


  // CLASSES:

  // PART 1: CLASSES THAT DEAL WITH DATA PREPARATION


  // CLASS 1: CREATE DATA FOR VOXEL GENERATION
  // Class that uses the base height map stored in ImageFiles to create a data structure to be passed on to the voxel generator
  public class HeightMap
  {
    // Fields
    public Bitmap finalMap;
    public string path;

    // Constructor
    public HeightMap(string path)
    {
      this.path = path;
      finalMap = ResizeImage(new Bitmap(path), 128);
    }

    // Methods

    // This method splits the received image into equal tiles of a specified size
    // In the Voxel Chunk component, each tile will be used to generate a separate voxel chunk
    public List<Bitmap> createTiles(Bitmap map, int tileSize)
    {
      List<Bitmap> imageTiles = new List<Bitmap>();

      int imgHeight = map.Height;
      int imgWidth = map.Width;

      int numberOfTiles = (int) (imgHeight / tileSize);

      for (int i = 0; i < numberOfTiles; i++)
      {
        for (int j = 0; j < numberOfTiles; j++)
        {
          Rectangle copyRect = new Rectangle(i * tileSize, j * tileSize, tileSize, tileSize);
          System.Drawing.Imaging.PixelFormat format = map.PixelFormat;
          Bitmap newTile = map.Clone(copyRect, format);

          imageTiles.Add(newTile);
        }
      }

      return imageTiles;
    }


    // This method generates a data array of binary values to represent present(1) or absent(0) voxels
    public int[,,] generateDataArray(Bitmap map, double heightScaling)
    {
      // The supplied image should always have the same Height and Width
      int[,,] dataArray = new int[map.Width, map.Height, 128];

      for (int x = 0; x < map.Width; x++)
      {
        for (int y = 0; y < map.Height; y++)
        {
          // The number of voxels in the Z direction is determined by the brightness of the pixel at the (X, Y) coordinates
          Color pixelColor = map.GetPixel(x, y);
          int pixelBrightness = Remap(pixelColor.GetBrightness(), map.Height, heightScaling);

          for (int z = 0; z < 128; z++)
          {
            // If the current position in the array is higher than the brightness, create an empty voxel
            if(z > pixelBrightness) dataArray[x, y, z] = 0;

            else dataArray[x, y, z] = 1;

          }
        }
      }

      return dataArray;
    }


    // This method generates a list of data arrays (one for each chunk)
    public List<int[,,]> generateWorldData(int tileSize, double heightScaling)
    {
      List<Bitmap> imageTiles = createTiles(finalMap, tileSize);

      List<int[,,]> worldData = new List<int[,,]>();

      for (int i = 0; i < imageTiles.Count; i++)
      {
        worldData.Add(generateDataArray(imageTiles[i], heightScaling));
      }

      return worldData;
    }

  }




  // PART 2: CLASSES THAT DEAL WITH VOXEL, CHUNK & WORLD MESH GENERATION


  // This enum contains the orientation of the faces of a voxel in the orde they are created
  public enum Direction {Bottom, South, East, North, West, Top}

  // This enum contains the different types of voxels that can exist
  public enum VoxelType {Grass, Stone, Snow, Water, NotImplemented}



  // CLASS 1: WORLD
  // This class enables the creation of a voxel world out of multiple chunks
  public class World
  {
    // Fields
    private List<VoxelChunkMesh> chunks;
    public List<Mesh> chunkMehses;

    // Constructor
    public World(List<VoxelChunkData> chunksData, double heightScaling)
    {
      chunks = new List<VoxelChunkMesh>();
      chunkMehses = new List<Mesh>();

      int chunkX = 0;
      int chunkY = 0;

      for (int i = 0; i < chunksData.Count; i++)
      {
        Point3d chunkOrigin = new Point3d(chunkX * 16, chunkY * 16, 0);

        chunkY += 1;

        if (chunkY >= Math.Sqrt(chunksData.Count))
        {
          chunkY = 0;
          chunkX += 1;
        }

        VoxelChunkMesh newChunk = new VoxelChunkMesh(chunksData[i], heightScaling);

        chunks.Add(newChunk);
        chunkMehses.Add(newChunk.mesh);
      }
    }

  }


  // CLASS 2: VOXEL CHUNK DATA
  // This class holds the data for a cluster of voxels, which will be used to create a mesh
  // It holds an array that determines in what positions voxels exist (1) or do not exist (0)
  public class VoxelChunkData
  {
    // Fields
    public int[,,] data;

    // orientations holds the data that determines in which direction "North", "South", "West" etc are in relation to a voxel
    VoxelCoordinates[] orientations = {
      new VoxelCoordinates (0, 0, -1),
      new VoxelCoordinates (0, -1, 0),
      new VoxelCoordinates (1, 0, 0),
      new VoxelCoordinates (0, 1, 0),
      new VoxelCoordinates (-1, 0, 0),
      new VoxelCoordinates (0, 0, 1)
      };

    // Constructor
    public VoxelChunkData(int[,,] receivedData)
    {
      this.data = receivedData;
    }

    // Methods

    // Get the number of elements in the first dimension of the array (width)
    public int Width
    {
      get {return data.GetLength(0);}
    }

    // Get the number of elements in the second dimension of the array (depth)
    public int Depth
    {
      get {return data.GetLength(1);}
    }

    // Get the number of elements in the third dimension of the array (height)
    public int Height
    {
      get {return data.GetLength(2);}
    }

    // Get the voxel data at the specified position in the array
    public int GetVoxel(int x, int y, int z)
    {
      return data[x, y, z];
    }

    // Check the neighbouring voxel of the specified voxel, in the specified direction.
    public int CheckNeighbour(int x, int y, int z, Direction direction)
    {
      VoxelCoordinates currentVoxelCheck = orientations[(int) direction];
      VoxelCoordinates neighbourCoordinates = new VoxelCoordinates(x + currentVoxelCheck.x, y + currentVoxelCheck.y, z + currentVoxelCheck.z);

      // An element is out of bounds if the neighbour coordinates are smaller than 0 or larger than the dimensions of the data array
      bool isOutOfBounds = (
        neighbourCoordinates.x < 0 || neighbourCoordinates.x >= Width ||
        neighbourCoordinates.y < 0 || neighbourCoordinates.y >= Depth ||
        neighbourCoordinates.z < 0 || neighbourCoordinates.z >= Height
        );

      if(isOutOfBounds) return 0;
      else return GetVoxel(neighbourCoordinates.x, neighbourCoordinates.y, neighbourCoordinates.z);
    }

    // Structure that holds the voxel coordinates information (in terms of position in data array, not world coordinates)
    struct VoxelCoordinates
    {
      public int x;
      public int y;
      public int z;

      public VoxelCoordinates(int x, int y, int z)
      {
        this.x = x;
        this.y = y;
        this.z = z;
      }
    }
  }


  // CLASS 3: VOXEL CHUNK MESH
  // This class creates a mesh from all the voxel data it received as an input
  public class VoxelChunkMesh
  {
    // Fields
    public VoxelChunkData voxelData;
    public Mesh mesh;


    // Constructor
    public VoxelChunkMesh(VoxelChunkData voxelData, double heightScaling)
    {
      this.voxelData = voxelData;

      mesh = new Mesh();

      // When iterating through the voxelData, keep track of what the current voxel index is
      int voxelIndex = 0;

      for (int z = 0; z < voxelData.Height; z++)
      {
        for (int y = 0; y < voxelData.Depth; y++)
        {
          for (int x = 0; x < voxelData.Width; x++)
          {
            if(voxelData.GetVoxel(x, y, z) == 1)
            {
              Point3d voxelOrigin = new Point3d(x, y, z);
              MakeVoxel(voxelOrigin, voxelIndex, heightScaling);

              voxelIndex += 1;
            }
          }
        }
      }
    }

    // Methods

    // This method creates a voxel/cube mesh with the specified parameters
    public void MakeVoxel(Point3d position, int voxelIndex, double heightScaling)
    {
      int chance = rnd.Next(0, 101);

      // Check the neighbours of the current voxel in order to determine what faces to add
      // If the voxel is next to another voxel, there is no point in creating a face in between them
      List<int> voxelNeighbours = new List<int>();
      voxelNeighbours.Add(voxelData.CheckNeighbour((int) position.X, (int) position.Y, (int) position.Z, Direction.Bottom));
      voxelNeighbours.Add(voxelData.CheckNeighbour((int) position.X, (int) position.Y, (int) position.Z, Direction.South));
      voxelNeighbours.Add(voxelData.CheckNeighbour((int) position.X, (int) position.Y, (int) position.Z, Direction.East));
      voxelNeighbours.Add(voxelData.CheckNeighbour((int) position.X, (int) position.Y, (int) position.Z, Direction.North));
      voxelNeighbours.Add(voxelData.CheckNeighbour((int) position.X, (int) position.Y, (int) position.Z, Direction.West));
      voxelNeighbours.Add(voxelData.CheckNeighbour((int) position.X, (int) position.Y, (int) position.Z, Direction.Top));

      // Create the vertices for the current voxel's mesh
      mesh.Vertices.AddVertices(Voxel.voxelVertices(position));
      // Create the faces for the current voxel's mesh, based on what neighbours it has
      mesh.Faces.AddFaces(Voxel.voxelFaces(voxelIndex, position, voxelNeighbours));
      // Add colours to the voxels based on their types (Stone, Grass, etc)
      mesh.VertexColors.AppendColors(Voxel.vertexColours(Voxel.assignVoxelType(position, voxelData.Height, heightScaling * 7, chance)));
    }
  }


  // CLASS 4: VOXEL
  // This static class holds the functionality required to create a single voxel/cube mesh
  public static class Voxel
  {
    // Methods

    // This method returns the 8 vertices of a voxel, based on the voxel's position
    public static Point3d[] voxelVertices(Point3d position)
    {
      // Define the position of the 8 vertices of the voxel/cube
      Point3d point1 = new Point3d(position.X, position.Y, position.Z);
      Point3d point2 = new Point3d(position.X + 1, position.Y, position.Z);
      Point3d point3 = new Point3d(position.X + 1, position.Y + 1, position.Z);
      Point3d point4 = new Point3d(position.X, position.Y + 1, position.Z);
      Point3d point5 = new Point3d(position.X, position.Y, position.Z + 1);
      Point3d point6 = new Point3d(position.X + 1, position.Y, position.Z + 1);
      Point3d point7 = new Point3d(position.X + 1, position.Y + 1, position.Z + 1);
      Point3d point8 = new Point3d(position.X, position.Y + 1, position.Z + 1);

      Point3d[] vertices = {point1, point2, point3, point4, point5, point6, point7, point8};

      return vertices;
    }

    // This method returns the 6 faces of a voxel, based on the voxel's index in the chunk data structure
    public static MeshFace[] voxelFaces(int voxelIndex, Point3d position, List<int> neighbours)
    {
      List<MeshFace> faces = new List<MeshFace>();

      // Define the 6 faces of the voxel/cube and add them to the list of faces if they are exposed
      // (If a face is hidden in between 2 voxels, do not add it)
      if(neighbours[0] == 0 && position.Z > 0)
      {
        MeshFace face1 = new MeshFace(0 + voxelIndex * 8, 3 + voxelIndex * 8, 2 + voxelIndex * 8, 1 + voxelIndex * 8); // Bottom face
        faces.Add(face1);
      }

      if(neighbours[1] == 0)
      {
        MeshFace face2 = new MeshFace(0 + voxelIndex * 8, 1 + voxelIndex * 8, 5 + voxelIndex * 8, 4 + voxelIndex * 8); // South face
        faces.Add(face2);
      }

      if(neighbours[2] == 0)
      {
        MeshFace face3 = new MeshFace(1 + voxelIndex * 8, 2 + voxelIndex * 8, 6 + voxelIndex * 8, 5 + voxelIndex * 8); // East face
        faces.Add(face3);
      }

      if(neighbours[3] == 0)
      {
        MeshFace face4 = new MeshFace(2 + voxelIndex * 8, 3 + voxelIndex * 8, 7 + voxelIndex * 8, 6 + voxelIndex * 8); // North face
        faces.Add(face4);
      }

      if(neighbours[4] == 0)
      {
        MeshFace face5 = new MeshFace(3 + voxelIndex * 8, 0 + voxelIndex * 8, 4 + voxelIndex * 8, 7 + voxelIndex * 8); // West face
        faces.Add(face5);
      }

      if(neighbours[5] == 0)
      {
        MeshFace face6 = new MeshFace(4 + voxelIndex * 8, 5 + voxelIndex * 8, 6 + voxelIndex * 8, 7 + voxelIndex * 8); // Top face
        faces.Add(face6);
      }

      return faces.ToArray();
    }

    // Method that adds vertex colors to the mesh
    public static Color[] vertexColours(VoxelType voxelType)
    {
      Color voxelColour;
      Color[] vertexColours = new Color[8];

      if (voxelType == VoxelType.Grass)
      {
        voxelColour = Color.Green;
      }

      else if (voxelType == VoxelType.Stone)
      {
        voxelColour = Color.Gray;
      }

      else if (voxelType == VoxelType.Snow)
      {
        voxelColour = Color.Snow;
      }

      else if (voxelType == VoxelType.Water)
      {
        voxelColour = Color.Blue;
      }

      else
      {
        voxelColour = Color.Empty;
      }

      for (int i = 0; i < 8; i++)
      {
        vertexColours[i] = voxelColour;
      }

      return vertexColours;
    }

    // Method that determines the type of the voxel -> used to color the vertices of individual voxels in vertexColours method
    public static VoxelType assignVoxelType(Point3d position, int chunkHeight, double cutOff, int chance)
    {
      VoxelType voxelType;

      int currentVoxelHeight = (int) position.Z;

      if (currentVoxelHeight <= 0)
      {
        voxelType = VoxelType.Water;
      }

      else if (currentVoxelHeight < (int) (chunkHeight / cutOff - 1) && currentVoxelHeight > 0)
      {
        voxelType = VoxelType.Grass;
      }

      else if (currentVoxelHeight >= (int) (chunkHeight / cutOff - 1) && currentVoxelHeight < (int) (chunkHeight / cutOff + 1) )
      {
        if (chance < 66) voxelType = VoxelType.Grass;
        else voxelType = VoxelType.Stone;
      }

      else if ((currentVoxelHeight >= (int) (chunkHeight / cutOff + 1)) && (currentVoxelHeight < (int) (chunkHeight * 2 / cutOff - 1)))
      {
        voxelType = VoxelType.Stone;
      }

      else if ((currentVoxelHeight >= (int) (chunkHeight * 2 / cutOff - 1)) && (currentVoxelHeight < (int) (chunkHeight * 2 / cutOff + 1)))
      {
        if (chance < 66) voxelType = VoxelType.Stone;
        else voxelType = VoxelType.Snow;
      }

      else
      {
        voxelType = VoxelType.Snow;
      }

      return voxelType;
    }
  }



  // PART 3: CLASSES THAT DEAL WITH POPULATING THE WORLD WITH ASSETS (SUCH AS FORESTS)

  // CLASS 1: TREE
  // Create a mesh tree to be used in the Forest class
  public class Tree
  {
    // Fields
    public Mesh treeMesh = new Mesh();

    public Point3d treeLocation;
    public double height;
    public Color crownColour;

    // Constructor
    public Tree(Point3d origin, double height, Color crownColour)
    {
      this.treeLocation = new Point3d(origin.X + 0.5, origin.Y + 0.5, origin.Z + 1);
      this.height = height;
      this.crownColour = crownColour;

      Point3d trunkTop = new Point3d(treeLocation.X, treeLocation.Y, treeLocation.Z + height);
      Line trunkLine = new Line(treeLocation, trunkTop);
      Cylinder trunkCylinder = new Cylinder(new Circle(treeLocation, 0.1), height);

      Mesh trunk = Mesh.CreateFromCylinder(trunkCylinder, 1, 5);

      foreach (var vertex in trunk.Vertices)
      {
        trunk.VertexColors.Add(Color.Brown);
      }

      double crownRadius = height * 0.55;
      Sphere crownSphere = new Sphere(trunkTop, crownRadius);

      Mesh crown = Mesh.CreateFromSphere(crownSphere, 10, 10);

      foreach (var vertex in crown.Vertices)
      {
        crown.VertexColors.Add(crownColour);
      }

      treeMesh.Append(trunk);
      treeMesh.Append(crown);

    }

  }



  // CLASS 2: FOREST
  // This class creates forests based on an input heightmap
  public class Forest
  {
    // Fields
    public Bitmap terrainMap;
    public List<Mesh> trees = new List<Mesh>();

    // Constructor
    public Forest(Bitmap terrainMap, double heightScaling)
    {
      // Create trees with different parameters based on the height of their location
      this.terrainMap = terrainMap;

      Random rnd = new Random();

      for (int x = 0; x < terrainMap.Width; x++)
      {
        for (int y = 0; y < terrainMap.Height; y++)
        {
          Color pixelColor = terrainMap.GetPixel(x, y);
          int pixelBrightness = Remap(pixelColor.GetBrightness(), terrainMap.Height, heightScaling);

          if(pixelBrightness > (128 / (heightScaling * 7)) && pixelBrightness < ((128 * 2) / (heightScaling * 3)))
          {
            if(rnd.NextDouble() < 0.09)
            {
              Color crownColour = Color.FromArgb(rnd.Next(75, 125), rnd.Next(150, 200), rnd.Next(70, 100));

              Tree newTree = new Tree(new Point3d(x, y, (int) (pixelBrightness / 8)), rnd.Next(1, 3), crownColour);
              trees.Add(newTree.treeMesh);
            }
          }

          else if (pixelBrightness > ((128 * 2) / (heightScaling * 3)) && pixelBrightness < (128 / heightScaling))
          {
            if(rnd.NextDouble() < 0.035)
            {
              Color crownColour = Color.FromArgb(rnd.Next(195, 200), rnd.Next(150, 200), rnd.Next(70, 100));

              Tree newTree = new Tree(new Point3d(x, y, (int) (pixelBrightness * 2 / (heightScaling * 8))), rnd.Next(1, 3), crownColour);
              trees.Add(newTree.treeMesh);
            }
          }

        }
      }
    }

  }

  // </Custom additional code> 
}