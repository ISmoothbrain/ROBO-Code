using UnityEngine;
using UnityEngine.UI;

// Lives on its own world-space Canvas, instantiated by BanditEnemy at runtime -
// it is NOT parented under the enemy. BanditEnemy flips by setting
// transform.localScale.x to -1, and a child of that transform would mirror
// (and un-readable-text) along with it. Following position only in LateUpdate
// avoids that.
public class EnemyHealthBar : MonoBehaviour
{
    public Transform target;              // the enemy to follow, set by BanditEnemy on spawn
    public Vector3 offset = new Vector3(0f, 1.2f, 0f);
    public Image fillImage;               // Image Type = Filled, Fill Method = Horizontal
    public Canvas canvas;                 // auto-filled from this object if left empty
    public bool hideWhenFull = false;

    void Awake()
    {
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }
    }

    void OnEnable()
    {
        ApplyColor(HealthBarSettings.CurrentColor);
        HealthBarSettings.OnHealthBarColorChanged += ApplyColor;
    }

    void OnDisable()
    {
        HealthBarSettings.OnHealthBarColorChanged -= ApplyColor;
    }

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }

    public void SetHealth(float current, float max)
    {
        if (fillImage == null) return;

        float pct = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        fillImage.fillAmount = pct;

        if (hideWhenFull && canvas != null)
        {
            canvas.enabled = pct < 1f;
        }
    }

    private void ApplyColor(Color color)
    {
        if (fillImage != null)
        {
            fillImage.color = color;
        }
    }
}
