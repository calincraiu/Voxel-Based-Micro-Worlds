# Voxel-Based Micro Worlds
 Genetic Algorithms, L-Systems, Heightmap Generation, Mesh Optimization, Voxels
 
 ## Dependencies  
 This project was created using the [Grasshopper SDK](https://developer.rhino3d.com/api/grasshopper/html/723c01da-9986-4db2-8f53-6f3a7494df75.htm) inside [Rhinoceros 3D](https://www.rhino3d.com/) (version: Rhino7). The language used to create the custom GH components is C#. 
 
 Notes:  
 - Other Rhino versions might work, but it is important to mention that before Rhino 6.0, Grasshopper was not part of the standard toolkit and had to be downloaded as a plugin.  
 -  Upon opening the Grasshopper definition, the custom components will attempt to generate a "Voxel-Based Micro World", process which might slow down the file upon opening. Give it a few seconds. 
 -  The Grasshopper file creates a single .jpg in the directory that it is placed in when running. This is the generated heightmap to be used for Voxel-World generation.
 
 ## Relevant Files
 
The provided working file can be found inside [GH_definition](https://github.com/calincraiu/Voxel-Based-Micro-Worlds/tree/main/GH_definition).

If unable to access the GH file, the code for each component is provided separately in [component_scripts](https://github.com/calincraiu/Voxel-Based-Micro-Worlds/tree/main/component_scripts).
