using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class GrassMeshGen : MonoBehaviour
{
    [Range(0.1f, 1.0f)]
    public float width = 0.1f;
    [Range(2, 4)]
    public int nVertical = 2;
    [Range(0.5f, 2f)]
    public float maxHeight = 1f;
    public float grassRadius = 2f;
    [Range(0f, 500f)]
    public float grassDensity = 0.1f;

    private Mesh mesh;
    private int[] triangles;
    // original vertices
    private Vector3[] fixedVertices;
    // runtime vertices
    private Vector3[] vertices;
    private Vector2[] uvs;


    #region Compute Shader
    struct VertexData
    {
        public Vector3 pos;
        public float time;
        public float vertexLenInv;
    };

    public ComputeShader shader;
    private ComputeBuffer buffer;
    private VertexData[] output;
    private VertexData[] data;
    #endregion


    #region MonoBehaviours
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        CreateMesh();
        UpdateMesh();

        buffer = new ComputeBuffer(vertices.Length, sizeof(float) * 5);
        data = new VertexData[vertices.Length];
        output = new VertexData[vertices.Length];

        for (int i = 0; i < data.Length; ++i)
        {
            data[i].pos = vertices[i];
            data[i].time = 0f;
            data[i].vertexLenInv = 1.0f / nVertical;
        }
    }

    // vertex movement with compute shader
    void Update()
    {
        float now = Time.time;
        for (int i = 0; i < data.Length; ++i)
        {
            data[i].time = now;
        }

        buffer.SetData(data);
        int kernel = shader.FindKernel("Swing");
        shader.SetBuffer(kernel, "dataBuffer", buffer);
        shader.Dispatch(kernel, data.Length, 1, 1);

        buffer.GetData(output);
        for (int i = 0; i < output.Length; ++i)
        {
            vertices[i] = output[i].pos;
        }
        UpdateMesh();
    }

    void OnDisable()
    {
        if (buffer != null)
            buffer.Dispose();
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
    }
    #endregion

    // Generate grass mesh
    void CreateMesh()
    {
        int nGrass = (int)Mathf.Floor(2 * Mathf.PI * grassRadius * grassRadius * grassDensity);
        int nVertex = 2 * nVertical + 1;
        int nTris = 2 * nVertical - 1;

        float nInverse = 1.0f / nVertical;
        Debug.Log("Generate grass: " + nGrass);

        vertices = new Vector3[nVertex * nGrass];
        triangles = new int[nTris * 3 * nGrass];
        uvs = new Vector2[vertices.Length];

        for (int n = 0; n < nGrass; ++n)
        {
            float height = Random.Range(maxHeight * .5f, maxHeight);
            // vertex
            int grassIdx = nVertex * n;
            for (int i = 0; i <= nVertical; ++i)
            {
                // grass width
                float w = Mathf.Lerp(width, 0, i * nInverse);
                vertices[grassIdx + i * 2] = new Vector3(width - w, height * i, 0);
                // only one vertex at top
                if (i != nVertical)
                    vertices[grassIdx + i * 2 + 1] = new Vector3(width, height * i, 0);
            }
            // triangle
            int trisIdx = nTris * 3 * n;
            for (int i = 0; i < nTris; i++)
            {
                triangles[trisIdx + i * 3] = grassIdx + i;
                triangles[trisIdx + i * 3 + 1] = grassIdx + i + 1;
                triangles[trisIdx + i * 3 + 2] = grassIdx + i + 2;
            }
            // uv
            for (int i = 0; i < uvs.Length; ++i)
            {
                Vector3 vert = vertices[i];
                uvs[i] = new Vector2(vert.x, vert.y);
            }
            VertexRotateMove();
        }
        Recenter();

        SetFixedVertices();
    }

    void VertexRotateMove()
    {
        // uniformly sample in a circle is a math problem!
        // https://stackoverflow.com/questions/5837572/generate-a-random-point-within-a-circle-uniformly

        float t = 2 * Mathf.PI * grassRadius;
        float u = Random.value + Random.value;
        float r = u > 1 ? 2 - u : u;
        Vector3 offset = new Vector3(r * Mathf.Cos(t), 0, r * Mathf.Sin(t));

        // rotate and move vertices
        Quaternion rotation = Quaternion.Euler(0, 30, 0);
        Matrix4x4 m = Matrix4x4.Rotate(rotation);

        for (int i = 0; i < vertices.Length; ++i)
        {
            vertices[i] = m.MultiplyPoint3x4(vertices[i]) + offset;
        }
    }

    void Recenter()
    {
        Vector3 center = Vector3.zero;
        foreach (Vector3 vertex in vertices)
        {
            center += vertex;
        }
        center /= vertices.Length;
        center.y = 0;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= center;
        }
    }

    void SetFixedVertices()
    {
        fixedVertices = new Vector3[vertices.Length];

        for (int i = 0; i < vertices.Length; ++i)
            fixedVertices[i] = vertices[i];
    }

    //private void OnDrawGizmos()
    //{
    //    if (vertices == null) return;
    //    for (int i = 0; i < vertices.Length; ++i)
    //    {
    //        Gizmos.DrawSphere(vertices[i], 0.01f);
    //    }
    //}
}
