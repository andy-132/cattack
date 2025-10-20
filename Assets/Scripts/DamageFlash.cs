using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DamageFlash : MonoBehaviour
{
    [SerializeField] Color flashColor = new Color(1f, 0.4f, 0.4f);
    [SerializeField] float flashTime = 0.08f;

    SpriteRenderer sr;
    Color original;
    float t;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        original = sr.color;
    }

    public void DoFlash()
    {
        t = flashTime;
        sr.color = flashColor;
    }

    void Update()
    {
        if (t > 0f)
        {
            t -= Time.deltaTime;
            if (t <= 0f) sr.color = original;
        }
    }
}