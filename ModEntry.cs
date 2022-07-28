using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using WastelessWatering.Integrations;
using xTile.Layers;
using Object = StardewValley.Object;

namespace WastelessWatering {
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod {


        private string[] object_names_allowed_to_water = { "bowl", "pot", "trough" };
        private ModConfig Config;

        private StardewAccessInterface? stardewAccess;

        public bool allowed_water = false;

        public int suppressed_ticks = 0;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper) {

            this.Config = this.Helper.ReadConfig<ModConfig>();

            this.Monitor.Log(Config.ToggleKey.ToString(), LogLevel.Debug);

            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.Input.ButtonsChanged += Input_ButtonsChanged;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;

            helper.Events.Input.ButtonPressed += Input_ButtonPressed;

        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e) {

            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => "Mod Active?",
                    tooltip: () => "Toggle on or off Wasteless Watering",
                    getValue: () => this.Config.Enabled,
                    setValue: value => this.Config.Enabled = value
            );
            configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => "Hold Mode",
                    tooltip: () => "Hold the Toggle Key to activate the mod instead of toggling it",
                    getValue: () => this.Config.HoldMode,
                    setValue: value => this.Config.HoldMode = value
            );

            configMenu.AddKeybindList(
                    mod: this.ModManifest,
                    name: () => "Toggle Key",
                    tooltip: () => "Toggle on or off Wasteless Watering",
                    getValue: () => this.Config.ToggleKey,
                    setValue: value => this.Config.ToggleKey = value
            );

        }

        private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e) {

            bool currently_active = this.Config.Enabled;

            if (e.Button.IsUseToolButton() && currently_active && Game1.activeClickableMenu == null) {

                if (this.Config.HoldMode && this.Config.ToggleKey.IsDown()) {
                    this.Monitor.Log("Hold Mode Activated", LogLevel.Debug);
                    return;
                }

                Farmer player = Game1.player;

                if (player.CurrentTool is WateringCan) {

                    bool suppress = true;

                    GameLocation location = Game1.currentLocation;
                    Vector2 tile_vector = Vector2.Floor(this.Helper.Input.GetCursorPosition().GrabTile);

                    if (location.CanRefillWateringCanOnTile((int)tile_vector.X, (int)tile_vector.Y)) {
                        suppress = false;
                    }

                    if (location is VolcanoDungeon) {
                        if (((VolcanoDungeon)location).isWaterTile((int)tile_vector.X, (int)tile_vector.Y)) {
                            suppress = false;
                        }
                    }

                    //pet bowls
                    Layer back_layer = location.map.GetLayer("Back");
                    if (back_layer.Tiles[(int)tile_vector.X, (int)tile_vector.Y] != null && back_layer.Tiles[(int)tile_vector.X, (int)tile_vector.Y].TileIndex == 1938) {
                        suppress = false;
                    }

                    //other objects
                    foreach (KeyValuePair<Vector2, Object> kvp in location.objects.Pairs) {
                        if (kvp.Key == tile_vector) {
                            string name = kvp.Value.Name.ToLower();
                            if (object_names_allowed_to_water.Any(x => name.Contains(x))) {
                                suppress = false;
                            }
                        }
                    }

                    if (location.terrainFeatures.ContainsKey(tile_vector)) {
                        TerrainFeature feature = Game1.currentLocation.terrainFeatures[tile_vector];
                        if (feature is HoeDirt) {
                            HoeDirt dirt = (HoeDirt)feature;
                            if (dirt.state.Value != HoeDirt.watered) {
                                suppress = false;
                            }
                        }
                    }

                    if (suppress) {

                        Helper.Input.Suppress(e.Button);

                        string text_output = $"Water use Prevented. (Wasteless Watering)";

                        Monitor.Log(text_output, LogLevel.Debug);
                        Game1.addHUDMessage(new HUDMessage(text_output, 3));

                    }

                }

            }

        }
        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e) {
            Boolean stardewAccessLoaded = Helper.ModRegistry.IsLoaded("shoaib.stardewaccess");
            if (stardewAccessLoaded) {
                stardewAccess = Helper.ModRegistry.GetApi<StardewAccessInterface>("shoaib.stardewaccess");
            }
        }

        private void Input_ButtonsChanged(object sender, ButtonsChangedEventArgs e) {

            if (Config.ToggleKey.JustPressed() && !Config.HoldMode) {

                this.Config.Enabled = !this.Config.Enabled;

                string text_output = $"Wasteless Watering " + (this.Config.Enabled ? "Activated" : "Disabled");
                Monitor.Log(text_output, LogLevel.Debug);

                Game1.addHUDMessage(new HUDMessage(text_output, 2));

                if (stardewAccess != null) {
                    stardewAccess.Say(text_output, true);
                }

                this.Helper.WriteConfig(this.Config);

            }
        }

    }
}