using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The choice at the start of your battle turn once HACK is crafted:
//   ATTACK            answer code as usual
//   HACK: OVERRIDE    take control: the enemy loses its next turn            (1 energy bar)
//   HACK: OVERLOAD    the enemy takes double damage and deals half           (1 energy bar)
// Keys 1 / 2 / 3 work too. Builds its own panel; BattleBootstrap places it on the battle screen.
public class HackMenu : MonoBehaviour
{
    private GameObject root;
    private Button[] buttons;
    private TextMeshProUGUI energyText;
    private int picked = -1;

    private void Awake() => EnsureBuilt();

    // 0 = attack, 1 = override, 2 = overload
    public IEnumerator Choose(bool canHack, int energyBars, Action<int> onDone)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);   // the battle hides non-battle objects
        EnsureBuilt();
        transform.SetAsLastSibling();

        buttons[1].interactable = buttons[2].interactable = canHack;
        energyText.text = canHack ? $"Energy: {energyBars} bar{(energyBars == 1 ? "" : "s")}  (a hack costs 1)"
                                  : "Not enough energy to hack (needs 1 bar)";
        picked = -1;
        root.SetActive(true);
        yield return null;

        while (picked < 0)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) picked = 0;
            else if (canHack && Input.GetKeyDown(KeyCode.Alpha2)) picked = 1;
            else if (canHack && Input.GetKeyDown(KeyCode.Alpha3)) picked = 2;
            yield return null;
        }
        root.SetActive(false);
        onDone(picked);
    }

    private void EnsureBuilt()
    {
        if (root != null) return;
        root = new GameObject("Root", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        RectTransform rr = (RectTransform)root.transform;
        rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
        rr.sizeDelta = new Vector2(760f, 330f);
        rr.anchoredPosition = new Vector2(0f, 40f);
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.07f, 0.12f, 0.92f);

        VerticalLayoutGroup v = root.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(28, 28, 20, 20);
        v.spacing = 12;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        UIBuild.Label(UIBuild.Node("Title", root.transform), 32, FontStyles.Bold, new Color(0.55f, 0.88f, 1f), TextAlignmentOptions.Center).text = "YOUR TURN";
        string[] labels =
        {
            "[1] ATTACK - answer code",
            "[2] HACK: OVERRIDE - enemy loses its next turn",
            "[3] HACK: OVERLOAD - double damage to it, half from it",
        };
        buttons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            int choice = i;
            buttons[i] = UIBuild.Button(root.transform, "Choice" + i, labels[i], 24, 56);
            buttons[i].onClick.AddListener(() => picked = choice);
        }
        energyText = UIBuild.Label(UIBuild.Node("Energy", root.transform), 22, FontStyles.Normal, new Color(0.6f, 0.8f, 1f), TextAlignmentOptions.Center);
        root.SetActive(false);
    }
}
