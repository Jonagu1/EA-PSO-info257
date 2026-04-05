using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class RastriginTerrain : MonoBehaviour
{
    [Header("Configuración del Dominio")]
    public float domainMin = -3f;
    public float domainMax = 7f;
    
    [Header("Configuración de Altura y Escala")]
    public float terrainScaleX = 100f; // Escala física X y Z en Unity Space
    public float terrainScaleY = 50f;  // Que tan alto quieres que se dibujen visualmente las montañas

    public static RastriginTerrain Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GenerarTerrenoRastrigin();
    }

    void GenerarTerrenoRastrigin()
    {
        // Obtenemos el componente Terrain y sus datos
        Terrain terrain = GetComponent<Terrain>();
        TerrainData terrainData = terrain.terrainData;

        // Obtenemos la resolución del heightmap (matriz de alturas)
        int resolution = terrainData.heightmapResolution;
        
        // Creamos la matriz rectangular de dos dimensiones
        float[,] rawHeights = new float[resolution, resolution];
        float computedMaxY = 0f;
        
        // Recorremos la matriz de alturas
        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                // Mapeamos los índices de la matriz [0, resolution-1] al dominio matemático [-3, 7]
                // i corresponde a la coordenada Z, j corresponde a la coordenada X
                float z = domainMin + (domainMax - domainMin) * ((float)i / (resolution - 1));
                float x = domainMin + (domainMax - domainMin) * ((float)j / (resolution - 1));

                // Calculamos el valor de la función Rastrigin para las variables x y z
                float y = 20f + (x * x - 10f * Mathf.Cos(2f * Mathf.PI * x)) 
                              + (z * z - 10f * Mathf.Cos(2f * Mathf.PI * z));
                
                rawHeights[i, j] = y;
                
                if (y > computedMaxY)
                {
                    computedMaxY = y;
                }
            }
        }
        
        // Creamos la matriz normalizada
        float[,] heights = new float[resolution, resolution];
        
        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                // Normalizamos entre 0.0 y 1.0 con el máximo real
                // También evitamos dividir por 0
                heights[i, j] = (computedMaxY > 0f) ? (rawHeights[i, j] / computedMaxY) : 0f;
            }
        }

        // Ajustamos la escala VÍSUAL del terreno en Unity Space
        // terrainScaleX define cuánto espacio ocupa, y terrainScaleY ajusta qué tan altos se ven los picos en el juego
        terrainData.size = new Vector3(terrainScaleX, terrainScaleY, terrainScaleX);

        // Aplicamos la matriz de alturas generada al terreno
        terrainData.SetHeights(0, 0, heights);
    }

    // --- MÉTODOS DE UTILIDAD PARA EL ALGORITMO PSO ---
    // Convierte tu "xi" del algoritmo matemático (por ej. -3, 0.5, 7) a posiciones reales en Unity (ej. 300x, 700z)
    public Vector3 MathToUnitySpace(float mathX, float mathZ)
    {
        Terrain terrain = GetComponent<Terrain>();
        Vector3 size = terrain.terrainData.size;
        Vector3 pos = terrain.transform.position;

        // Mapear de [-3, 7] a [0, 1] y luego a la escala de Unity [0, size]
        float tX = (mathX - domainMin) / (domainMax - domainMin);
        float tZ = (mathZ - domainMin) / (domainMax - domainMin);

        float unityX = pos.x + tX * size.x;
        float unityZ = pos.z + tZ * size.z;
        
        Vector3 unityPos = new Vector3(unityX, 0, unityZ);
        unityPos.y = terrain.SampleHeight(unityPos) + pos.y; // Ajusta a la altura del terreno automáticamente
        
        return unityPos;
    }

    // Convierte posiciones físicas de Unity (tu partícula se mueve en el mundo) al valor matemático "xi" ([-3, 7]) para evaluarla
    public Vector2 UnityToMathSpace(Vector3 unityPos)
    {
        Terrain terrain = GetComponent<Terrain>();
        Vector3 size = terrain.terrainData.size;
        Vector3 pos = terrain.transform.position;

        float tX = (unityPos.x - pos.x) / size.x;
        float tZ = (unityPos.z - pos.z) / size.z;

        float mathX = domainMin + tX * (domainMax - domainMin);
        float mathZ = domainMin + tZ * (domainMax - domainMin);

        return new Vector2(mathX, mathZ);
    }
}