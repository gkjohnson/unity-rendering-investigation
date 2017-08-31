using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public abstract class RenderingApproach
{
    virtual public void Prepare(GameObject model) { }
    virtual public void SetEnabled(bool enabled) { }
    virtual public void Render(Camera cam = null, Transform root = null) { }
    virtual public void Dispose() { }
    virtual public void OnGUI() { }
}

// Use the basic Unity Renderer component
public class RendererTest : RenderingApproach
{
    GameObject go;

    public override void Prepare(GameObject model)
    {
        Dictionary<Color, Material> matDict = new Dictionary<Color, Material>();
        Shader shader = Shader.Find("Basic Shader");
        go = Object.Instantiate(model);

        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            Color32 col = r.sharedMaterial.color;
            if (!matDict.ContainsKey(col))
            {
                Material mat = new Material(shader);
                mat.color = col;

                matDict[col] = mat;
            }

            r.sharedMaterial = matDict[col];
        }
    }

    public override void SetEnabled(bool state)
    {
        go.SetActive(state);
    }

    public override void Dispose()
    {
        Object.Destroy(go);
    }
}

// Use the Graphics.DrawMesh() API
public class DrawMeshTest : RenderingApproach
{
    struct DrawSet
    {
        public Material material;
        public Mesh mesh;
        public Matrix4x4 localMat;
    }

    DrawSet[] _drawArray;

    public override void Prepare(GameObject model)
    {
        List<DrawSet> drawList = new List<DrawSet>();
        Dictionary<Color, Material> matDict = new Dictionary<Color, Material>();
        Shader shader = Shader.Find("Basic Shader");

        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            Mesh mesh = r.GetComponent<MeshFilter>().sharedMesh;
            Transform transform = r.transform;

            Color col = r.sharedMaterial.color;
            if (!matDict.ContainsKey(col))
            {
                Material mat = new Material(shader);
                mat.color = col;
                matDict.Add(col, mat);
            }

            drawList.Add(new DrawSet()
            {
                material = matDict[col],
                mesh = mesh,
                localMat = transform.localToWorldMatrix
            });
        }

        _drawArray = drawList.ToArray();
    }

    public override void Render(Camera cam = null, Transform root = null)
    {
        for(int i = 0; i < _drawArray.Length; i++)
        {
            DrawSet ds = _drawArray[i];
            Graphics.DrawMesh(ds.mesh, ds.localMat, ds.material, 0);
        }
    }
}

// Use the Graphics.DrawMesh() API with MaterialPropertyBlocks
public class DrawMeshWithPropBlockTest : RenderingApproach
{
    struct DrawSet
    {
        public MaterialPropertyBlock propBlock;
        public Mesh mesh;
        public Matrix4x4 localMat;
    }

    DrawSet[] _drawArray;
    Material _mat;

    public override void Prepare(GameObject model)
    {
        List<DrawSet> drawList = new List<DrawSet>();
        Shader shader = Shader.Find("Basic Shader Per Renderer");
        _mat = new Material(shader);

        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            Mesh mesh = r.GetComponent<MeshFilter>().sharedMesh;
            Transform transform = r.transform;

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", r.sharedMaterial.color);

            drawList.Add(new DrawSet()
            {
                propBlock = mpb,
                mesh = mesh,
                localMat = transform.localToWorldMatrix
            });
        }

        _drawArray = drawList.ToArray();
    }

    public override void Render(Camera cam = null, Transform root = null)
    {
        for (int i = 0; i < _drawArray.Length; i++)
        {
            DrawSet ds = _drawArray[i];
            Graphics.DrawMesh(ds.mesh, ds.localMat, _mat, 0, null, 0, ds.propBlock);
        }
    }
}

// Use the Graphics.DrawProcedural API with separate
// buffers for both the indices and attributes
public class DrawProceduralTest : RenderingApproach
{
    struct DrawSet
    {
        public Material material;
        public ComputeBuffer idxsBuffer;
        public ComputeBuffer attrBuffer;
        public Matrix4x4 localMat;
        public int count;
    }

    DrawSet[] drawArray;

    protected virtual void GetBuffers(Mesh mesh, ref ComputeBuffer idxbuff, ref ComputeBuffer attrbuff, ref int count)
    {
        ImportStructuredBufferMesh.Import(mesh, ref idxbuff, ref attrbuff);
        count = idxbuff.count;
    }

    protected virtual Shader GetShader() {
        return Shader.Find("Indirect Shader");
    }

