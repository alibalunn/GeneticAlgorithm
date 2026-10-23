using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PopulationManager : MonoBehaviour
{
    public enum HuntMode { Brightest, Dullest, Largest, Smallest }

    public DNA personPrefab;
    public Camera sceneCamera;

    public int populationSize = 10;
    public float generationDuration = 10f;
    public float selectedFraction = 0.5f;
    public float mutationRate = 0.02f;

    public HuntMode huntMode = HuntMode.Brightest;

    public bool useFixedSeed = true;
    public int fixedSeed = 12345;
    public float spawnMargin = 0.8f;

    private struct Genome
    {
        public float r, g, b, s;
    }

    private readonly List<DNA> population = new List<DNA>();
    private readonly List<string> history = new List<string>();
    private int generation = 1;
    private float elapsed;

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        StartExperiment();
    }

    private void StartExperiment()
    {
        foreach (DNA d in population)
            if (d != null) 
                Destroy(d.gameObject);

        population.Clear();
        history.Clear();

        if (useFixedSeed) 
            Random.InitState(fixedSeed);

        generation = 1;
        elapsed = 0f;

        for (int i = 0; i < populationSize; i++)
        {
            Genome genome = new Genome
            {
                r = Random.value,
                g = Random.value,
                b = Random.value,
                s = RandomSize()
            };

            Spawn(genome);
        }
    }

    private void Update()
    {
        HandleKeys();

        elapsed += Time.deltaTime;
        HandleHunterClick();

        bool allDead = population.All(d => d.IsDead);

        if (elapsed >= generationDuration || allDead)
            BreedNewPopulation();
    }

    private void HandleKeys()
    {
        if (Keyboard.current == null) 
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) 
            huntMode = HuntMode.Brightest;

        if (Keyboard.current.digit2Key.wasPressedThisFrame) 
            huntMode = HuntMode.Dullest;

        if (Keyboard.current.digit3Key.wasPressedThisFrame) 
            huntMode = HuntMode.Largest;

        if (Keyboard.current.digit4Key.wasPressedThisFrame) 
            huntMode = HuntMode.Smallest;

        if (Keyboard.current.spaceKey.wasPressedThisFrame) 
            BreedNewPopulation();

        if (Keyboard.current.rKey.wasPressedThisFrame) 
            StartExperiment();
    }

    private void HandleHunterClick()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) 
            return;

        Vector2 p = Mouse.current.position.ReadValue();
        Vector3 world = sceneCamera.ScreenToWorldPoint(new Vector3(p.x, p.y, -sceneCamera.transform.position.z));

        Collider2D hit = Physics2D.OverlapPoint((Vector2)world);

        if (hit != null && hit.TryGetComponent(out DNA target))
            target.Kill(elapsed);
    }

    private void BreedNewPopulation()
    {
        foreach (DNA d in population)
            d.FinishGeneration(generationDuration);

        RecordStats();

        List<DNA> ranked = population
            .OrderBy(_ => Random.value)
            .OrderByDescending(d => d.SurvivalTime)
            .ToList();

        int k = Mathf.Clamp(Mathf.CeilToInt(populationSize * selectedFraction), 2, ranked.Count);

        List<Genome> children = new List<Genome>();
        for (int i = 0; i < populationSize; i++)
        {
            DNA mother = ranked[Random.Range(0, k)];
            DNA father = ranked[Random.Range(0, k)];
            children.Add(Breed(mother, father));
        }

        foreach (DNA old in population) 
            Destroy(old.gameObject);

        population.Clear();

        foreach (Genome genome in children) 
            Spawn(genome);

        elapsed = 0f;
        generation++;
    }

    private Genome Breed(DNA mother, DNA father)
    {
        return new Genome
        {
            r = InheritOrMutate(mother.r, father.r, Random.value),
            g = InheritOrMutate(mother.g, father.g, Random.value),
            b = InheritOrMutate(mother.b, father.b, Random.value),
            s = InheritOrMutate(mother.s, father.s, RandomSize())
        };
    }
    private float InheritOrMutate(float mother, float father, float mutatedValue)
    {
        float inherited = Random.value < 0.5f ? mother : father;

        if (Random.value < mutationRate) 
            return mutatedValue;

        return inherited;
    }

    private void Spawn(Genome genome)
    {
        DNA person = Instantiate(personPrefab, RandomPosition(), Quaternion.identity);
        person.Initialize(genome.r, genome.g, genome.b, genome.s);
        population.Add(person);
    }

    private float RandomSize()
    {
        return Random.Range(DNA.MinSize, DNA.MaxSize);
    }

    private Vector3 RandomPosition()
    {
        float halfH = sceneCamera.orthographicSize - spawnMargin;
        float halfW = sceneCamera.orthographicSize * sceneCamera.aspect - spawnMargin;

        Vector3 cam = sceneCamera.transform.position;

        return new Vector3(
            cam.x + Random.Range(-halfW, halfW),
            cam.y + Random.Range(-halfH, halfH),
            0f);
    }

    private float MeanLuminance() => population.Count == 0 ? 0f : population.Average(d => d.Luminance);
    private float MeanSize() => population.Count == 0 ? 0f : population.Average(d => d.s);

    private void RecordStats()
    {
        int survivors = population.Count(d => !d.IsDead);
        string line = $"Nesil {generation} | {huntMode} | Ort. L = {MeanLuminance():F3} | " +
                      $"Ort. boyut = {MeanSize():F2} | Hayatta kalan = {survivors}";
        history.Add(line);
        Debug.Log(line);
    }

    private string ModeInstruction()
    {
        switch (huntMode)
        {
            case HuntMode.Brightest: 
                return "PARLAK olanlara tikla";

            case HuntMode.Dullest: 
                return "MAT (koyu) olanlara tikla";

            case HuntMode.Largest: 
                return "BUYUK olanlara tikla";

            default: 
                return "KUCUK olanlara tikla";
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        style.normal.textColor = Color.white;

        float y = 10f;
        GUI.Label(new Rect(10, y, 700, 24),
            $"Nesil {generation}  |  Sure: {Mathf.Max(0f, generationDuration - elapsed):F1}s", style);

        y += 24;
        GUI.Label(new Rect(10, y, 700, 24), $"AV MODU [1-4]: {ModeInstruction()}", style);

        y += 24;
        GUI.Label(new Rect(10, y, 700, 24), $"Ort. parlaklik L = {MeanLuminance():F3}  |  Ort. boyut = {MeanSize():F2}", style);

        y += 24;
        GUI.Label(new Rect(10, y, 700, 24), "Space: nesli bitir  |  R: deneyi sifirla", style);
        y += 30;

        int start = Mathf.Max(0, history.Count - 12);

        for (int i = start; i < history.Count; i++)
        {
            GUI.Label(new Rect(10, y, 900, 22), history[i], style);
            y += 22;
        }
    }
}