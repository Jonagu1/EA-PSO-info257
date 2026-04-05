using System.Collections.Generic;
using UnityEngine;

public class swarmAlgorithm : MonoBehaviour
{
    public static swarmAlgorithm Instance;
    
    public List<Particle> population = new List<Particle>();

    [Header("Selector de Algoritmo")]
    [Tooltip("Marca para usar EA. Desmarca para usar PSO.")]
    public bool usaEvolutivo = false; 

    [Header("Parámetros Compartidos")]
    public float gbestFit = float.MaxValue; // Mejor fitness global encontrado [16]
    public float gbestX, gbestZ; // Coordenadas del mejor global [16]

    [Header("Parámetros Algoritmo Evolutivo (EA)")]
    public float prob_cruce = 0.8f;
    public float tasa_mutacion = 0.05f;
    public int tournamentSize = 8;

    [Header("Parámetros Enjambre de Partículas (PSO)")]
    public float w = 0.5f; // Factor de inercia [16]
    public float C1 = 1.5f; // Factor de aprendizaje propio (Cognitivo) [16]
    public float C2 = 1.5f; // Factor de aprendizaje social [16]
    [Tooltip("Límite de velocidad matemática. Bajar este número hace que el enjambre vuele más lento sin arruinar el cruce")]
    public float maxV = 0.1f;

    [Header("Control de Tiempo")]
    [Tooltip("Cada cuántos segundos ocurre una nueva generación en el EA (Poblacional)")]
    public float segundosPorGeneracionEA = 1.0f; 
    [Tooltip("Cuántos pasos matemáticos por segundo da el PSO (Movimiento de enjambre)")]
    public float pasosPorSegundoPSO = 30f; 
    
    private float timer = 0f;
    public int iteracionActual = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        if (population.Count == 0) return;

        timer += Time.deltaTime;