    public override void Prepare(GameObject model)
    {
        List<DrawSet> drawList = new List<DrawSet>();
        Shader shader = GetShader();
        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            Mesh mesh = mf.sharedMesh;
            ComputeBuffer idxbuff = null;
            ComputeBuffer attrbuff = null;
            int count = 0;

            GetBuffers(mesh, ref idxbuff, ref attrbuff, ref count);

            Material mat = new Material(shader);
            mat.SetBuffer("indices", idxbuff);
            mat.SetBuffer("points", attrbuff);
            mat.color = r.sharedMaterial.color;

            drawList.Add(new DrawSet()
            {
                material = mat,
                idxsBuffer = idxbuff,
                attrBuffer = attrbuff,
                count = count,
                localMat = r.transform.localToWorldMatrix
            });
        }

        drawArray = drawList.ToArray();
    }

    public override void Render(Camera cam = null, Transform root = null)
    {
        for (int i = 0; i < drawArray.Length; i++)
        {
            DrawSet ds = drawArray[i];
            GL.PushMatrix();
            GL.MultMatrix(ds.localMat);
            ds.material.SetPass(0);
            Graphics.DrawProcedural(MeshTopology.Triangles, ds.count, 1);
            GL.PopMatrix();
        }
    }

    public override void Dispose()
    {
        for (int i = 0; i < drawArray.Length; i++)
        {
            if(drawArray[i].idxsBuffer != null) drawArray[i].idxsBuffer.Dispose();
            if(drawArray[i].attrBuffer != null) drawArray[i].attrBuffer.Dispose();
        }
    }
}

// Use the Graphics.DrawProcedural API with separate
// buffers for both the indices and attributes
public class UnpackedDrawProceduralTest : DrawProceduralTest
{
    protected override void GetBuffers(Mesh mesh, ref ComputeBuffer idxbuff, ref ComputeBuffer attrbuff, ref int count)
    {
        idxbuff = null;
        ImportStructuredBufferMesh.ImportAndUnpack(mesh, ref attrbuff);
        count = attrbuff.count;
    }

    protected override Shader GetShader()
    {
        return Shader.Find("Unpacked Indirect Shader");
    }
}

// Use the Graphics.DrawProcedural API with separate
// buffers for both the indices and attributes
public class VisibleTriangleRenderTest : RenderingApproach
{
    struct OtherAttrs
    {
        public Matrix4x4 matrix;
        public Color color;
    }

    const int OC_RESOLUTION = 1024;
    const int MAX_TRIANGLES = 100000;
    RenderTexture octex;
    Camera occam;

    const int ACCUM_KERNEL = 0;
    const int CLEAR_KERNEL = 1;
    const int MAP_KERNEL = 2;
    ComputeShader compute;
    ComputeBuffer idaccum, trilist;
    uint[] triarr;
    int[] accumarr;
    int nextTriIndex = 0;

    ComputeBuffer offsetbuff, attrbuff, otherbuff;
    Material mat;
    Material idmat;

    Coroutine routine = null;

    public override void Prepare(GameObject model)
    {
        octex = new RenderTexture(OC_RESOLUTION, OC_RESOLUTION, 16, RenderTextureFormat.ARGB32);
        octex.enableRandomWrite = true;
        octex.Create();

        List<Mesh> meshes = new List<Mesh>();
        List<OtherAttrs> otherattrs = new List<OtherAttrs>();

        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            meshes.Add(mf.sharedMesh);
            otherattrs.Add(new OtherAttrs(){
                matrix = r.transform.localToWorldMatrix,
                color = r.sharedMaterial.color
            });
        }

        // Triangle buffers
        ImportStructuredBufferMesh.ImportAllAndUnpack(meshes.ToArray(), ref attrbuff, ref offsetbuff);
        otherbuff = new ComputeBuffer(otherattrs.Count, Marshal.SizeOf(typeof(OtherAttrs)), ComputeBufferType.Default);
        otherbuff.SetData(otherattrs.ToArray());

        // Compute Shader Buffers
        idaccum = new ComputeBuffer(attrbuff.count / 3, Marshal.SizeOf(typeof(int)));
        trilist = new ComputeBuffer(MAX_TRIANGLES, Marshal.SizeOf(typeof(uint)));
        triarr = new uint[MAX_TRIANGLES];
        accumarr = new int[attrbuff.count / 3];

        // Compute Shader
        compute = Resources.Load<ComputeShader>("Shaders/compute/countTris");
        
        compute.SetBuffer(ACCUM_KERNEL, "_idaccum", idaccum);
        compute.SetTexture(ACCUM_KERNEL, "_idTex", octex);

