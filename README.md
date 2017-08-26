# unity-rendering-investigation

A basic performance investigation around a variety of rendering techniques within Unity

## Approaches
#### Unity Renderer MonoBehaviour
Every mesh gets its own `GameObject`, `MeshFilter`, and `Renderer` components.

#### Graphics.DrawMesh API
Transform matrices and materials are cached and all meshes are drawn using the `Graphics.DrawMesh()` function.

#### Graphics.DrawMesh API with MaterialPropertyBlock
Same as above, but one material is used and each mesh gets its own `MaterialPropertyBlock` to augment that material.

#### Graphics.DrawProcedural API
Each mesh is converted into two `ComputeBuffer`s for both indices and attributes which are referenced in the vertex shader. A material and matrix are cached for each mesh and rendered using the `Graphics.DrawProcedural()` function and `GL.PushMatrix()` to set the transform of the draw.

#### Graphics.DrawProcedural API with Unpacked Vertex Buffer
Same as above, but the attributes for the mesh are unpacked into a single `ComputeBuffer` with three vertices per triangle.
