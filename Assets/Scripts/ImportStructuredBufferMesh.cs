using System.Runtime.InteropServices;
using UnityEngine;

// Utility for converting a unity mesh into
// a buffer of indices and a buffer of attributes
public static class ImportStructuredBufferMesh {

    struct Point
    {
        public Vector3 vertex;
        public Vector3 normal;
    }

    public static void Import(Mesh mesh, ref ComputeBuffer indices, ref ComputeBuffer attr)
    {
        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        Vector3[] norms = mesh.normals;

        indices = new ComputeBuffer(tris.Length, Marshal.SizeOf(typeof(int)), ComputeBufferType.Default);
        indices.SetData(tris);

        Point[] data = new Point[verts.Length];
        for(int i = 0; i < data.Length; i++)
        {
            data[i] = new Point()
            {
                vertex = verts[i],
                normal = norms[i]
            };
        }

        attr = new ComputeBuffer(data.Length, Marshal.SizeOf(typeof(Point)), ComputeBufferType.Default);
        attr.SetData(data);
    }

    public static void ImportAndUnpack(Mesh mesh, ref ComputeBuffer attr)
    {
        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        Vector3[] norms = mesh.normals;

        Point[] data = new Point[tris.Length];
        for (int i = 0; i < data.Length; i++)
        {
            int idx = tris[i];
            data[i] = new Point()
            {
                vertex = verts[idx],
                normal = norms[idx]
            };
        }

        attr = new ComputeBuffer(data.Length, Marshal.SizeOf(typeof(Point)), ComputeBufferType.Default);
        attr.SetData(data);
    }
}
