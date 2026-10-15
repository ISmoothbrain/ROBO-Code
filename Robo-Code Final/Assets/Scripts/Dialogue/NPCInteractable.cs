using TMPro;
using UnityEngine;

// Put this on an NPC or a shop. Walk up, press E to talk.
// Tick "Opens Shop" to offer a shop at the end of the conversation.
// A trigger collider is added automatically if the object doesn't have one.
public class NPCInteractable : MonoBehaviour
{
    [Header("Dialogue")]
    public string npcName = "Shopkeeper";
    [TextArea(2, 4)]
    public string[] lines =
    {
        "Welcome, traveller! Coins from those bandits spend just fine here.",
        "Potions, power-ups... take a look."
    };

    [Header("Shop")]
    public bool opensShop = true;
    public string shopChoice = "Browse wares";
    public string leaveChoice = "Leave";
    [Tooltip("Leave empty to sell the default stock (Health Potion, Damage Boost, Heart Container, Quick Snack).")]
    public ShopItem[] shopItems;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public string promptText = "[E] Talk";
    public Vector3 promptOffset = new Vector3(0f, 3.6f, 0f);

    private bool playerInRange;
    private TextMeshPro prompt;

    private void Reset()
    {
        EnsureTrigger();
    }

    private void Awake()
    {
        EnsureTrigger();
        CreatePrompt();
    }

    private void EnsureTrigger()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                box.size = sr.sprite.bounds.size;
                box.offset = sr.sprite.bounds.center;
            }
            c = box;
        }
        c.isTrigger = true;
    }

    private void CreatePrompt()
    {
        GameObject go = new GameObject("Prompt");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = promptOffset;

        prompt = go.AddComponent<TextMeshPro>();
        UIFont.Apply(prompt);
        prompt.text = promptText;
        prompt.fontSize = 4f;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.color = Color.white;
        prompt.rectTransform.sizeDelta = new Vector2(6f, 1f);
        prompt.sortingOrder = 200;
        go.SetActive(false);
    }

    private void Update()
    {
        // Keep the prompt readable even if this object is flipped with a negative scale.
        if (prompt != null)
        {
            Vector3 s = prompt.transform.localScale;
            s.x = Mathf.Abs(s.x) * Mathf.Sign(transform.lossyScale.x);
            prompt.transform.localScale = s;
        }

        bool busy = DialogueUI.IsOpen || ShopUI.IsOpen || BattleManager.InBattle || CodeChallengeUI.IsOpen;
        if (prompt != null) prompt.gameObject.SetActive(playerInRange && !busy);

        if (!playerInRange || busy || !Input.GetKeyDown(interactKey)) return;
        Talk();
    }

    public void Talk()
    {
        if (DialogueUI.Instance == null)
        {
            Debug.LogWarning($"{name}: no DialogueUI found. It's created automatically when a level with a Player loads.");
            return;
        }

        if (opensShop)
        {
            DialogueUI.Instance.Show(npcName, lines, new[] { shopChoice, leaveChoice }, choice =>
            {
                if (choice == 0 && ShopUI.Instance != null) ShopUI.Instance.Open(npcName, shopItems);
            });
        }
        else
        {
            DialogueUI.Instance.Show(npcName, lines);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}
