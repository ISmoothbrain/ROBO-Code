using TMPro;
using UnityEngine;

// Press E next to a chest to open it and collect coins. Uses the chest's own opening animation.
// Added automatically to Cainos chests: Golden = 20 coins, Silver = 10, Wooden = 5.
// A chest stays open (and empty) for the rest of the game once looted.
public class LootChest : MonoBehaviour
{
    public int coins = 10;
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 1.4f;

    private Player player;
    private Animator animator;
    private TextMeshPro prompt;
    private bool opened;

    private string SaveKey => GameSave.Prefix + "Chest_" + gameObject.scene.name + "_" + Mathf.RoundToInt(transform.position.x * 10) + "_" + Mathf.RoundToInt(transform.position.y * 10);

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
        animator = GetComponentInChildren<Animator>();
        opened = PlayerPrefs.GetInt(SaveKey, 0) == 1;
        if (opened && animator != null) animator.SetBool("IsOpened", true);

        GameObject go = new GameObject("Prompt");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        prompt = go.AddComponent<TextMeshPro>();
        UIFont.Apply(prompt);
        prompt.fontSize = 4f;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.rectTransform.sizeDelta = new Vector2(6f, 1f);
        prompt.sortingOrder = 200;
        go.SetActive(false);
    }

    private void Update()
    {
        if (player == null) { player = FindAnyObjectByType<Player>(); return; }
        bool near = Vector2.Distance(player.transform.position, transform.position) <= interactRange && !player.IsDead;
        bool show = near && !opened && !UIBusy.AnyOpen && !StoneOfRecall.MenuOpen;
        if (prompt != null)
        {
            prompt.gameObject.SetActive(show);
            prompt.text = $"[E] Open (+{coins} coins)";
        }
        if (show && Input.GetKeyDown(interactKey)) Open();
    }

    private void Open()
    {
        opened = true;
        if (animator != null) animator.SetBool("IsOpened", true);
        CoinCounter.Add(coins);
        PlayerPrefs.SetInt(SaveKey, 1);
        PlayerPrefs.Save();
        if (prompt != null) prompt.gameObject.SetActive(false);
    }

    [ContextMenu("Reset (close this chest again)")]
    private void ResetChest()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        opened = false;
        if (animator != null) animator.SetBool("IsOpened", false);
    }
}
