using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Top-left HUD: a row of hearts for health and a row of capsules for mana.
// Built automatically by GameUIBootstrap. Hearts and capsules fill up/drain smoothly.
public class HudUI : MonoBehaviour
{
    public RectTransform heartRow;
    public RectTransform manaRow;

    [Tooltip("HP per heart. 10 = one heart per bandit hit.")]
    public int healthPerHeart = 10;
    [Tooltip("Mana per capsule.")]
    public int manaPerCapsule = 20;
    public float heartSize = 48f;   // 3x the 16px heart
    public float capsuleSize = 45f; // 3x the 15px capsule

    private Sprite heartFull, heartEmpty, manaFull, manaEmpty;
    private readonly List<Image> hearts = new List<Image>();
    private readonly List<Image> capsules = new List<Image>();
    private Player player;
    private PlayerSkills skills;

    private void Awake()
    {
        heartFull = Resources.Load<Sprite>("Icons/heart_full");
        heartEmpty = Resources.Load<Sprite>("Icons/heart_empty");
        manaFull = Resources.Load<Sprite>("Icons/mana_full");
        manaEmpty = Resources.Load<Sprite>("Icons/mana_empty");
    }

    private void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null) return;
            skills = player.GetComponent<PlayerSkills>();
        }

        // Health
        int heartCount = Mathf.Max(1, Mathf.CeilToInt(player.maxHealth / (float)healthPerHeart));
        EnsureIcons(hearts, heartRow, heartCount, heartEmpty, heartFull, heartSize, Image.FillMethod.Horizontal);
        for (int i = 0; i < hearts.Count; i++)
            hearts[i].fillAmount = Mathf.Clamp01((player.health - i * healthPerHeart) / (float)healthPerHeart);

        // Mana (only when the player uses mana)
        manaRow.gameObject.SetActive(skills != null);
        if (skills != null)
        {
            int capsuleCount = Mathf.Max(1, Mathf.CeilToInt(skills.maxMana / (float)manaPerCapsule));
            EnsureIcons(capsules, manaRow, capsuleCount, manaEmpty, manaFull, capsuleSize, Image.FillMethod.Vertical);
            for (int i = 0; i < capsules.Count; i++)
                capsules[i].fillAmount = Mathf.Clamp01((skills.Mana - i * manaPerCapsule) / manaPerCapsule);
        }
    }

    // Each icon is an "empty" image with a "full" image on top that fills from 0 to 1.
    private static void EnsureIcons(List<Image> fills, RectTransform row, int count, Sprite empty, Sprite full,
                                    float size, Image.FillMethod method)
    {
        while (fills.Count < count)
        {
            GameObject slot = new GameObject("Icon", typeof(RectTransform));
            slot.transform.SetParent(row, false);
            LayoutElement le = slot.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = size;
            Image back = slot.AddComponent<Image>();
            back.sprite = empty;
            back.preserveAspect = true;
            back.raycastTarget = false;

            GameObject front = new GameObject("Fill", typeof(RectTransform));
            front.transform.SetParent(slot.transform, false);
            RectTransform fr = (RectTransform)front.transform;
            fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
            Image fill = front.AddComponent<Image>();
            fill.sprite = full;
            fill.preserveAspect = true;
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = method;
            fill.fillOrigin = 0;
            fills.Add(fill);
        }
        for (int i = 0; i < fills.Count; i++)
            fills[i].transform.parent.gameObject.SetActive(i < count);
        while (fills.Count > count) fills.RemoveAt(fills.Count - 1);
    }
}
