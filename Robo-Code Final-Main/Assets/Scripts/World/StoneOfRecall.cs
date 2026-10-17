using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The player spawns at the Stone of Recall. Walk up to it and press E to choose a level to travel to.
// Locked levels open up as you complete the one before (see LevelProgress).
// Added automatically to any object named "...Stone of Recall" (WorldBootstrap).
public class StoneOfRecall : MonoBehaviour
{
    public static bool MenuOpen { get; private set; }

    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 1.6f;
    [Tooltip("Where the player appears, relative to the stone.")]
    public Vector3 spawnOffset = new Vector3(1.2f, 0.8f, 0f);

    private Player player;
    private TextMeshPro prompt;
    private static GameObject menuRoot;
    private static Transform listParent;
    private static TextMeshProUGUI menuInfo;

    public Vector3 SpawnPosition => transform.position + spawnOffset;

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
        GameObject go = new GameObject("Prompt");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        prompt = go.AddComponent<TextMeshPro>();
        UIFont.Apply(prompt);
        prompt.text = "[E] Stone of Recall";
        prompt.fontSize = 4f;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.rectTransform.sizeDelta = new Vector2(8f, 1f);
        prompt.sortingOrder = 200;
        go.SetActive(false);
    }

    private void OnDestroy()
    {
        if (MenuOpen) MenuOpen = false;
    }

    private void Update()
    {
        if (player == null) { player = FindAnyObjectByType<Player>(); return; }

        if (MenuOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseMenu();
            return;
        }

        bool near = Vector2.Distance(player.transform.position, transform.position) <= interactRange && !player.IsDead;
        bool busy = UIBusy.AnyOpen;
        if (prompt != null) prompt.gameObject.SetActive(near && !busy);
        if (near && !busy && !UIBusy.ClosedThisFrame && Input.GetKeyDown(interactKey)) OpenMenu();
    }

    [ContextMenu("Reset Level Progress")]
    private void ResetProgress()
    {
        LevelProgress.ResetAll();
        Debug.Log("Level progress reset: only The Safe Haven is unlocked.");
    }

    // ---------------------------------------------------------------- the travel menu

    private void OpenMenu()
    {
        if (menuRoot == null) BuildMenu();
        foreach (Transform child in listParent) Destroy(child.gameObject);

        LevelProgress.Destination here = LevelProgress.Current();
        foreach (LevelProgress.Destination d in LevelProgress.Destinations)
        {
            bool unlocked = LevelProgress.IsUnlocked(d);
            bool isHere = here == d;
            string label = d.displayName + (isHere ? "   (you are here)" : unlocked ? "" : "   [Locked]");
            Button b = UIBuild.Button(listParent, d.id, label, 30, 64);
            Image img = b.GetComponent<Image>();
            img.color = isHere ? new Color(0.2f, 0.3f, 0.4f) : unlocked ? new Color(0.18f, 0.42f, 0.32f) : new Color(0.22f, 0.22f, 0.26f);
            b.interactable = unlocked && !isHere;
            LevelProgress.Destination target = d;
            b.onClick.AddListener(() => Travel(target));
        }

        LevelProgress.Destination next = here != null ? LevelProgress.Next(here) : null;
        menuInfo.text = next != null && !LevelProgress.IsUnlocked(next)
            ? $"Complete {here.displayName} to unlock {next.displayName}."
            : "Choose where to travel.";

        menuRoot.SetActive(true);
        MenuOpen = true;
    }

    private static void CloseMenu()
    {
        if (menuRoot != null) menuRoot.SetActive(false);
        MenuOpen = false;
        UIBusy.MarkClosed();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private static void Travel(LevelProgress.Destination d)
    {
        int index = LevelProgress.BuildIndexOf(d);
        if (index < 0)
        {
            menuInfo.text = $"Couldn't find the scene for {d.displayName}. Add it in File > Build Settings.";
            return;
        }
        CloseMenu();
        SceneTransition.LoadScene(index, true, d.displayName); // fade, title, arrive at the stone
    }

    private static void BuildMenu()
    {
        Canvas canvas = UIBuild.Canvas(null, "StoneOfRecallMenu", 95);
        menuRoot = UIBuild.Node("Root", canvas.transform);
        UIBuild.Stretch(menuRoot, 0, 0, 1, 1);
        menuRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        GameObject window = UIBuild.Node("Window", menuRoot.transform);
        RectTransform wr = (RectTransform)window.transform;
        wr.anchorMin = wr.anchorMax = new Vector2(0.5f, 0.5f);
        wr.sizeDelta = new Vector2(760f, 0f);
        UIBuild.Sliced(window, Resources.Load<Sprite>("UI/panel"), new Color(0.04f, 0.09f, 0.15f, 1f));
        VerticalLayoutGroup v = window.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(32, 32, 24, 26);
        v.spacing = 12;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        window.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        UIBuild.Label(UIBuild.Node("Title", window.transform), 40, FontStyles.Bold, new Color(0.55f, 0.88f, 1f), TextAlignmentOptions.Center).text = "Stone of Recall";

        GameObject list = UIBuild.Node("Destinations", window.transform);
        VerticalLayoutGroup lv = list.AddComponent<VerticalLayoutGroup>();
        lv.spacing = 10;
        lv.childControlWidth = lv.childControlHeight = true;
        lv.childForceExpandWidth = true;
        lv.childForceExpandHeight = false;
        listParent = list.transform;

        menuInfo = UIBuild.Label(UIBuild.Node("Info", window.transform), 22, FontStyles.Normal, new Color(0.8f, 0.86f, 0.92f), TextAlignmentOptions.Center);
        Button close = UIBuild.Button(window.transform, "Close", "Close  [Esc]", 24, 52);
        close.onClick.AddListener(CloseMenu);

        menuRoot.SetActive(false);
    }
}
