using UnityEngine;

// Lets menus know a dialogue/shop window just closed this frame, so a single
// Escape press doesn't close the shop AND open the pause menu.
public static class UIBusy
{
    private static int lastCloseFrame = -1;

    public static bool ClosedThisFrame => Time.frameCount == lastCloseFrame;

    public static void MarkClosed() => lastCloseFrame = Time.frameCount;
}
