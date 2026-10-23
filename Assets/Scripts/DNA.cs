using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DNA : MonoBehaviour
{
    public const float MinSize = 0.5f;
    public const float MaxSize = 1.5f;

    public float r;
    public float g;
    public float b;
    public float s = 1f;

    public bool IsDead { get; private set; }
    public float SurvivalTime { get; private set; }
    public float Luminance => 0.2126f * r + 0.7152f * g + 0.0722f * b;

    private SpriteRenderer spriteRenderer;
    private Collider2D hitCollider;
    private Vector3 originalScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitCollider = GetComponent<Collider2D>();
        originalScale = transform.localScale;
    }

    public void Initialize(float red, float green, float blue, float size)
    {
        r = Mathf.Clamp01(red);
        g = Mathf.Clamp01(green);
        b = Mathf.Clamp01(blue);
        s = Mathf.Clamp(size, MinSize, MaxSize);

        IsDead = false;
        SurvivalTime = 0f;

        spriteRenderer.color = new Color(r, g, b, 1f);
        transform.localScale = originalScale * s;

        spriteRenderer.enabled = true;
        hitCollider.enabled = true;
    }

    public void Kill(float elapsedTime)
    {
        if (IsDead) 
            return;

        IsDead = true;
        SurvivalTime = elapsedTime;

        HideFromHunter();
    }

    public void FinishGeneration(float duration)
    {
        if (!IsDead) 
            SurvivalTime = duration;
    }

    private void HideFromHunter()
    {
        spriteRenderer.enabled = false;
        hitCollider.enabled = false;
    }
}