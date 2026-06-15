using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PureMathCanvas : MonoBehaviour
{
    // ÊÚÑíİ ÃäæÇÚ ÇáÃÓØÍ ÇáãØáæÈÉ
    public enum SurfaceType { Wood, Plastic }

    [Header("Surface Settings")]
    public SurfaceType currentSurface = SurfaceType.Plastic;
    private SurfaceType lastSurface;

    [Header("Procedural Mesh Settings")]
    public int gridResolutionX = 20;
    public int gridResolutionZ = 20;
    public float canvasWidth = 5f;
    public float canvasHeight = 5f;

    [Header("Texture Resolution")]
    public int textureWidth = 1024;
    public int textureHeight = 1024;

    // ãÕİæİÇÊ ÇáÈäíÉ ÇáåäÏÓíÉ (Mesh)
    private Mesh proceduralMesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;

    // ãÕİæİÇÊ äÓíÌ ÇáÑÓã æÇáÃáæÇä æÇáØÈŞÇÊ
    private Texture2D canvasTexture;
    private Color[] blankPixels;
    private float[] pixelWetnessMap;

    [Header("Dynamic Physics Properties")]
    public float dryingSpeed = 0.08f;
    public float paintSpreadFactor = 1.0f; // ãÏì ÇäÊÔÇÑ ÇáÈŞÚÉ Çááæäí ÈÑãÌíÇğ

    void Start()
    {
        BuildProceduralGrid();
        InitializeTexture();

        // ÊÚííä ÇáÓØÍ ÇáÇÈÊÏÇÆí æÊÍÏíË ÇáÃáæÇä
        lastSurface = currentSurface;
        ApplySurfaceProperties();
    }

    void Update()
    {
        // ÇáÊÍŞŞ ãä ÊÛííÑ ÇáÓØÍ ãä ÇáãİÊÔ (Inspector) ÃËäÇÁ ÇáÊÔÛíá áÊÍÏíË Çááæä İæÑÇğ
        if (currentSurface != lastSurface)
        {
            ApplySurfaceProperties();
            lastSurface = currentSurface;
        }

        // ãÍÇßÇÉ ÌİÇİ ÇáØáÇÁ æÊÈÎÑ ÇáÑØæÈÉ ÚÈÑ ÇáæŞÊ
        for (int i = 0; i < pixelWetnessMap.Length; i++)
        {
            if (pixelWetnessMap[i] > 0f)
            {
                pixelWetnessMap[i] -= dryingSpeed * Time.deltaTime;
                if (pixelWetnessMap[i] < 0f) pixelWetnessMap[i] = 0f;
            }
        }
    }

    /// <summary>
    /// ÊØÈíŞ ÎÕÇÆÕ ÇáÓØÍ æÊÛííÑ áæä ÇáÎáİíÉ ÇáÃÈíÖ ÈäÇÁ Úáì ÇÎÊíÇÑ ÇááæÍÉ
    /// </summary>
    void ApplySurfaceProperties()
    {
        int totalPixels = textureWidth * textureHeight;
        blankPixels = new Color[totalPixels];

        if (currentSurface == SurfaceType.Wood)
        {
            // ÅÚÏÇÏÇÊ áæÍÉ ÇáÎÔÈ:
            dryingSpeed = 0.05f;         // ÇáÎÔÈ íãÊÕ ÈÈØÁ ãŞÇÑäÉ ÈÇáæÑŞ æáßäå íÌİ ÃÓÑÚ ãä ÇáÈáÇÓÊíß
            paintSpreadFactor = 0.8f;     // ÇäÊÔÇÑ ÇáØáÇÁ Şáíá ÈÓÈÈ ÎÔæäÉ ÇáÓØÍ

            // ÊæáíÏ áæä ÎÔÈí ÏÇİÆ ÑíÇÖíğÇ (Èäí İÇÊÍ) ãÚ ÅÖÇİÉ "ÊÌÒíÚÇÊ" ÎİíİÉ áÎÇãÉ ÇáÎÔÈ ÈÑãÌíÇğ
            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    // ãÚÇÏáÉ ÑíÇÖíÉ ÈÓíØÉ (ãæÌÉ ÌíÈíÉ) áÊæáíÏ ÎØæØ ÊÔÈå ÃáíÇİ ÇáÎÔÈ ÇáØÈíÚí
                    float woodGrain = Mathf.Sin(x * 0.05f + Mathf.PerlinNoise(x * 0.01f, y * 0.01f) * 10f) * 0.03f;

                    float r = 0.65f + woodGrain; // ÏÑÌÉ Çááæä ÇáÃÍãÑ İí ÇáÈäí
                    float g = 0.45f + woodGrain; // ÏÑÌÉ Çááæä ÇáÃÎÖÑ
                    float b = 0.25f;             // ÏÑÌÉ Çááæä ÇáÃÒÑŞ

                    blankPixels[y * textureWidth + x] = new Color(r, g, b, 1f);
                }
            }
        }
        else if (currentSurface == SurfaceType.Plastic)
        {
            // ÅÚÏÇÏÇÊ áæÍÉ ÇáÈáÇÓÊíß:
            dryingSpeed = 0.01f;         // ÇáØáÇÁ íÌİ ÈÈØÁ ÔÏíÏ ÌÏÇğ áÃä ÇáÈáÇÓÊíß ÕŞíá æáÇ íãÊÕ ÇáÓæÇÆá ãØáŞÇğ
            paintSpreadFactor = 1.3f;     // ÇáØáÇÁ íäÓÇÈ æíäÊÔÑ ÈãÓÇÍÉ ÃßÈÑ İæŞ ÇáÓØÍ ÇáäÇÚã

            // áæä ÈáÇÓÊíßí ÑãÇÏí ãÇÆá ááÈíÇÖ¡ ÕŞíá æãÊÌÇäÓ ÈÇáßÇãá
            Color plasticColor = new Color(0.92f, 0.92f, 0.95f, 1f);
            for (int i = 0; i < totalPixels; i++)
            {
                blankPixels[i] = plasticColor;
            }
        }

        // ÊÍÏíË ÇáäÓíÌ ÈÑãÌíÇğ æÖÎå ßÑÊ ÇááãÓ
        canvasTexture.SetPixels(blankPixels);
        canvasTexture.Apply();
    }

    void BuildProceduralGrid()
    {
        proceduralMesh = new Mesh();
        proceduralMesh.name = "PureMathCanvas_Mesh";

        int vCount = (gridResolutionX + 1) * (gridResolutionZ + 1);
        vertices = new Vector3[vCount];
        uvs = new Vector2[vCount];
        triangles = new int[gridResolutionX * gridResolutionZ * 6];

        float dx = canvasWidth / gridResolutionX;
        float dz = canvasHeight / gridResolutionZ;

        int v = 0;
        for (int z = 0; z <= gridResolutionZ; z++)
        {
            for (int x = 0; x <= gridResolutionX; x++)
            {
                float posX = (x * dx) - (canvasWidth / 2f);
                float posZ = (z * dz) - (canvasHeight / 2f);

                vertices[v] = new Vector3(posX, 0f, posZ);
                uvs[v] = new Vector2((float)x / gridResolutionX, (float)z / gridResolutionZ);
                v++;
            }
        }

        int t = 0;
        for (int z = 0; z < gridResolutionZ; z++)
        {
            for (int x = 0; x < gridResolutionX; x++)
            {
                int row1 = z * (gridResolutionX + 1) + x;
                int row2 = (z + 1) * (gridResolutionX + 1) + x;

                triangles[t++] = row1;
                triangles[t++] = row2;
                triangles[t++] = row1 + 1;

                triangles[t++] = row1 + 1;
                triangles[t++] = row2;
                triangles[t++] = row2 + 1;
            }
        }

        proceduralMesh.vertices = vertices;
        proceduralMesh.triangles = triangles;
        proceduralMesh.uv = uvs;

        proceduralMesh.RecalculateNormals();
        proceduralMesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = proceduralMesh;
    }

    void InitializeTexture()
    {
        canvasTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
        pixelWetnessMap = new float[textureWidth * textureHeight];

        // ÑÈØ ÇáÊßÓÊÔÑ ÇáãÊæáÏ ÈÑãÌíÇğ ÈÇáãÇÊíÑíÇá ÇáÎÇÕÉ ÈÇááæÍÉ ÈÔßá İæÑí
        GetComponent<MeshRenderer>().material.mainTexture = canvasTexture;
    }
}