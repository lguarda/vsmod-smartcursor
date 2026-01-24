using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using System;
using System.Collections.Generic;

using Vintagestory.API.MathTools;


namespace SmartCursor {

public class SmartCursorKeybind {

    public const string HOTKEY_SMARTCURSOR = "smartcursor";
    public const string HOTKEY_SMARTCURSOR_TOGGLE = "smartcursor toggle";
    public const string HOTKEY_SMARTCURSOR_ONE_SHOT = "smartcursor one shot";
    public const string HOTKEY_SMARTCURSOR_BLACKLIST_TOGGLE = "smartcursor blacklist toggle";
    #if WITH_SERVER
    public const string HOTKEY_SMARTCURSOR_PLACEMENT = "smartcursor block placement";
    #endif

    static public void RegisterClientKey(ICoreClientAPI capi, string keyCode, GlKeys key, bool altPressed = false, bool ctrlPressed = false,
                             bool shiftPressed = false) {
        string keybindDisplayName = Lang.Get($"smartcursor:{keyCode}");

        capi.Input.RegisterHotKey(keyCode, $"Smart cursor: {keybindDisplayName}", key, HotkeyType.GUIOrOtherControls,
                                   altPressed, ctrlPressed, shiftPressed);
        capi.Input.SetHotKeyHandler(keyCode, (_) => true);
    }

}
}