        if (usaEvolutivo)
        {
            // El EA es discreto y generacional
            if (timer >= segundosPorGeneracionEA)
            {
                timer -= segundosPorGeneracionEA;
                EvolveGenerationEA(); 
                iteracionActual++;
            }
        }
        else
        {
            // Usamos tu variable 'pasosPorSegundoPSO' para limitar la velocidad matemática
            float tiempoPorPaso = 1f / pasosPorSegundoPSO;
            
            if (timer >= tiempoPorPaso)
            {
                timer -= tiempoPorPaso;
                StepPSO(); // Ahora el PSO avanza a 30 pasos por segundo, como en Processing
                iteracionActual++; // Opcional: para que veas subir el contador en la UI
            }
        }
    }
    // ==========================================
    // MÉTODO 1: PARTICLE SWARM OPTIMIZATION (PSO)
    // ==========================================
    void StepPSO()
    {
        // 1. Evaluar población para encontrar el GBest (Mejor Social)
        foreach (Particle p in population)
        {
            p.EvaluateFitness();
            if (p.fitness < gbestFit)
            {
                gbestFit = p.fitness;
                gbestX = p.mathX;
                gbestZ = p.mathZ;
            }
        }

        // 2. Mover las partículas ajustando su velocidad hacia GBest y PBest [7, 9]
        foreach (Particle p in population)
        {
            p.MovePSO(gbestX, gbestZ, w, C1, C2 , maxV);
        }
    }

    // ==========================================
    // MÉTODO 2: ALGORITMO EVOLUTIVO (EA)
    // ==========================================
    void EvolveGenerationEA()
    {
        // 1. Evaluar aptitud (Fitness) [14]
        foreach (Particle p in population)
        {
            p.EvaluateFitness();
            if (p.fitness < gbestFit)
            {
                gbestFit = p.fitness;
                gbestX = p.mathX;
                gbestZ = p.mathZ;
            }
        }

        List<Vector2> nuevaGeneracion = new List<Vector2>();

        // 2. Selección, Cruzamiento Promediado y Mutación
        int numPares = population.Count / 2;
        
        for (int i = 0; i < numPares; i++)
        {
            // Seleccionamos las dos posiciones de los padres a reemplazar (y eliminar de la siguiente generación)
            Particle padre1 = SeleccionTournament(tournamentSize);
            Particle padre2 = SeleccionTournament(tournamentSize);

            // Valores de los hijos si no hay cruzamiento (clones directos de cada padre)
            float hijo1X = padre1.mathX;
            float hijo1Z = padre1.mathZ;
            float hijo2X = padre2.mathX;
            float hijo2Z = padre2.mathZ;

            // Cruzamiento Guiado (Evita que todas las partículas colapsen al mismo punto)
            // Genera hijos alrededor de los padres en dirección al otro padre, radio máximo < 1
            if (Random.value < prob_cruce)
            {
                float dx = padre2.mathX - padre1.mathX;
                float dz = padre2.mathZ - padre1.mathZ;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                // Evitamos división por cero si los padres están en el mismo punto exacto
                if (dist > 0.0001f)
                {
                    // Movemos al hijo 1 desde el padre 1 en dirección al padre 2, como máximo 1 unidad visual/matemática
                    float move1 = Random.Range(0f, Mathf.Min(1f, dist));
                    hijo1X = padre1.mathX + (dx / dist) * move1;
                    hijo1Z = padre1.mathZ + (dz / dist) * move1;

                    // Movemos al hijo 2 desde el padre 2 en dirección al padre 1, como máximo 1 unidad
                    float move2 = Random.Range(0f, Mathf.Min(1f, dist));
                    hijo2X = padre2.mathX - (dx / dist) * move2;
                    hijo2Z = padre2.mathZ - (dz / dist) * move2;
                }
            }

            // Aplicamos mutación a cada hijo independientemente
            if (Random.value < tasa_mutacion) 
            {
                hijo1X = Random.Range(RastriginTerrain.Instance.domainMin, RastriginTerrain.Instance.domainMax);
                hijo1Z = Random.Range(RastriginTerrain.Instance.domainMin, RastriginTerrain.Instance.domainMax);
            }
            if (Random.value < tasa_mutacion) 
            {
                hijo2X = Random.Range(RastriginTerrain.Instance.domainMin, RastriginTerrain.Instance.domainMax);
                hijo2Z = Random.Range(RastriginTerrain.Instance.domainMin, RastriginTerrain.Instance.domainMax);
            }

            // Añadimos la pareja de hijos para rellenar los 2 huecos generacionales
            nuevaGeneracion.Add(new Vector2(hijo1X, hijo1Z));
            nuevaGeneracion.Add(new Vector2(hijo2X, hijo2Z));
        }
        
        // Si la lista tuviera un número impar de partículas, clonamos al final al mejor global para rellenar
        if (nuevaGeneracion.Count < population.Count)
        {
            nuevaGeneracion.Add(new Vector2(gbestX, gbestZ));
        }

        // 3. Reinserción Generacional
        for (int i = 0; i < population.Count; i++)
        {
            population[i].SetPositionEA(nuevaGeneracion[i].x, nuevaGeneracion[i].y);
        }
    }

    // Torneo extraído de tu modelo de Python [14]
    Particle SeleccionTournament(int n)
    {
        Particle mejor = null;
        for (int i = 0; i < n; i++)
        {
            Particle candidato = population[Random.Range(0, population.Count)];
            if (mejor == null || candidato.fitness < mejor.fitness)
            {
                mejor = candidato;
            }
        }
        return mejor;
    }

    // ==========================================
    // INTERFAZ DE USUARIO (GUI) RESTAURADA
    // ==========================================
    void OnGUI()
    {
        if (RastriginTerrain.Instance == null) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        
        // Caja de fondo semitransparente para hacer legibles los textos
        GUI.Box(new Rect(10, 10, 480, 210), "");
        GUI.Box(new Rect(10, 10, 480, 210), "Panel de Monitorización del Enjambre");

        string mode = usaEvolutivo ? "Algoritmo Evolutivo (EA)" : "Enjambre de Partículas (PSO)";
        
        GUI.Label(new Rect(20, 40, 400, 30), $"Modo Actual: {mode}", style);
        GUI.Label(new Rect(20, 70, 400, 30), $"Partículas Activas: {population.Count}", style);
        
        // Las evaluaciones totales son las generaciones (o pasos) por la cantidad global del enjambre
        int totalEvals = iteracionActual * population.Count;
        GUI.Label(new Rect(20, 100, 450, 30), $"Iteración Generacional: {iteracionActual} | Evals: {totalEvals}", style);

        if (gbestFit != float.MaxValue)
        {
            // F4 asegura mostrar 4 decimales
            GUI.Label(new Rect(20, 130, 450, 30), $"Mejor Global (Fitness): {gbestFit.ToString("F6")}", style);
            GUI.Label(new Rect(20, 160, 450, 30), $"Ubicación Óptima (X, Z): {gbestX.ToString("F4")} , {gbestZ.ToString("F2")}", style);
        }
        else
        {
            GUI.Label(new Rect(20, 130, 400, 30), "Calculando Mejor Fitness...", style);
        }
    }
}