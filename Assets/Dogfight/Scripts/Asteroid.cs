using UnityEngine;

public class Asteroid : MonoBehaviour
{
    [SerializeField]
    private Mesh[] meshes;
    [SerializeField]
    private float[] thresh;

    // Parameter radius is just an approximate value.
    // After applying cnoise and random scaling, the
    // actual asteroid can be somewhat smaller or larger.
    public void Initialize(float radius)
    {
        int iSize = 0;
        if (radius > thresh[1])
        {
            iSize = 2;
        }
        else if (radius > thresh[0])
        {
            iSize = 1;
        }

        Mesh mesh = Instantiate(meshes[iSize]);
        Vector3[] vertices = mesh.vertices;
        Color[] colors = new Color[vertices.Length];
        float frq = Random.Range(0.5f, 10f);
        float amp = 1f;
        
        if (iSize > 0)
        {
            frq = Random.Range(0.5f, 2f);
            amp = Mathf.Lerp(1f, 0.25f, (frq - 0.5f) / 1.5f);
        }
        frq *= (Random.value > 0.5f ? 1f : -1f);
        amp *= (Random.value > 0.5f ? 1f : -1f);

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] += vertices[i] * Unity.Mathematics.noise.cnoise(vertices[i] * frq) * amp;
            float col = vertices[i].magnitude - 0.5f;
            colors[i] = new Color(col, col, col, 1);
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;

        // Prefab scale is 2.
        transform.localScale = new Vector3(
            radius * Random.Range(0.75f, 1.25f),
            radius * Random.Range(0.75f, 1.25f),
            radius * Random.Range(0.75f, 1.25f)
        );
    }
}