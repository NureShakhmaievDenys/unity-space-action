using UnityEngine;

public class PlanetGenerator : MonoBehaviour
{ 
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

    [Header("Planet Settings")]
    public float sectorRadius = 40000f;

    void Start()
    {
        GeneratePlanets();
    }

    void GeneratePlanets()
    {
        int planetCount = Random.Range(1, 4);

        for (int i = 0; i < planetCount; i++)
        {
            Vector3 position = Random.onUnitSphere * sectorRadius;

            GameObject planet = Instantiate(planetPrefab, position, Quaternion.identity);

            Renderer renderer = planet.GetComponentInChildren<Renderer>();

            int type = Random.Range(0, 12);

            Texture2D texture = GetPlanetTexture(type);

            Material mat = new Material(renderer.sharedMaterial);

            if (texture != null)
                mat.SetTexture("_BaseMap", texture);

            renderer.material = mat;

            float size = Random.Range(5000f, 12000f);

            if (type == 11)
            {
                size = Random.Range(15000f, 25000f);
            }

            planet.transform.localScale = Vector3.one * size;

            // НАКЛОН ПЛАНЕТЫ
            float tilt = Random.Range(5f, 35f);

            planet.transform.rotation =
                Quaternion.Euler(tilt, Random.Range(0f, 360f), 0f);

            // ВРАЩЕНИЕ ПЛАНЕТЫ
            PlanetRotation rotation = planet.AddComponent<PlanetRotation>();

            rotation.rotationSpeed = Random.Range(1f, 5f);

            if (type == 11) // газовые гиганты вращаются быстрее
            {
                rotation.rotationSpeed = Random.Range(4f, 10f);
            }
        }
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

        return barrenPlanets[0];
    }

    Texture2D GetRandom(Texture2D[] textures)
    {
        if (textures == null || textures.Length == 0)
            return null;

        return textures[Random.Range(0, textures.Length)];
    }
}