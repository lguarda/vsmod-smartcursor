using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using System;
using System.Collections.Generic;

using Vintagestory.API.MathTools;

namespace SmartCursor
{

    public class SmartCursorKeybind
    {

        public const string HOTKEY_SMARTCURSOR = "smartcursor";
        public const string HOTKEY_SMARTCURSOR_TOGGLE = "smartcursor toggle";
        public const string HOTKEY_SMARTCURSOR_ONE_SHOT = "smartcursor one shot";
        public const string HOTKEY_SMARTCURSOR_BLACKLIST_TOGGLE = "smartcursor blacklist toggle";
        // this name if to reference the best mod in vs
        // https://mods.vintagestory.at/show/mod/31020
        public const string HOTKEY_SMARTCURSOR_PUTITINTHEBAG = "smartcursor put it in the bag";

        static public void RegisterClientKey(ICoreClientAPI capi, string keyCode, GlKeys key,
                                             ActionConsumable<KeyCombination> handler = null, bool altPressed = false,
                                             bool ctrlPressed = false, bool shiftPressed = false)
        {
            string keybindDisplayName = Lang.Get($"smartcursor:{keyCode}");

            capi.Input.RegisterHotKey(keyCode, $"Smart cursor: {keybindDisplayName}", key, HotkeyType.GUIOrOtherControls,
                                      altPressed, ctrlPressed, shiftPressed);

            if (handler != null)
            {
                capi.Input.SetHotKeyHandler(keyCode, handler);
            }
            else
            {
                capi.Input.SetHotKeyHandler(keyCode, (_) => true);
            }
        }
    }
}
