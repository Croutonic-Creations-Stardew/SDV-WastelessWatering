using StardewModdingAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WastelessWatering {
    class ModConfig {

        public Boolean Enabled { get; set; } = true;
        public Boolean HoldMode { get; set; } = true;
        public KeybindList ToggleKey { get; set; } = KeybindList.Parse("LeftShift + OemComma, RightShift + OemComma");

    }
}
