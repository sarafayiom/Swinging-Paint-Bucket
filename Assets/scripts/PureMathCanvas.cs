using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PureMathCanvas : MonoBehaviour
{
    [Header("Procedural Mesh Settings")]
    public int gridResolutionX = 20; // ÚÏÏ ÇáÊŞÓíãÇÊ ÇáåäÏÓíÉ ÚÑÖÇğ
    public int gridResolutionZ = 20; // ÚÏÏ ÇáÊŞÓíãÇÊ ÇáåäÏÓíÉ ØæáÇğ
    public float canvasWidth = 5f;   // ÇáÚÑÖ ÇáİÚáí ááæÍÉ İí ÇáÚÇáã
    public float canvasHeight = 5f;  // ÇáØæá ÇáİÚáí ááæÍÉ İí ÇáÚÇáã

    [Header("Texture Resolution")]
    public int textureWidth = 1024;
    public int textureHeight = 1024;

    [Header("Surface Physics")]
    [Tooltip("Options: Paper, Metal")]
    public string surfaceType = "Paper";

    // ãÕİæİÇÊ ÇáÈäíÉ ÇáåäÏÓíÉ (Mesh)
    private Mesh proceduralMesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;

    // ãÕİæİÇÊ äÓíÌ ÇáÑÓã æÇáÃáæÇä æÇáØÈŞÇÊ
    private Texture2D canvasTexture;
    private Color[] blankPixels;
    private float[] pixelWetnessMap;
    private float dryingSpeed = 0.08f;

    void Start()
    {
        // 1. ÈäÇÁ ÇáÔßá ÇáåäÏÓí ááæÍÉ ãä ÇáÕİÑ ÈÇáÑíÇÖíÇÊ (ãËá ÇáÍÈá ÊãÇãÇğ)
        BuildProceduralGrid();

        // 2. ÅäÔÇÁ äÓíÌ ÈßÓáÇÊ ÇáÑÓã ÈÑãÌíÇğ
        InitializeTexture();
    }

    void Update()
    {
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
    /// ÎæÇÑÒãíÉ ÊæáíÏ ÔÈßÉ ÇáãÑÈÚÇÊ æÇáãËáËÇÊ ÈÑãÌíÇğ ÈÏæä Ãí ãÌÓã ÌÇåÒ
    /// </summary>
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

        // ÊæáíÏ ÇáäŞÇØ (Vertices) æÇáÅÍÏÇËíÇÊ ÇáäÓÈíÉ (UVs)
        int v = 0;
        for (int z = 0; z <= gridResolutionZ; z++)
        {
            for (int x = 0; x <= gridResolutionX; x++)
            {
                // ÍÓÇÈ ãæŞÚ ÇáäŞØÉ ÈÑãÌíÇğ ÈÍíË íßæä ãÑßÒ ÇááæÍÉ åæ ÇáÜ (0,0,0) ÇáãÍáí
                float posX = (x * dx) - (canvasWidth / 2f);
                float posZ = (z * dz) - (canvasHeight / 2f);

                vertices[v] = new Vector3(posX, 0f, posZ);

                // ÑÈØ ÅÍÏÇËíÇÊ ÇáÜ UV (ãåãÉ ÌÏÇğ áßí íİåã ßæÏ ÇáÑÓã Ãíä ÊŞÚ ÇáÈßÓáÇÊ)
                uvs[v] = new Vector2((float)x / gridResolutionX, (float)z / gridResolutionZ);
                v++;
            }
        }

        // ÊæáíÏ ÇáãËáËÇÊ (Triangles) áÊæÕíá ÇáäŞÇØ ÈÈÚÖåÇ (ßá ãÑÈÚ íÊßæä ãä ãËáËíä)
        int t = 0;
        for (int z = 0; z < gridResolutionZ; z++)
        {
            for (int x = 0; x < gridResolutionX; x++)
            {
                int row1 = z * (gridResolutionX + 1) + x;
                int row2 = (z + 1) * (gridResolutionX + 1) + x;

                // ÇáãËáË ÇáÃæá
                triangles[t++] = row1;
                triangles[t++] = row2;
                triangles[t++] = row1 + 1;

                // ÇáãËáË ÇáËÇäí
                triangles[t++] = row1 + 1;
                triangles[t++] = row2;
                triangles[t++] = row2 + 1;
            }
        }

        // ÊÚííä ÇáÈíÇäÇÊ ÇáãÈÑãÌÉ ááãÔ ÇáãÎÕÕ
        proceduralMesh.vertices = vertices;
        proceduralMesh.triangles = triangles;
        proceduralMesh.uv = uvs;

        // ÍÓÇÈ ÇáÅÖÇÁÉ æÇáÙáÇá ÑíÇÖíÇğ ÊáŞÇÆíÇğ ááãÌÓã ÇáÌÏíÏ
        proceduralMesh.RecalculateNormals();
        proceduralMesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = proceduralMesh;
    }

    void InitializeTexture()
    {
        canvasTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        blankPixels = new Color[textureWidth * textureHeight];
        pixelWetnessMap = new float[textureWidth * textureHeight];

        for (int i = 0; i < blankPixels.Length; i++)
        {
            blankPixels[i] = Color.white;
            pixelWetnessMap[i] = 0f;
        }

        canvasTexture.SetPixels(blankPixels);
        canvasTexture.Apply();

        GetComponent<MeshRenderer>().material.mainTexture = canvasTexture;
    }

    /// <summary>
    /// ÏÇáÉ ÅÓŞÇØ ãæŞÚ ÇáÏáæ ËáÇËí ÇáÃÈÚÇÏ Úáì ÇááæÍÉ ÇáãÕäæÚÉ ÈÑãÌíÇğ
    /// </summary>
    public void PaintAtWorldPosition(Vector3 bucketWorldPos, float baseRadius, Color paintColor, float bucketSpeed, float fluidFlowRate)
    {
        // ÊÍæíá ãæŞÚ ÇáÏáæ ãä ÇáÚÇáã ÇáÎÇÑÌí Åáì ãÓÇÍÉ ÇááæÍÉ ÇáãÍáíÉ (Local Space)
        Vector3 localPos = transform.InverseTransformPoint(bucketWorldPos);

        // ÍÓÇÈ ÇáäÓÈÉ ÇáãÚíÇÑíÉ (ãä 0 Åáì 1) áãæŞÚ ÇáÏáæ ÈäÇÁğ Úáì ÃÈÚÇÏ ÇááæÍÉ ÇáÈÑãÌíÉ ÇáãÍÏÏÉ íÏæíÇğ
        float normalizedX = (localPos.x / canvasWidth) + 0.5f;
        float normalizedZ = (localPos.z / canvasHeight) + 0.5f;

        // ÇáÊÍŞŞ ãä Ãä ÇáÅÓŞÇØ ÇáÑíÇÖí íŞÚ ÏÇÎá ãÓÇÍÉ ÇááæÍÉ ÇáäÇÊÌÉ
        if (normalizedX >= 0 && normalizedX <= 1 && normalizedZ >= 0 && normalizedZ <= 1)
        {
            int pixelX = (int)(normalizedX * textureWidth);
            int pixelY = (int)(normalizedZ * textureHeight);

            ApplyAdvancedBlending(pixelX, pixelY, baseRadius, paintColor, bucketSpeed, fluidFlowRate);
        }
    }

    private void ApplyAdvancedBlending(int cx, int cy, float baseRadius, Color newColor, float bucketSpeed, float fluidFlowRate)
    {
        if (bucketSpeed < 0.1f) bucketSpeed = 0.1f;
        float paintThickness = (1.0f / bucketSpeed) * fluidFlowRate;

        int r = (int)baseRadius;
        float absorptionSpread = 1.0f;

        if (surfaceType == "Paper")
        {
            absorptionSpread = 1.5f;
            r = (int)(baseRadius * absorptionSpread * (1.0f + paintThickness * 0.4f));
        }
        else if (surfaceType == "Metal")
        {
            absorptionSpread = 0.7f;
            r = (int)(baseRadius * absorptionSpread);
        }

        int startX = Mathf.Max(0, cx - r);
        int endX = Mathf.Min(textureWidth - 1, cx + r);
        int startY = Mathf.Max(0, cy - r);
        int endY = Mathf.Min(textureHeight - 1, cy + r);

        bool textureChanged = false;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                {
                    int pixelIndex = y * textureWidth + x;
                    Color existingColor = canvasTexture.GetPixel(x, y);
                    Color blendedColor = existingColor;

                    float currentWetness = pixelWetnessMap[pixelIndex];
                    float dynamicBlendFactor = Mathf.Clamp01(paintThickness * (1.0f + currentWetness));

                    if (existingColor == Color.white)
                    {
                        blendedColor = newColor;
                    }
                    else
                    {
                        if (currentWetness > 0.15f)
                        {
                            // ÊÃËíÑ (Wet-on-Wet) ÇáãÓÊæÍì ãä ÇáÃÈÍÇË
                            blendedColor.r = (newColor.r * dynamicBlendFactor) + (existingColor.r * (1f - dynamicBlendFactor));
                            blendedColor.g = (newColor.g * dynamicBlendFactor) + (existingColor.g * (1f - dynamicBlendFactor));
                            blendedColor.b = (newColor.b * dynamicBlendFactor) + (existingColor.b * (1f - dynamicBlendFactor));
                        }
                        else
                        {
                            // ÊÃËíÑ (Wet-on-Dry)
                            blendedColor = Color.Lerp(existingColor, newColor, dynamicBlendFactor);
                        }
                    }

                    blendedColor.a = 1f;
                    canvasTexture.SetPixel(x, y, blendedColor);

                    pixelWetnessMap[pixelIndex] = 1.0f;
                    textureChanged = true;
                }
            }
        }

        if (textureChanged)
        {
            canvasTexture.Apply(false);
        }
    }
}