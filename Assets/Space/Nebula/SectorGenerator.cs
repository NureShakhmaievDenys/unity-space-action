using UnityEngine;
using System.Collections.Generic;

public class SectorGenerator : MonoBehaviour

{
    private int currentAsteroids = 0;
    [System.Serializable]
    public class ClusterSettings
    {
        public int minClusters = 3;
        public int maxClusters = 6;

        public int minAsteroids = 20;
        public int maxAsteroids = 50;

        public float clusterRadius = 250f;
        public float minDistanceFromCenter = 500f;
    }

    [System.Serializable]
    public class SuperClusterSettings
    {
        public int count = 1;
        public int minAsteroids = 80;
        public int maxAsteroids = 150;

        public float radius = 600f;
        public float minDistanceFromCenter = 800f;
    }

    [System.Serializable]
    public class RingSettings
    {
        public int minAsteroids = 80;
        public int maxAsteroids = 150;

        public float radius = 1200f;
        public float thickness = 150f;
    }
    [System.Serializable]
    public class SectorSkybox
    {
        public Material skybox;
        public Color sectorColor;
    }

    [System.Serializable]
    public class AsteroidGroup
    {
        public GameObject[] prefabs;
        public float spawnChance;
        public Vector2 scaleRange;
    }

    [Header("Seed")]
    public int seed = 12345;

    [Header("Skyboxes")]
    public SectorSkybox[] skyboxes;

    [Header("Planet Prefab")]
    public GameObject planetPrefab;

    [Header("Planet Textures")]
    public Texture2D[] aridPlanets;
    public Texture2D[] barrenPlanets;
    public Texture2D[] dustyPlanets;
    public Texture2D[] grassPlanets;
    public Texture2D[] junglePlanets;
    public Texture2D[] marshPlanets;
    public Texture2D[] martianPlanets;
    public Texture2D[] methanePlanets;
    public Texture2D[] sandyPlanets;
    public Texture2D[] snowyPlanets;
    public Texture2D[] tundraPlanets;
    public Texture2D[] gasGiantPlanets;

    [Header("Lighting")]
    public Light starLight;

    [Header("Planet Settings")]
    public int minPlanets = 0;
    public int maxPlanets = 3;
    public float sectorRadius = 40000f;

    [Header("Asteroids")]
    public int minAsteroids = 50;
    public int maxAsteroids = 200;
    public float asteroidRadius = 35000f;
    public float minAsteroidDistance = 500f;
    [Header("Asteroid Textures")]
    public Texture2D[] asteroidTextures;
    [Header("Asteroid Groups")]
    public AsteroidGroup[] asteroidGroups;
    [Header("Cluster Settings")]
    public ClusterSettings clusters;

    [Header("Super Clusters")]
    public SuperClusterSettings superClusters;

    [Header("Asteroid Ring")]
    public RingSettings ring;
    [Header("Field Settings")]
    public float fieldRadius = 2400f;
    void Start()
    {
        GenerateSector();
    }

    void GenerateSector()
    {
        Random.InitState(seed);

        if (skyboxes == null || skyboxes.Length == 0)
        {
            Debug.LogWarning("No skyboxes assigned!");
            return;
        }

        SectorSkybox sector = skyboxes[Random.Range(0, skyboxes.Length)];

        RenderSettings.skybox = sector.skybox;

        if (starLight != null)
            starLight.color = sector.sectorColor;

        GeneratePlanets(sector.sectorColor);
        GenerateAsteroids(); // 👈 добавили
    }
    void GeneratePlanets(Color sectorColor)
    {
        if (planetPrefab == null)
        {
            Debug.LogWarning("Planet prefab missing!");
            return;
        }

        int planetCount = Random.Range(minPlanets, maxPlanets + 1);

        List<Vector3> planetPositions = new List<Vector3>();
        float minDistance = 30000f;

        for (int i = 0; i < planetCount; i++)
        {
            Vector3 position;
            int attempts = 0;

            do
            {
                position = Random.onUnitSphere * sectorRadius;
                attempts++;
            }
            while (IsTooClose(position, planetPositions, minDistance) && attempts < 30);

            planetPositions.Add(position);

            GameObject planet = Instantiate(planetPrefab, position, Quaternion.identity);

            SetupPlanet(planet, sectorColor);
        }
    }
    // ===================== ПЛАНЕТЫ =====================

   

