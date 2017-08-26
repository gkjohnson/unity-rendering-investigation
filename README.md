# unity-rendering-investigation

A basic performance investigation around a variety of rendering techniques within Unity

## Approaches
#### Unity Renderer MonoBehaviour
Every mesh gets its own GameObject, MeshFilter, and Renderer components.

#### Graphics.DrawMesh API
Transform matrices and materials are cached and all meshes are drawn using the `Graphics.DrawMesh()` function

#### Graphics.DrawMesh API with MaterialPropertyBlock
_TODO_

#### Graphics.DrawProcedural API
_TODO_

#### Graphics.DrawProcedural API with Unpacked Vertex Buffer
_TODO_

## Comparisons
_TODO_
