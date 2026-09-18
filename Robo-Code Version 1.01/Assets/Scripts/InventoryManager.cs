using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public GameObject InventoryMenu;

    private bool menuActivated = false;

    void Start()
    {
        InventoryMenu.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            menuActivated = !menuActivated;
            InventoryMenu.SetActive(menuActivated);
        }
    }
}