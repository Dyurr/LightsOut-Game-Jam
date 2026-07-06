using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class VisionCone : MonoBehaviour
{
    [Header("Cone Shape")]
    public float viewAngle = 90f;
    public float verticalViewAngle = 60f;
    public float viewRange = 10f;
    [Range(4, 40)] public int segments = 20;
    [Range(2, 20)] public int vSegments = 20;

    [Header("Appearance")]
    public Color coneColor = new Color(1f, 0.1f, 0.1f, 1f);
    public float glowIntensity = 2.5f; // push this higher for a stronger "hot" look, especially with Bloom enabled

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

        float halfH = viewAngle * 0.5f;
        float halfV = verticalViewAngle * 0.5f;
        float hStep = viewAngle / segments;
        float vStep = verticalViewAngle / vSegments;

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color> colors = new List<Color>();

        // single apex point at the origin
        int apexIndex = verts.Count;
        verts.Add(Vector3.zero);
        colors.Add(Color.white);

        // grid of surface points — each one is 'viewRange' away from the apex,
        // fanned out by yaw (left/right) and pitch (up/down) simultaneously
        int gridStart = verts.Count;
        for (int h = 0; h <= segments; h++)
        {
            float yaw = (-halfH + hStep * h) * Mathf.Deg2Rad;
            for (int v = 0; v <= vSegments; v++)
            {
                float pitch = (-halfV + vStep * v) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(
                    Mathf.Sin(yaw) * Mathf.Cos(pitch),
                    Mathf.Sin(pitch),
                    Mathf.Cos(yaw) * Mathf.Cos(pitch)
                );
                verts.Add(dir * viewRange);
                colors.Add(new Color(1, 1, 1, 0f)); // fades out toward the surface
            }
        }

        int Idx(int h, int v) => gridStart + h * (vSegments + 1) + v;

        // curved outer surface (the "cap" of the cone)
        for (int h = 0; h < segments; h++)
        {
            for (int v = 0; v < vSegments; v++)
            {
                int a = Idx(h, v), b = Idx(h + 1, v), c = Idx(h, v + 1), d = Idx(h + 1, v + 1);
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }

        // fan the apex to all four border edges of the grid to close the volume
        for (int v = 0; v < vSegments; v++) // left edge
        {
            tris.Add(apexIndex); tris.Add(Idx(0, v + 1)); tris.Add(Idx(0, v));
        }
        for (int v = 0; v < vSegments; v++) // right edge
        {
            tris.Add(apexIndex); tris.Add(Idx(segments, v)); tris.Add(Idx(segments, v + 1));
        }
        for (int h = 0; h < segments; h++) // bottom edge
        {
            tris.Add(apexIndex); tris.Add(Idx(h, 0)); tris.Add(Idx(h + 1, 0));
        }
        for (int h = 0; h < segments; h++) // top edge
        {
            tris.Add(apexIndex); tris.Add(Idx(h + 1, vSegments)); tris.Add(Idx(h, vSegments));
        }

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Material BuildConeMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                        ?? Shader.Find("Particles/Additive")
                        ?? Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Unlit/Color");

        Material mat = new Material(shader);

        // Boost the color well above 1.0 so it reads as "glowing" rather than flat red.
        Color glow = coneColor * glowIntensity;
        mat.color = glow;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", glow);

        // Additive blending: SrcAlpha One means it adds light instead of covering what's behind it.
        if (mat.HasProperty("_BlendOp")) mat.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0); // don't write depth, so it never occludes things behind it
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_ALPHABLEND_ON");

        return mat;
    }
}