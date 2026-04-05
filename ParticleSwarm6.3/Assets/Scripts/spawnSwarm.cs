
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spawnSwarm : MonoBehaviour
{
    public GameObject particlePrefab;
    float spawnRange = 20.0f;
    // Start is called before the first frame update
    void Start()
    {
        int puntos = 100;
        // size(1024,512); //setea width y height (de acuerdo al tamaño de la imagen)
        // surf = loadImage("Moon_LRO_LOLA_global_LDEM_1024_b.jpg");
        
        for(int i = 0; i < puntos; i++)
        {
            Vector3 spawnPos = GenerateSpawnPos();
            GameObject newParticle = Instantiate(particlePrefab, spawnPos, particlePrefab.transform.rotation);
            
            // Auto-agregar el script Particle si no lo tiene configurado en el Inspector
            if (newParticle.GetComponent<Particle>() == null)
            {
                newParticle.AddComponent<Particle>();
            }
        }
    }
    private Vector3 GenerateSpawnPos()
    {
        float spawnPosX = 0f;
        float spawnPosZ = 0f;
        
        // Si hay terreno activo, generamos la posición dentro de los límites físicos del terreno
        if (Terrain.activeTerrain != null)
        {
            Vector3 terrainPos = Terrain.activeTerrain.transform.position;
            Vector3 terrainSize = Terrain.activeTerrain.terrainData.size;
            
            spawnPosX = Random.Range(terrainPos.x, terrainPos.x + terrainSize.x);
            spawnPosZ = Random.Range(terrainPos.z, terrainPos.z + terrainSize.z);
        }
        else
        {
            spawnPosX = Random.Range(-spawnRange, spawnRange);
            spawnPosZ = Random.Range(-spawnRange, spawnRange);
        }

        Vector3 randomPos = new Vector3(spawnPosX, 0, spawnPosZ);
        
        // Para que la partícula nazca pegada al terreno (la caja de huevo) sin usar RigidBody
        if (Terrain.activeTerrain != null)
        {
            // SampleHeight funciona con coordenadas del mundo, por lo que randomPos debe ser una posición global.
            randomPos.y = Terrain.activeTerrain.SampleHeight(randomPos) + Terrain.activeTerrain.transform.position.y;
        }
        
        return randomPos;
    }
    void SpawnParticleWave(int elementsN)
    {
        for (int j = 0; j < elementsN; j++)
        {
            Vector3 spawnPos = GenerateSpawnPos();
            GameObject newParticle = Instantiate(particlePrefab, spawnPos, particlePrefab.transform.rotation);
            if (newParticle.GetComponent<Particle>() == null)
            {
                newParticle.AddComponent<Particle>();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
