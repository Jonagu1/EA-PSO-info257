using UnityEngine;

public class Particle : MonoBehaviour
{
    [Header("Variables Compartidas")]
    public float mathX;
    public float mathZ;
    public float fitness;

    [Header("Variables exclusivas de PSO")]
    public float px, pz; // Personal Best Math X y Z [6]
    public float pfit = float.MaxValue; // Personal Best Fitness (Minimización) [6]
    public float vx, vz; // Velocidad de la partícula [7]

    void Start()
    {
        // 1. Agregar a la lista del enjambre si existe
        if (swarmAlgorithm.Instance != null && !swarmAlgorithm.Instance.population.Contains(this))
        {
            swarmAlgorithm.Instance.population.Add(this);
        }

        // 2. Traducir la posición inicial física de Unity al espacio Matemático para arrancar
        if (RastriginTerrain.Instance != null)
        {
            Vector2 mPos = RastriginTerrain.Instance.UnityToMathSpace(transform.position);
            mathX = mPos.x;
            mathZ = mPos.y;
        }

        // 3. Inicializar variables de PSO
        px = mathX;
        pz = mathZ;
        vx = Random.Range(-0.5f, 0.5f);
        vz = Random.Range(-0.5f, 0.5f);
        pfit = float.MaxValue; // Inicia esperando mejora
    }

    void Update()
    {
        if (swarmAlgorithm.Instance.usaEvolutivo)
        {
            SetPositionEA(mathX, mathZ);
            Vector3 targetUnityPos = RastriginTerrain.Instance.MathToUnitySpace(mathX, mathZ);
            transform.position = targetUnityPos;
        }
        else
        {
            // ELIMINADO: MovePSO(...); ya no se llama aquí.
            
            // Solo dejamos el Lerp para que persiga a la matemática fluidamente
            Vector3 targetUnityPos = RastriginTerrain.Instance.MathToUnitySpace(mathX, mathZ);
            transform.position = Vector3.Lerp(transform.position, targetUnityPos, 10f * Time.deltaTime);
        }
    }

    // 1. Evaluación de la función (Común para ambos)
    public void EvaluateFitness()
    {
        // Función Rastrigin: f(x) = 10n + sum(x_i^2 - 10*cos(2*pi*x_i)) [12, 13]
        float termX = (mathX * mathX) - 10f * Mathf.Cos(2f * Mathf.PI * mathX);
        float termZ = (mathZ * mathZ) - 10f * Mathf.Cos(2f * Mathf.PI * mathZ);
        fitness = 20f + termX + termZ;

        // Actualizar la memoria individual (Solo importa para PSO, pero es seguro calcularlo siempre) [8]
        if (fitness < pfit)
        {
            pfit = fitness;
            px = mathX;
            pz = mathZ;
        }
    }

    // 2. Movimiento para EA: Teletransporte directo [14, 15]
    public void SetPositionEA(float newX, float newZ)
    {
        float min = RastriginTerrain.Instance.domainMin; // [10]
        float max = RastriginTerrain.Instance.domainMax; // [10]

        // Acotamos en el borde si se salen [11]
        mathX = Mathf.Clamp(newX, min, max);
        mathZ = Mathf.Clamp(newZ, min, max);
    }

    // 3. Movimiento para PSO: Cálculo de inercia y rebote [9]
    public void MovePSO(float gbestX, float gbestZ, float w, float c1, float c2 , float maxv)
    {
        // Actualiza velocidad según inercia, componente cognitiva y componente social [9]
        vx = w * vx + Random.Range(0f, 1f) * c1 * (px - mathX) + Random.Range(0f, 1f) * c2 * (gbestX - mathX);
        vz = w * vz + Random.Range(0f, 1f) * c1 * (pz - mathZ) + Random.Range(0f, 1f) * c2 * (gbestZ - mathZ);

        // Limitador de Velocidad: Truncamos en vez de multiplicar. Esto mantiene la simulación Pura
        // y nos deja ver cómo exploran sin arruinar la concordancia posicional de la gráfica real.
        float modulo = Mathf.Sqrt(vx * vx + vz * vz);
        if (modulo > maxv && modulo > 0.00001f)
        {
            vx = (vx / modulo) * maxv;
            vz = (vz / modulo) * maxv;
        }

        // Suma Pura de Rastrigin
        mathX += vx;
        mathZ += vz;

        // Restricción: Si chocan con el límite, invierten su velocidad (rebote) [9, 11]
        float min = RastriginTerrain.Instance.domainMin;
        float max = RastriginTerrain.Instance.domainMax;

        if (mathX < min || mathX > max) 
        { 
            vx = -vx; 
            mathX = Mathf.Clamp(mathX, min, max); 
        }
        if (mathZ < min || mathZ > max) 
        { 
            vz = -vz; 
            mathZ = Mathf.Clamp(mathZ, min, max); 
        }
    }
}