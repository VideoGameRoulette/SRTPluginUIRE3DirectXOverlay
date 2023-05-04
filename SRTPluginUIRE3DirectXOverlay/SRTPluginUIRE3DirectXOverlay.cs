using GameOverlay.Drawing;
using GameOverlay.Windows;
using SRTPluginBase;
using SRTPluginProviderRE3;
using SRTPluginProviderRE3.Structs;
using SRTPluginProviderRE3.Structs.GameStructs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace SRTPluginUIRE3DirectXOverlay
{
    public class SRTPluginUIRE3DirectXOverlay : PluginBase, IPluginUI
    {
        internal static PluginInfo _Info = new PluginInfo();
        public override IPluginInfo Info => _Info;
        public string RequiredProvider => "SRTPluginProviderRE3";
        private IPluginHostDelegates hostDelegates;
        private IGameMemoryRE3 gameMemory;

        // DirectX Overlay-specific.
        private OverlayWindow _window;
        private Graphics _graphics;
        private SharpDX.Direct2D1.WindowRenderTarget _device;

        private Font _consolasBold;

        private SolidBrush _black;
        private SolidBrush _white;
        private SolidBrush _grey;
        private SolidBrush _darkred;
        private SolidBrush _red;
        private SolidBrush _lightred;
        private SolidBrush _lightyellow;
        private SolidBrush _lightgreen;
        private SolidBrush _lawngreen;
        private SolidBrush _goldenrod;
        private SolidBrush _greydark;
        private SolidBrush _greydarker;
        private SolidBrush _darkgreen;
        private SolidBrush _darkyellow;

        private SolidBrush _lightpurple;
        private SolidBrush _darkpurple;
        private SolidBrush _lightpink;
        private SolidBrush _darkpink;

        private IReadOnlyDictionary<ItemID, SharpDX.Mathematics.Interop.RawRectangleF> itemToImageTranslation;
        private IReadOnlyDictionary<Weapon, SharpDX.Mathematics.Interop.RawRectangleF> weaponToImageTranslation;
        private SharpDX.Direct2D1.Bitmap _invItemSheet1;
        private SharpDX.Direct2D1.Bitmap _invItemSheet2;
        private int INV_SLOT_WIDTH;
        private int INV_SLOT_HEIGHT;
        public PluginConfiguration config;
        private Process GetProcess() => Process.GetProcessesByName("re3")?.FirstOrDefault();
        private Process gameProcess;
        private IntPtr gameWindowHandle;

        //STUFF
        SolidBrush HPBarColor;
        SolidBrush TextColor;

        private string PlayerName = "";

        [STAThread]
        public override int Startup(IPluginHostDelegates hostDelegates)
        {
            this.hostDelegates = hostDelegates;
            config = LoadConfiguration<PluginConfiguration>();

            gameProcess = GetProcess();
            if (gameProcess == default)
                return 1;
            gameWindowHandle = gameProcess.MainWindowHandle;

            DEVMODE devMode = default;
            devMode.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            PInvoke.EnumDisplaySettings(null, -1, ref devMode);

            // Create and initialize the overlay window.
            _window = new OverlayWindow(0, 0, devMode.dmPelsWidth, devMode.dmPelsHeight);
            _window?.Create();

            // Create and initialize the graphics object.
            _graphics = new Graphics()
            {
                MeasureFPS = false,
                PerPrimitiveAntiAliasing = false,
                TextAntiAliasing = true,
                UseMultiThreadedFactories = false,
                VSync = false,
                Width = _window.Width,
                Height = _window.Height,
                WindowHandle = _window.Handle
            };
            _graphics?.Setup();

            // Get a refernence to the underlying RenderTarget from SharpDX. This'll be used to draw portions of images.
            _device = (SharpDX.Direct2D1.WindowRenderTarget)typeof(Graphics).GetField("_device", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_graphics);

            _consolasBold = _graphics?.CreateFont("Consolas", 12, true);

            _black = _graphics?.CreateSolidBrush(0, 0, 0);
            _white = _graphics?.CreateSolidBrush(255, 255, 255);
            _grey = _graphics?.CreateSolidBrush(128, 128, 128);
            _greydark = _graphics?.CreateSolidBrush(64, 64, 64);
            _greydarker = _graphics?.CreateSolidBrush(24, 24, 24, 100);
            _darkred = _graphics?.CreateSolidBrush(153, 0, 0, 100);
            _darkgreen = _graphics?.CreateSolidBrush(0, 102, 0, 100);
            _darkyellow = _graphics?.CreateSolidBrush(218, 165, 32, 100);
            _red = _graphics?.CreateSolidBrush(255, 0, 0);
            _lightred = _graphics?.CreateSolidBrush(255, 183, 183);
            _lightyellow = _graphics?.CreateSolidBrush(255, 255, 0);
            _lightgreen = _graphics?.CreateSolidBrush(0, 255, 0);
            _lawngreen = _graphics?.CreateSolidBrush(124, 252, 0);
            _goldenrod = _graphics?.CreateSolidBrush(218, 165, 32);
            HPBarColor = _grey;
            TextColor = _white;

            _lightpurple = _graphics?.CreateSolidBrush(222, 182, 255);
            _darkpurple = _graphics?.CreateSolidBrush(73, 58, 85, 100);

            _lightpink = _graphics?.CreateSolidBrush(253, 164, 175);
            _darkpink = _graphics?.CreateSolidBrush(190, 24, 93);

            if (!config.NoInventory)
            {
                INV_SLOT_WIDTH = 112;
                INV_SLOT_HEIGHT = 112;

                _invItemSheet1 = ImageLoader.LoadBitmap(_device, Properties.Resources.ui0100_iam_texout);
                _invItemSheet2 = ImageLoader.LoadBitmap(_device, Properties.Resources.ui0100_wp_iam_texout);
                GenerateClipping();
            }

            return 0;
        }

        public override int Shutdown()
        {
            SaveConfiguration(config);

            weaponToImageTranslation = null;
            itemToImageTranslation = null;

            _invItemSheet2?.Dispose();
            _invItemSheet1?.Dispose();

            _black?.Dispose();
            _white?.Dispose();
            _grey?.Dispose();
            _greydark?.Dispose();
            _greydarker?.Dispose();
            _darkred?.Dispose();
            _darkgreen?.Dispose();
            _darkyellow?.Dispose();
            _red?.Dispose();
            _lightred?.Dispose();
            _lightyellow?.Dispose();
            _lightgreen?.Dispose();
            _lawngreen?.Dispose();
            _goldenrod?.Dispose();
            HPBarColor.Dispose();
            TextColor.Dispose();

            _lightpurple?.Dispose();
            _darkpurple?.Dispose();
            _lightpink?.Dispose();
            _darkpink?.Dispose();

            _consolasBold?.Dispose();

            _device = null; // We didn't create this object so we probably shouldn't be the one to dispose of it. Just set the variable to null so the reference isn't held.
            _graphics?.Dispose(); // This should technically be the one to dispose of the _device object since it was pulled from this instance.
            _graphics = null;
            _window?.Dispose();
            _window = null;

            gameProcess?.Dispose();
            gameProcess = null;

            return 0;
        }

        public int ReceiveData(object gameMemory)
        {
            this.gameMemory = (IGameMemoryRE3)gameMemory;
            _window?.PlaceAbove(gameWindowHandle);
            _window?.FitTo(gameWindowHandle, true);

            try
            {
                _graphics?.BeginScene();
                _graphics?.ClearScene();

                if (config.ScalingFactor != 1f)
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(config.ScalingFactor, 0f, 0f, config.ScalingFactor, 0f, 0f);
                else
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);

                DrawOverlay();
            }
            catch (Exception ex)
            {
                hostDelegates.ExceptionMessage.Invoke(ex);
            }
            finally
            {
                _graphics?.EndScene();
            }

            return 0;
        }

        private void SetColors()
        {
            if (gameMemory.PlayerManager.HasParasite) // Poisoned
            {
                HPBarColor = _darkpurple;
                TextColor = _lightpurple;
                return;
            }
            if (gameMemory.PlayerManager.IsPoisoned) // Poisoned
            {
                HPBarColor = _darkpurple;
                TextColor = _lightpurple;
                return;
            }
            else if (gameMemory.PlayerManager.HealthState == PlayerState.Fine) // Fine
            {
                HPBarColor = _darkgreen;
                TextColor = _lightgreen;
                return;
            }
            else if (gameMemory.PlayerManager.HealthState == PlayerState.Caution) // Caution (Yellow)
            {
                HPBarColor = _darkyellow;
                TextColor = _lightyellow;
                return;
            }
            else if (gameMemory.PlayerManager.HealthState == PlayerState.Danger) // Danger (Red)
            {
                HPBarColor = _darkred;
                TextColor = _lightred;
                return;
            }
            else
            {
                HPBarColor = _greydarker;
                TextColor = _white;
                return;
            }
        }

        private void DrawOverlay()
        {
            float baseXOffset = config.PositionX;
            float baseYOffset = config.PositionY;

            // Player HP
            float statsXOffset = baseXOffset + 5f;
            float statsYOffset = baseYOffset + 0f;

            float textOffsetX = 0f;
            _graphics?.DrawText(_consolasBold, 20f, _white, statsXOffset + 10, statsYOffset += 24, "IGT: ");
            textOffsetX = statsXOffset + 10f + GetStringSize("IGT: ") + 10f;
            _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.Timer.IGTFormattedString); //110f

            PlayerName = string.Format("{0}: ", gameMemory.PlayerManager.CurrentSurvivorString);
            SetColors();

            if (config.ShowHPBars)
            {
                if (gameMemory.PlayerManager.IsLoaded)
                    DrawHealthBar(ref statsXOffset, ref statsYOffset, PlayerName, gameMemory.PlayerManager.Health.CurrentHP, gameMemory.PlayerManager.Health.MaxHP, gameMemory.PlayerManager.Health.Percentage);
            }
            else
            {
                string perc = float.IsNaN(gameMemory.PlayerManager.Health.Percentage) ? "0%" : string.Format("{0:P1}", gameMemory.PlayerManager.Health.Percentage);
                if (gameMemory.PlayerManager.IsLoaded)
                    _graphics?.DrawText(_consolasBold, 20f, TextColor, statsXOffset + 10f, statsYOffset += 24, string.Format("{0}{1} / {2} {3:P1}", PlayerName, gameMemory.PlayerManager.Health.CurrentHP, gameMemory.PlayerManager.Health.MaxHP, perc));
            }

            textOffsetX = 0f;
            if (config.Debug)
            {
                _graphics?.DrawText(_consolasBold, 20f, _grey, statsXOffset, statsYOffset += 24, "Raw IGT");
                _graphics?.DrawText(_consolasBold, 20f, _grey, statsXOffset, statsYOffset += 24, string.Format("A:{0}", gameMemory.Timer.GameSaveData.GameElapsedTime.ToString("00000000000000000000")));
                _graphics?.DrawText(_consolasBold, 20f, _grey, statsXOffset, statsYOffset += 24, string.Format("C:{0}", gameMemory.Timer.GameSaveData.DemoSpendingTime.ToString("00000000000000000000")));
                _graphics?.DrawText(_consolasBold, 20f, _grey, statsXOffset, statsYOffset += 24, string.Format("M:{0}", gameMemory.Timer.GameSaveData.InventorySpendingTime.ToString("00000000000000000000")));
                _graphics?.DrawText(_consolasBold, 20f, _grey, statsXOffset, statsYOffset += 24, string.Format("P:{0}", gameMemory.Timer.GameSaveData.PauseSpendingTime.ToString("00000000000000000000")));
            }

            if (config.ShowDifficultyAdjustment)
            {
                _graphics?.DrawText(_consolasBold, 20f, _grey, config.PositionX + 15f, statsYOffset += 24, config.ScoreString);
                textOffsetX = config.PositionX + 15f + GetStringSize(config.ScoreString) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, Math.Floor(gameMemory.RankManager.RankPoint).ToString()); //110f
                textOffsetX += GetStringSize(gameMemory.RankManager.RankPoint.ToString()) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _grey, textOffsetX, statsYOffset, config.RankString); //178f
                textOffsetX += GetStringSize(config.RankString) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.RankManager.GameRank.ToString()); //261f
                textOffsetX += GetStringSize(gameMemory.RankManager.GameRank.ToString()) + 10f;
            }

            if (config.ShowMapLocations)
            {
                var locId = "Loc ID:";
                var locName = "Loc Name:";
                _graphics?.DrawText(_consolasBold, 20f, _grey, config.PositionX + 15f, statsYOffset += 24, locId);
                textOffsetX = config.PositionX + 15f + GetStringSize(locId) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.LocationID.ToString());
                textOffsetX += GetStringSize(gameMemory.LocationID.ToString()) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _grey, textOffsetX, statsYOffset, locName);
                textOffsetX += GetStringSize(locName) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.LocationName);

                var mapId = "Map ID:";
                var mapName = "Map Name:";
                _graphics?.DrawText(_consolasBold, 20f, _grey, config.PositionX + 15f, statsYOffset += 24, mapId);
                textOffsetX = config.PositionX + 15f + GetStringSize(mapId) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.MapID.ToString());
                textOffsetX += GetStringSize(gameMemory.MapID.ToString()) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _grey, textOffsetX, statsYOffset, mapName);
                textOffsetX += GetStringSize(mapName) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.MapName);
            }

            // Enemy HP
            var xOffset = config.EnemyHPPositionX == -1 ? statsXOffset : config.EnemyHPPositionX;
            var yOffset = config.EnemyHPPositionY == -1 ? statsYOffset : config.EnemyHPPositionY;
            foreach (Enemy enemyHP in gameMemory.Enemies.Where(a => a.IsAlive).OrderBy(a => a.MaxHP).ThenBy(a => a.Percentage).ThenByDescending(a => a.CurrentHP))
                if (config.ShowHPBars)
                {
                    DrawProgressBar(ref xOffset, ref yOffset, (int)enemyHP.EnemyID, enemyHP.CurrentHP, enemyHP.MaxHP, enemyHP.Percentage);
                }
                else
                {
                    _graphics.DrawText(_consolasBold, 20f, _white, xOffset + 10f, yOffset += 28f, string.Format("{0}: {1} / {2} {3:P1}", GetEnemyName((int)enemyHP.EnemyID), enemyHP.CurrentHP, enemyHP.MaxHP, enemyHP.Percentage));
                }

            // Inventory
            if (!config.NoInventory)
            {
                if (gameMemory.Timer.MeasurePauseSpendingTime || gameMemory.Timer.MeasureInventorySpendingTime) return;
                float invXOffset = config.InventoryPositionX == -1 ? statsXOffset : config.InventoryPositionX;
                float invYOffset = config.InventoryPositionY == -1 ? yOffset + 24f : config.InventoryPositionY; // Using yOffset instead of statsYOffset to offset everything relative to the other stats Y position.
                if (itemToImageTranslation != null && weaponToImageTranslation != null)
                {
                    for (int i = 0; i < gameMemory.Items.Length; ++i)
                    {
                        // Only do logic for non-blank and non-broken items.
                        if (gameMemory.Items[i].SlotNo >= 0 && gameMemory.Items[i].SlotNo <= 19 && !gameMemory.Items[i].IsEmptySlot)
                        {
                            int slotColumn = gameMemory.Items[i].SlotNo % 4;
                            int slotRow = gameMemory.Items[i].SlotNo / 4;
                            float imageX = invXOffset + (slotColumn * INV_SLOT_WIDTH);
                            float imageY = invYOffset + (slotRow * INV_SLOT_HEIGHT);
                            //float textX = imageX + (INV_SLOT_WIDTH * options.ScalingFactor);
                            //float textY = imageY + (INV_SLOT_HEIGHT * options.ScalingFactor);
                            float textX = imageX + (INV_SLOT_WIDTH * 0.96f);
                            float textY = imageY + (INV_SLOT_HEIGHT * 0.68f);
                            SolidBrush textBrush = _white;
                            if (gameMemory.Items[i].Count == 0)
                                textBrush = _darkred;

                            Weapon weapon = new Weapon();
                            if (gameMemory.Items[i].IsWeapon)
                            {
                                weapon.WeaponID = gameMemory.Items[i].WeaponId;
                                weapon.Attachments = gameMemory.Items[i].WeaponParts;
                            }

                            // Get the region of the inventory sheet where this item's icon resides.
                            SharpDX.Mathematics.Interop.RawRectangleF imageRegion;
                            if (gameMemory.Items[i].IsItem && itemToImageTranslation.ContainsKey(gameMemory.Items[i].ItemId))
                                imageRegion = itemToImageTranslation[gameMemory.Items[i].ItemId];

                            //FAILING TO RETURN IT CONTAINS KEY?
                            else if (gameMemory.Items[i].IsWeapon && weaponToImageTranslation.ContainsKey(weapon))
                                imageRegion = weaponToImageTranslation[weapon];
                            else 
                                imageRegion = new SharpDX.Mathematics.Interop.RawRectangleF(0, 0, INV_SLOT_WIDTH, INV_SLOT_HEIGHT);

                            imageRegion.Right += imageRegion.Left;
                            imageRegion.Bottom += imageRegion.Top;

                            // Get the region to draw our item icon to.
                            SharpDX.Mathematics.Interop.RawRectangleF drawRegion;
                            if (imageRegion.Right - imageRegion.Left == INV_SLOT_WIDTH * 2f)
                            {
                                // Double-slot item, adjust the draw region width and text's X coordinate.
                                textX += INV_SLOT_WIDTH;
                                drawRegion = new SharpDX.Mathematics.Interop.RawRectangleF(imageX, imageY, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT);
                            }
                            else // Normal-sized icon.
                                drawRegion = new SharpDX.Mathematics.Interop.RawRectangleF(imageX, imageY, INV_SLOT_WIDTH, INV_SLOT_HEIGHT);
                            drawRegion.Right += drawRegion.Left;
                            drawRegion.Bottom += drawRegion.Top;

                            if (gameMemory.Items[i].IsItem)
                                _device?.DrawBitmap(_invItemSheet1, drawRegion, 1f, SharpDX.Direct2D1.BitmapInterpolationMode.Linear, imageRegion);
                            else if (gameMemory.Items[i].IsWeapon)
                                _device?.DrawBitmap(_invItemSheet2, drawRegion, 1f, SharpDX.Direct2D1.BitmapInterpolationMode.Linear, imageRegion);
                            

                            // Draw the quantity text.
                            _graphics?.DrawText(_consolasBold, 22f, textBrush, textX - GetStringSize(gameMemory.Items[i].Count.ToString(), 22f), textY, (gameMemory.Items[i].Count != -1) ? gameMemory.Items[i].Count.ToString() : "∞");
                        }
                    }
                }
            }
        }

        private string GetEnemyName(int type)
        {
            if (type == 0) return "Zombie (M)";
            if (type == 1) return "Zombie (F)";
            if (type == 2) return "Fat Zombie";
            if (type == 3) return "Licker";
            if (type == 4) return "Zombie Dog";
            if (type == 6) return "G-Young";
            if (type == 7) return "Ivy";
            if (type == 8) return "G-Adult";
            if (type == 10) return "Mr. X";
            if (type == 11) return "Tyrant";
            if (type == 12) return "G";
            if (type == 13) return "G2";
            if (type == 15) return "G3";
            if (type == 16) return "G4";
            if (type == 17) return "G5";
            if (type == 18) return "Chief Irons";
            if (type == 23) return "Pale Head";
            if (type == 24) return "Psn Zombie";
            return "??";
        }

        private float GetStringSize(string str, float size = 20f)
        {
            return (float)_graphics?.MeasureString(_consolasBold, size, str).X;
        }

        private bool IsBoss(int id)
        {
            int[] bosses = new int[] { 10, 11, 12, 13, 15, 16 };
            return bosses.Contains(id);
        }

        private void DrawProgressBar(ref float xOffset, ref float yOffset, int id, float chealth, float mhealth, float percentage = 1f)
        {
            if (config.ShowDamagedEnemiesOnly && percentage == 1f) { return; }
            if (config.ShowBossOnly && !IsBoss(id)) { return; }
            if (id == 18) { return; }
            string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
            float endOfBar = config.PositionX + 342f - GetStringSize(perc, 16f);
            _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 342f, yOffset + 22f, 4f);
            _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 340f, yOffset + 20f);
            _graphics.FillRectangle(_darkred, xOffset + 1f, yOffset + 1f, xOffset + (340f * percentage), yOffset + 20f);
            _graphics.DrawText(_consolasBold, 16f, _lightred, xOffset + 10f, yOffset, string.Format("{0}: {1} / {2}", GetEnemyName(id), chealth, mhealth));
            _graphics.DrawText(_consolasBold, 16f, _lightred, endOfBar, yOffset, perc);
        }

        private void DrawHealthBar(ref float xOffset, ref float yOffset, string name, float chealth, float mhealth, float percentage = 1f)
        {
            string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
            float endOfBar = config.PositionX + 342f - GetStringSize(perc, 16f);
            _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 342f, yOffset + 22f, 4f);
            _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 340f, yOffset + 20f);
            _graphics.FillRectangle(HPBarColor, xOffset + 1f, yOffset + 1f, xOffset + (340f * percentage), yOffset + 20f);
            _graphics.DrawText(_consolasBold, 16f, TextColor, xOffset + 10f, yOffset, string.Format("{0}{1} / {2}", name, chealth, mhealth));
            _graphics.DrawText(_consolasBold, 16f, TextColor, endOfBar, yOffset, perc);
        }

        public void GenerateClipping()
        {
            int itemColumnInc = -1;
            int itemRowInc = -1;
            itemToImageTranslation = new Dictionary<ItemID, SharpDX.Mathematics.Interop.RawRectangleF>()
            {
                { ItemID.None, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * 0, INV_SLOT_HEIGHT * 8, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },

                // Row 0.
                { ItemID.First_Aid_Spray, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Green_Herb, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Red_Herb, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Mixed_Herb_GG, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Mixed_Herb_GR, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Mixed_Herb_GGG, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Green_Herb2, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Red_Herb2, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },

                // Row 1.
                { ItemID.Handgun_Ammo, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Shotgun_Shells, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Assault_Rifle_Ammo, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.MAG_Ammo, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Acid_Rounds, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Flame_Rounds, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Explosive_Rounds, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Mine_Rounds, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Gunpowder, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.HighGrade_Gunpowder, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Explosive_A, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Explosive_B, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },

                // Row 2.
                { ItemID.Moderator_Handgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Dot_Sight_Handgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Extended_Magazine_Handgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.SemiAuto_Barrel_Shotgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Tactical_Stock_Shotgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Shell_Holder_Shotgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Scope_Assault_Rifle, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Dual_Magazine_Assault_Rifle, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Tactical_Grip_Assault_Rifle, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Extended_Barrel_MAG, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Supply_Crate_Acid_Rounds, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Supply_Crate_Extended_Barrel_MAG, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Supply_Crate_Extended_Magazine_Handgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Supply_Crate_Flame_Rounds, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Supply_Crate_Moderator_Handgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Supply_Crate_Shotgun_Shells, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },

                //Row 3.
                { ItemID.Battery, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Safety_Deposit_Key, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Detonator_No_Battery, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Brads_ID_Card, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Detonator, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Detonator2, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Lock_Pick, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 8), INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Bolt_Cutters, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },

                // Row 4.
                { ItemID.Fire_Hose, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Fire_Hose2, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Kendos_Gate_Key, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Battery_Pack, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { ItemID.Case_Lock_Pick, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 4), INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Green_Jewel, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Blue_Jewel, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Red_Jewel, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Fancy_Box_Green_Jewel, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Fancy_Box_Blue_Jewel, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Fancy_Box_Red_Jewel, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },

                // Row 5.
                { ItemID.Hospital_ID_Card, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Audiocassette_Tape, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Vaccine_Sample, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Fuse1, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Fuse2, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Fuse3, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Audiocassette_Tape2, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Tape_Player, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Tape_Player_Tape_Inserted, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Locker_Room_Key, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },

                // Row 6.
                { ItemID.Override_Key, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Vaccine, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Culture_Sample, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Liquidfilled_Test_Tube, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Vaccine_Base, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },

                // Row 7.
                { ItemID.Hip_Pouch, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 1), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Iron_Defense_Coin, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 5), INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Assault_Coin, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Recovery_Coin, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.Crafting_Companion, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { ItemID.STARS_Field_Combat_Manual, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },

            };

            int weaponColumnInc = -1;
            int weaponRowInc = -1;
            weaponToImageTranslation = new Dictionary<Weapon, SharpDX.Mathematics.Interop.RawRectangleF>()
            {
                { new Weapon() { WeaponID = WeaponType.None, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * 0, INV_SLOT_HEIGHT * 5, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },

                // Row 10.
                { new Weapon() { WeaponID = WeaponType.G19_Handgun, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 0), INV_SLOT_HEIGHT * ++weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.G19_Handgun, Attachments = WeaponParts.First }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.G19_Handgun, Attachments = WeaponParts.First | WeaponParts.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 3), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.G19_Handgun, Attachments = WeaponParts.First | WeaponParts.Second | WeaponParts.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 5), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.G19_Handgun, Attachments = WeaponParts.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 7), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.G19_Handgun, Attachments = WeaponParts.Second | WeaponParts.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.G19_Handgun, Attachments = WeaponParts.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.G19_Handgun, Attachments = WeaponParts.First | WeaponParts.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 10), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Samurai_Edge, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 12), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.G18_Handgun, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 16), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.G18_Burst_Handgun, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },

                // Row 11.
                { new Weapon() { WeaponID = WeaponType.Shotgun, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 0), INV_SLOT_HEIGHT * ++weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Shotgun, Attachments = WeaponParts.First }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Shotgun, Attachments = WeaponParts.First | WeaponParts.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 3), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Shotgun, Attachments = WeaponParts.First | WeaponParts.Second | WeaponParts.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 5), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Shotgun, Attachments = WeaponParts.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 7), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Shotgun, Attachments = WeaponParts.Second | WeaponParts.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Shotgun, Attachments = WeaponParts.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Shotgun, Attachments = WeaponParts.First | WeaponParts.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 10), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Lightning_Hawk, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 12), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Lightning_Hawk, Attachments = WeaponParts.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },

                // Row 12.
                { new Weapon() { WeaponID = WeaponType.CQBR_Assault_Rifle, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 0), INV_SLOT_HEIGHT * ++weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.CQBR_Assault_Rifle, Attachments = WeaponParts.First }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 2), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.CQBR_Assault_Rifle, Attachments = WeaponParts.First | WeaponParts.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 4), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.CQBR_Assault_Rifle, Attachments = WeaponParts.First | WeaponParts.Second | WeaponParts.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 6), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.CQBR_Assault_Rifle, Attachments = WeaponParts.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 8), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.CQBR_Assault_Rifle, Attachments = WeaponParts.Second | WeaponParts.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 10), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.CQBR_Assault_Rifle, Attachments = WeaponParts.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 12), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.CQBR_Assault_Rifle, Attachments = WeaponParts.First | WeaponParts.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 14), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },


                // Row 13.
                { new Weapon() { WeaponID = WeaponType.Infinite_Rocket_Launcher, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 6), INV_SLOT_HEIGHT * ++weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Infinite_CQBR_Assault_Rifle, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 8), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },

                // Row 14.
                { new Weapon() { WeaponID = WeaponType.Combat_Knife_Carlos, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 0), INV_SLOT_HEIGHT * ++weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Survival_Knife_Jill, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Infinite_MUP_Handgun, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 4), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.RAIDEN, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.HOT_DOGGER, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 7), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Hand_Grenade, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 9), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Flash_Grenade, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
                { new Weapon() { WeaponID = WeaponType.Grenade_Launcher, Attachments = WeaponParts.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },

            };
        }
    }
}
