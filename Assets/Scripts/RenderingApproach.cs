using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;

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

    int OC_RESOLUTION = 1024;
    RenderTexture octex;
    Camera occam;

    ComputeBuffer offsetbuff, attrbuff, otherbuff;
    Material mat;
    int totalMeshes = 0;

    public override void Prepare(GameObject model)
    {
        occam = new GameObject("OC Camera").AddComponent<Camera>();
        occam.enabled = false;

        octex = new RenderTexture(OC_RESOLUTION, OC_RESOLUTION, 16);
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

        ImportStructuredBufferMesh.ImportAllAndUnpack(meshes.ToArray(), ref attrbuff, ref offsetbuff);
        otherbuff = new ComputeBuffer(otherattrs.Count, Marshal.SizeOf(typeof(OtherAttrs)), ComputeBufferType.Default);
        otherbuff.SetData(otherattrs.ToArray());

        mat = new Material(Shader.Find("Indirect Shader Single Call"));
        mat.SetBuffer("offsets", offsetbuff);
        mat.SetBuffer("other", otherbuff);
        mat.SetBuffer("points", attrbuff);
    }

    public override void Render(Camera cam = null, Transform root = null)
    {
        occam.CopyFrom(cam);
        occam.targetTexture = octex;
        occam.fieldOfView *= 1.5f;

        // TODO: Dispatch pertriangle id render
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = octex;

        mat.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, attrbuff.count, 1);
        // TODO: Dispatch visible triangle accumulation

        // Dispatch the full model
        RenderTexture.active = prev;
        mat.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, attrbuff.count, 1);
    }

    public override void Dispose()
    {
        Object.Destroy(octex);
        offsetbuff.Dispose();
        attrbuff.Dispose();
        otherbuff.Dispose();
    }
}