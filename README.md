# unity-rendering-investigation

A basic performance investigation around a variety of rendering techniques within Unity.

## Approaches
### Unity Renderer MonoBehaviour
Every mesh gets its own _GameObject_, _MeshFilter_, and _Renderer_ components.

### Graphics.DrawMesh API
Transform matrices and materials are cached and all meshes are drawn using the `Graphics.DrawMesh()` function.

##### Details
No overhead and management of _Renderers_ and _MeshFilters_.

### Graphics.DrawMesh API with MaterialPropertyBlock
Same as above, but one material is used and each mesh gets its own _MaterialPropertyBlock_ to augment that material.

##### Details
_MaterialPropertyBlock_ is supposed to be more efficient, but seems to take more time render.

It's possible that this is just more memory efficient. Results in the same number of set pass calls and batches.

### Graphics.DrawProcedural API
Each mesh is converted into two _ComputeBuffers_ for both indices and attributes which are referenced in the vertex shader. A material and matrix are cached for each mesh and rendered using the `Graphics.DrawProcedural()` function and `GL.PushMatrix()` to set the transform of the draw.

Each material takes both that "points" and "attributes" compute buffers as parameters.

##### Details
Despite many draws, the Unity stats window only displays that two "draw calls" are being made.

Every draw requires a new set pass call.

Because the mesh is procedural this will by-pass all of Unity's internal calculations rendering logic like frustum culling.

Attributes can be stored in custom formats and unpacked in the vertex shader to save on memory.

### Graphics.DrawProcedural API with Unpacked Vertex Buffer
Same as above, but the attributes for the mesh are unpacked into a single _ComputeBuffer_ with three vertices per triangle.

##### Details
Only pro might be that there is less array access in the vertex shader.

This approach takes more memory and transforms more vertices.

## Other Concepts
### Single ComputeBuffer for All Meshes
One compute buffer could be used to store all the attributes for all meshes with an offset buffer to help address a specific point in the attribute buffer to render. Multiple meshes could then be drawn by instancing and the instanceId can be used to address the specific mesh to draw.

The biggest issue is that when using `DrawProcedura()`, you have to specify the amount of vertices, which means every mesh must be the same size.

### Dynamic ComputeBuffer
Decide which triangles should be drawn and generate the data for the compute buffer that will be drawn.