    void SetupPlanet(GameObject planet, Color sectorColor)
    {
        Renderer renderer = planet.GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        int type = Random.Range(0, 12);
        Texture2D texture = GetPlanetTexture(type);

        Material mat = new Material(renderer.sharedMaterial);

        if (texture != null)
            mat.SetTexture("_BaseMap", texture);

        renderer.material = mat;

        float size = Random.Range(5000f, 12000f);
        if (type == 11)
            size = Random.Range(15000f, 25000f);

        planet.transform.localScale = Vector3.one * size;

        float tilt = Random.Range(-35f, 35f);

        planet.transform.rotation =
            Quaternion.Euler(tilt, Random.Range(0f, 360f), 0f);

        PlanetRotation rotation = planet.AddComponent<PlanetRotation>();

        rotation.rotationSpeed = (type == 11)
            ? Random.Range(4f, 10f)
            : Random.Range(1f, 5f);

        Transform atmosphere = planet.transform.Find("PlanetAtmosphere");

        if (atmosphere != null)
        {
            Renderer atmRenderer = atmosphere.GetComponent<Renderer>();

            if (atmRenderer != null)
                atmRenderer.material.SetColor("_BaseColor", sectorColor);
        }
    }
    void GenerateAsteroids()
    {
        if (asteroidGroups == null || asteroidGroups.Length == 0)
        {
            Debug.LogWarning("No asteroid groups!");
            return;
        }

     

        GenerateClusters(fieldRadius);
        GenerateSuperClusters(fieldRadius);
        GenerateAsteroidRing(fieldRadius);
    }
    void GenerateClusters(float fieldRadius)
    {
        int clusterCount = Random.Range(clusters.minClusters, clusters.maxClusters);

        for (int c = 0; c < clusterCount; c++)
        {
            Vector3 center = Random.onUnitSphere * 
                             Random.Range(clusters.minDistanceFromCenter, fieldRadius);

            int count = Random.Range(clusters.minAsteroids, clusters.maxAsteroids);

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = center + Random.insideUnitSphere * clusters.clusterRadius;

                if (!ValidPosition(pos)) continue;
                if (!PassNoise(pos)) continue;

                SpawnAsteroid(pos);
            }
        }
    }
    void GenerateSuperClusters(float fieldRadius)
    {
        for (int c = 0; c < superClusters.count; c++)
        {
            Vector3 center = Random.onUnitSphere * 
                             Random.Range(superClusters.minDistanceFromCenter, fieldRadius);

            int count = Random.Range(superClusters.minAsteroids, superClusters.maxAsteroids);

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = center + Random.insideUnitSphere * superClusters.radius;

                if (!ValidPosition(pos)) continue;
                if (!PassNoise(pos)) continue;

                SpawnAsteroid(pos);
            }
        }
    }
    void GenerateAsteroidRing(float fieldRadius)
    {
        int count = Random.Range(ring.minAsteroids, ring.maxAsteroids);

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2);

            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * ring.radius,
                Random.Range(-ring.thickness, ring.thickness),
                Mathf.Sin(angle) * ring.radius
            );

            if (!ValidPosition(pos)) continue;
            if (!PassNoise(pos)) continue;

            SpawnAsteroid(pos);
        }
    }
    bool ValidPosition(Vector3 pos)
    {
        // ограничение по количеству
        if (currentAsteroids >= maxAsteroids)
            return false;

        // ограничение по радиусу сектора
        if (pos.magnitude > fieldRadius)
            return false;

        return true;
    }
    bool PassNoise(Vector3 pos)
    {
        float scale = 0.002f;

        float noise = Mathf.PerlinNoise(
            pos.x * scale + seed,
            pos.z * scale + seed
        );

        return noise > 0.4f; // чем больше — тем реже астероиды
    }
    void SpawnAsteroid(Vector3 pos)
    {
        GameObject prefab = GetRandomAsteroid();
        if (prefab == null) return;

        GameObject asteroid = Instantiate(prefab, pos, Random.rotation);

        float scale = Mathf.Max(100f, GetAsteroidScale(prefab));
        asteroid.transform.localScale = Vector3.one * scale;

        Renderer renderer = asteroid.GetComponentInChildren<Renderer>();

        if (renderer != null && asteroidTextures.Length > 0)
        {
            Texture2D tex = asteroidTextures[Random.Range(0, asteroidTextures.Length)];

            renderer.material.SetTexture("_BaseMap", tex);

        }

        currentAsteroids++;
    }
    GameObject GetRandomAsteroid()
    {
        float total = 0f;

        foreach (var g in asteroidGroups)
            total += g.spawnChance;

        float rand = Random.Range(0, total);

        foreach (var g in asteroidGroups)
        {
            if (rand < g.spawnChance)
                return GetRandom(g.prefabs);

            rand -= g.spawnChance;
        }

        return null;
    }

    float GetAsteroidScale(GameObject prefab)
    {
        foreach (var g in asteroidGroups)
        {
            if (System.Array.Exists(g.prefabs, p => p == prefab))
            {
                return Random.Range(g.scaleRange.x, g.scaleRange.y);
            }
        }

        return 1000f;
    }

    // ===================== ОБЩЕЕ =====================

    bool IsTooClose(Vector3 pos, List<Vector3> existing, float minDistance)
    {
        foreach (Vector3 p in existing)
        {
            if (Vector3.Distance(pos, p) < minDistance)
                return true;
        }

        return false;
    }

    Texture2D GetPlanetTexture(int type)
    {
        switch (type)
        {
            case 0: return GetRandom(aridPlanets);
            case 1: return GetRandom(barrenPlanets);
            case 2: return GetRandom(dustyPlanets);
            case 3: return GetRandom(grassPlanets);
            case 4: return GetRandom(junglePlanets);
            case 5: return GetRandom(marshPlanets);
            case 6: return GetRandom(martianPlanets);
            case 7: return GetRandom(methanePlanets);
            case 8: return GetRandom(sandyPlanets);
            case 9: return GetRandom(snowyPlanets);
            case 10: return GetRandom(tundraPlanets);
            case 11: return GetRandom(gasGiantPlanets);
        }

        return null;
    }

    GameObject GetRandom(GameObject[] array)
    {
        if (array == null || array.Length == 0)
            return null;

        return array[Random.Range(0, array.Length)];
    }

    Texture2D GetRandom(Texture2D[] array)
    {
        if (array == null || array.Length == 0)
            return null;

        return array[Random.Range(0, array.Length)];
    }
}