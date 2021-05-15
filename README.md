# Voxel-Based Micro Worlds
 Genetic Algorithms, L-Systems, Heightmap Generation, Mesh Optimization, Voxels
 
<p align="center">
  <img src="https://github.com/calincraiu/Voxel-Based-Micro-Worlds/blob/main/images/Micro-Worlds-GIF.gif">
</p>
 
 ## Dependencies  
 <p align="justify">
 This project was created using the [Grasshopper SDK](https://developer.rhino3d.com/api/grasshopper/html/723c01da-9986-4db2-8f53-6f3a7494df75.htm) inside [Rhinoceros 3D](https://www.rhino3d.com/) (version: Rhino7). The language used to create the custom GH components is C#. 
 </p>
 
 
 Notes:  
 - Other Rhino versions might work, but it is important to mention that before Rhino 6.0, Grasshopper was not part of the standard toolkit and had to be downloaded as a plugin. 
 -  Upon opening the Grasshopper definition, the custom components will attempt to generate a "Voxel-Based Micro World", process which might slow down the file upon opening. Give it a few seconds. 
 -  The Grasshopper file creates a single .jpg in the directory that it is placed in when running. This is the generated heightmap to be used for Voxel-World generation.
 -  The provided code is structured in a way that is compatible with GH components. Objects are defined under "custom additional code" and called under "RUNTIME CODE".
 
 ## Relevant Files
The provided working file can be found inside [GH_definition](https://github.com/calincraiu/Voxel-Based-Micro-Worlds/tree/main/GH_definition).

If unable to run the GH file, the code for each component is provided separately in [component_scripts](https://github.com/calincraiu/Voxel-Based-Micro-Worlds/tree/main/component_scripts).  

 ## About  
 <p align="justify">
Micro Worlds as a code is largely focused on Heightmap generation through L-Systems optimized by a Genetic Algorithm. The mentioned heightmaps are in turn used to form a voxel-based terrain with associated assets. The point of this exercise is to create a system for procedural terrain/world generation that has the potential to retain specificity in user/designer input.
</p>   
<p align="justify">
Normally, procedural terrain generation is approached by implementing a noise function and layering octaves on top of it (examples being Perlin, Simplex, Pink, etc). This allows for control over the height, definition and detail of the terrain, as well as biome definition. However, using L-Systems for bitmap manipulation would allow for placement of landmasses at particular coordinates (x, y on the bitmap), as well as control their dimensions (via number of system iterations). If implemented without additional algorithmic intervention, a user could create a heightmap by repeatedly adding landmasses created by an L-System with complete control over their shape, size and location. 
</p>
<p align="justify">
However, this is laborious work, and therefore, in this implementation, Genetic algorithms have been used as a means of automating and optimizing the placement of said landmasses. A user would be prompted to specify a percentage and a terrain type to optimize, and the GA would provide a solution. There are numerous possible configurations of voxels in the parameter hyperspace, therefore the GA can provide different optimal solutions to the same input parameters.  
 </p>  
 
## End Notes  
<p align="justify">
This project was created close to the start of my programming journey, therefore the code could use some improvement. Feel free to contact me with suggestions.
</p>  
