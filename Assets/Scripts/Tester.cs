using System.Collections.Generic;
using UnityEngine;

public class Tester : MonoBehaviour {
    public enum Approach
    {
        DRAW_MESH,
        DRAW_MESH_PROP_BLOCK,
        DRAW_PROCEDURAL,
        UNPACKED_DRAW_PROCEDURAL,
        VISIBLE_TRIANGLES,
        UNITY_RENDERER
    }

    public GameObject testObj;
    public Approach approach = Approach.UNITY_RENDERER;
    Approach lastApproach = Approach.UNITY_RENDERER;

    Dictionary<Approach, RenderingApproach> approaches = new Dictionary<Approach, RenderingApproach>();
    RenderingApproach curr { get { return approaches.ContainsKey(approach) ? approaches[approach] : null; } }

    const int DEFAULT_FRAME_TIMEOUT = 20;
    int frameTimeout = DEFAULT_FRAME_TIMEOUT;
    int frames = 0;
    float frametime = 0;

	void Start () {
        approaches[Approach.DRAW_MESH]                  = new DrawMeshTest();
        approaches[Approach.DRAW_MESH_PROP_BLOCK]       = new DrawMeshWithPropBlockTest();
        approaches[Approach.DRAW_PROCEDURAL]            = new DrawProceduralTest();
        approaches[Approach.UNPACKED_DRAW_PROCEDURAL]   = new UnpackedDrawProceduralTest();
        approaches[Approach.VISIBLE_TRIANGLES]          = new VisibleTriangleRenderTest();
        approaches[Approach.UNITY_RENDERER]             = new RendererTest();

        foreach (var kv in approaches) kv.Value.Prepare(testObj);
	}

    private void Update()
    {
        //if (Camera.current != Camera.main) return;

        // Debug.Log("COUNT: " + Time.frameCount + " : " + Camera.current);
        if (approach != lastApproach)
        {
            frameTimeout = DEFAULT_FRAME_TIMEOUT;

            if (approaches.ContainsKey(lastApproach)) approaches[lastApproach].SetEnabled(false);
            if (approaches.ContainsKey(approach)) approaches[approach].SetEnabled(true);

            lastApproach = approach;

            frametime = 0;
            frames = 0;
        }
        
        frameTimeout--;
        if (frameTimeout < 0)
        {
            frames++;
            frametime += (Time.deltaTime - frametime) / frames;
            Debug.Log("FPS: " + (1.0f / frametime).ToString("0.00000") + " - " + (1.0f / Time.deltaTime).ToString("0.00000"));
        }
    }

    void OnRenderObject()
    {
        if (curr != null) curr.Render(Camera.main, transform);
    }

    private void OnDestroy()
    {
        foreach (var kv in approaches) kv.Value.Dispose();
    }

    private void OnGUI()
    {
        if (curr != null) curr.OnGUI();
    }
}