        compute.SetBuffer(CLEAR_KERNEL, "_idaccum", idaccum);

        compute.SetBuffer(MAP_KERNEL, "_idaccum", idaccum);
        compute.SetTexture(MAP_KERNEL, "_idTex", octex);

        compute.Dispatch(CLEAR_KERNEL, idaccum.count, 1, 1);


        // Material
        mat = new Material(Shader.Find("Indirect Shader Single Call"));
        mat.SetBuffer("offsets", offsetbuff);
        mat.SetBuffer("other", otherbuff);
        mat.SetBuffer("points", attrbuff);
        mat.SetBuffer("trilist", trilist);

        idmat = new Material(Shader.Find("Indirect Shader Single Call Ids"));
        idmat.SetBuffer("offsets", offsetbuff);
        idmat.SetBuffer("other", otherbuff);
        idmat.SetBuffer("points", attrbuff);
        
        occam = new GameObject("OC CAM").AddComponent<Camera>();
        occam.targetTexture = octex;
        occam.enabled = false;
    }

    public override void SetEnabled(bool enabled)
    {
        if (enabled) routine = StaticCoroutine.StaticStartCoroutine(GatherTriangles());
        else StaticCoroutine.StaticStopCoroutine(routine);
    }

    IEnumerator GatherTriangles()
    {
        // TODO: Tri to generate the triangle list on
        // the GPU, too, to avoid transfers
        // TODO: Dispatch multiple threads to better use
        // compute shader
        // TODO: On particularly large models iterating over
        // the texture is simpler than iterating over
        // every triangle id 1024 * 1024 ~= 1000000, so models
        // will less geometry are more expensive to iterate over
        // The data may be more expensive to transfer, though
        // TODO: Use shorts or something smaller
        // TODO: There's a triangle marked as id "0" which will never
        // be drawn because we don't add the 0 index to the triangle
        // array because it's the background color. We should 1-index
        // the triangles or make the background white with full alpha
        while (true)
        {
            // Render the OC frame
            occam.CopyFrom(Camera.main);
            occam.fieldOfView *= 1.25f;

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = octex;
            GL.Clear(true, true, new Color32(0, 0, 0, 0));
       
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.modelview = occam.worldToCameraMatrix;
            GL.LoadProjectionMatrix(occam.projectionMatrix);
            
            idmat.SetPass(0);
            Graphics.DrawProcedural(MeshTopology.Triangles, attrbuff.count, 1);
            
            GL.PopMatrix();
            RenderTexture.active = prev;

            // accumulate the ids
            compute.Dispatch(CLEAR_KERNEL, idaccum.count, 1, 1);
            compute.Dispatch(ACCUM_KERNEL, octex.width, octex.height, 1);

            // TODO: Use an appened buffer to build the triangle id
            // array on the fly
            //trilist.SetCounterValue(0);
            //compute.Dispatch(CLEAR_KERNEL, idaccum.count, 1, 1);

            // Wait for the compute shader to complete
            yield return null;
            yield return null;

            // Copy the ids from the GPU
            // TODO: Unity does this synchronously, which is bad
            int STEP = accumarr.Length;
            for (int i = 0; i < accumarr.Length; i += STEP)
            {
                idaccum.GetData(accumarr, i, i, Mathf.Min(STEP, accumarr.Length - i));
                yield return null;
            }

            // Push the discovered triangle ids on to
            int newTriIndex = 0;
            int stride = accumarr.Length / 10;
            for (uint i = 0; i < accumarr.Length && newTriIndex < triarr.Length; i++)
            {
                if (accumarr[i] != 0)
                {
                    triarr[newTriIndex] = i;
                    newTriIndex++;
                }

                if (i % stride == 0 && i != 0)
                {
                    yield return null;
                    
                    // This will progressively add the new ids
                    // to be rendered the shader as they are discovered
                    // but can result in pieces being lost and flickering
                    // in and out of view

                    // trilist.SetData(triarr, 0, 0, newTriIndex);
                    // nextTriIndex = nextTriIndex > newTriIndex ? nextTriIndex : newTriIndex;
                }
            }

            nextTriIndex = newTriIndex;            
            trilist.SetData(triarr, 0, 0, nextTriIndex);
        }
    }
    
    public override void Render(Camera cam = null, Transform root = null)
    {
        mat.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, nextTriIndex * 3, 1);
    }

    public override void Dispose()
    {
        Object.Destroy(octex);
        offsetbuff.Dispose();
        attrbuff.Dispose();
        otherbuff.Dispose();

        trilist.Dispose();
        idaccum.Dispose();
    }
}