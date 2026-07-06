using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class VisionCone : MonoBehaviour
{
    [Header("Cone Shape")]
    public float viewAngle = 90f;
    public float viewRange = 10f;
    public float coneHeight = 1.5f; // vertical thickness so it has volume for the collider
    [Range(4, 40)] public int segments = 20;

    [Header("Appearance")]
    public Color coneColor = new Color(1f, 0f, 0f, 0.3f);

    void Awake()
    {
        Mesh mesh = BuildConeMesh();

        GetComponent<MeshFilter>().mesh = mesh;

        MeshCollider col = GetComponent<MeshCollider>();
        col.sharedMesh = mesh;
        col.convex = true;   // required for trigger colliders; Unity builds a convex hull from the verts
        col.isTrigger = true;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.material = BuildConeMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private Mesh BuildConeMesh()
    {
        Mesh mesh = new Mesh { name = "VisionCone" };

        float halfAngle = viewAngle * 0.5f;
        float angleStep = viewAngle / segments;

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        Vector3 apexBottom = Vector3.zero;
        Vector3 apexTop = new Vector3(0, coneHeight, 0);
        verts.Add(apexBottom); // 0
        verts.Add(apexTop);    // 1

        int bottomStart = verts.Count;
        int topStart = bottomStart + (segments + 1);

        for (int i = 0; i <= segments; i++)
        {
            float rad = (-halfAngle + angleStep * i) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * viewRange;
            verts.Add(dir);                          // bottom ring
        }
        for (int i = 0; i <= segments; i++)
        {
            float rad = (-halfAngle + angleStep * i) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * viewRange;
            verts.Add(dir + Vector3.up * coneHeight); // top ring
        }

        // bottom fan
        for (int i = 0; i < segments; i++)
        {
            tris.Add(0); tris.Add(bottomStart + i); tris.Add(bottomStart + i + 1);
        }
        // top fan
        for (int i = 0; i < segments; i++)
        {
            tris.Add(1); tris.Add(topStart + i + 1); tris.Add(topStart + i);
        }
        // outer curved wall
        for (int i = 0; i < segments; i++)
        {
            int b0 = bottomStart + i, b1 = bottomStart + i + 1;
            int t0 = topStart + i, t1 = topStart + i + 1;
            tris.Add(b0); tris.Add(t0); tris.Add(b1);
            tris.Add(b1); tris.Add(t0); tris.Add(t1);
        }
        // two flat side walls (closing the pie slice)
        int bL = bottomStart, tL = topStart, bR = bottomStart + segments, tR = topStart + segments;
        tris.Add(0); tris.Add(tL); tris.Add(bL);
        tris.Add(tL); tris.Add(1); tris.Add(bL);
        tris.Add(0); tris.Add(bR); tris.Add(tR);
        tris.Add(bR); tris.Add(1); tris.Add(tR);

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Material BuildConeMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Unlit/Color")
                        ?? Shader.Find("Standard");
        Material mat = new Material(shader) { color = coneColor };
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = 3000;
        if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3); // Standard shader: Transparent mode
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // URP: Transparent surface
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // visible from both sides
        return mat;
    }
}