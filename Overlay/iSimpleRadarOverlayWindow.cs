using GameOverlay.Drawing;
using GameOverlay.Windows;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using Color = GameOverlay.Drawing.Color;
using Font = GameOverlay.Drawing.Font;
using SolidBrush = GameOverlay.Drawing.SolidBrush;
using Timer = System.Timers.Timer;

namespace iSimpleRadar.Overlay
{
    public class iSimpleRadarOverlayWindow
    {
        private readonly GraphicsWindow overlayWindow;
        private readonly Dictionary<string, SolidBrush> _brushes;
        private readonly Dictionary<string, Font> _fonts;

        public float posX = 0, posY = 0;
        public string CarBehindWarn;
        public string CarBehindDanger;

        public bool carLeft;
        public bool carRight;

        public iSimpleRadarOverlayWindow()
        {

            _brushes = new Dictionary<string, SolidBrush>();
            _fonts = new Dictionary<string, Font>();
            var graphics = new GameOverlay.Drawing.Graphics
            {
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true,
                UseMultiThreadedFactories = true,
                VSync = true,
                WindowHandle = IntPtr.Zero
            };
            // it is important to set the window to visible (and topmost) if you want to see it!
            overlayWindow = new GraphicsWindow(graphics)
            {
                IsTopmost = true,
                IsVisible = true,
                //IsAppWindow = UserSettings.GetUserSettings().getBoolean("make_overlay_app_window"),
                FPS = 60,
                X = 0,
                Y = 0,
                Width = Screen.PrimaryScreen.Bounds.Width,
                Height = Screen.PrimaryScreen.Bounds.Height,
                Title = "iSimpleRadar Overlay",
                ClassName = "iSimpleRadar_Overlay"

            };

            overlayWindow.SetupGraphics += overlayWindow_SetupGraphics;
            overlayWindow.DestroyGraphics += overlayWindow_DestroyGraphics;
            overlayWindow.DrawGraphics += overlayWindow_DrawGraphics;



        }

        public void Run()
        {
            if (!overlayWindow.IsInitialized)
                overlayWindow.Create();

        }

        public void Stop()
        {
            // creates the window and setups the graphics
            overlayWindow.Dispose();

        }
        private void overlayWindow_DrawGraphics(object? sender, DrawGraphicsEventArgs e)
        {

            var gfx = e.Graphics;

            gfx.ClearScene();

            //gfx.FillRectangle(backgroundBrush, posX - 100, posY-100, posX +100, posY +100);
            //gfx.DrawBox2D(fontBrush, fontBrush, posX, posY, posX + 10, posY + 10, 1);
            if (!string.IsNullOrEmpty(CarBehindWarn) || !string.IsNullOrEmpty(CarBehindDanger) || carLeft || carRight)
            {
                gfx.DrawCrosshair(_brushes["whiteAlpha"], (Screen.PrimaryScreen.Bounds.Width / 2), (Screen.PrimaryScreen.Bounds.Height / 2) - 150, 50, 3, CrosshairStyle.Cross);
                gfx.DrawCircle(_brushes["whiteAlpha"], (Screen.PrimaryScreen.Bounds.Width / 2), (Screen.PrimaryScreen.Bounds.Height / 2) - 150, 10, 3);

                gfx.DrawCircle(_brushes["whiteAlpha"], (Screen.PrimaryScreen.Bounds.Width / 2), (Screen.PrimaryScreen.Bounds.Height / 2) - 150, 30, 3);
                gfx.DrawCircle(_brushes["whiteAlpha"], (Screen.PrimaryScreen.Bounds.Width / 2), (Screen.PrimaryScreen.Bounds.Height / 2) - 150, 50, 3);



                if (!string.IsNullOrEmpty(CarBehindWarn))
                {
                    gfx.DrawLine(_brushes["orange"], (Screen.PrimaryScreen.Bounds.Width / 2) - 100, (Screen.PrimaryScreen.Bounds.Height / 2) - 150 - (posY / 20 * 50), (Screen.PrimaryScreen.Bounds.Width / 2) + 100, (Screen.PrimaryScreen.Bounds.Height / 2) - 150 - (posY / 20 * 50), 3);

                    gfx.DrawText(_fonts["consolas"], _brushes["orange"], (Screen.PrimaryScreen.Bounds.Width / 2) + 50, (Screen.PrimaryScreen.Bounds.Height / 2) - 150 - (posY / 20 * 50), CarBehindWarn);
                }
                if (!string.IsNullOrEmpty(CarBehindDanger))
                {
                    gfx.DrawLine(_brushes["red"], (Screen.PrimaryScreen.Bounds.Width / 2) - 100, (Screen.PrimaryScreen.Bounds.Height / 2) - 150 - (posY / 20 * 50), (Screen.PrimaryScreen.Bounds.Width / 2) + 100, (Screen.PrimaryScreen.Bounds.Height / 2) - 150 - (posY / 20 * 50), 3);
                    gfx.DrawText(_fonts["consolas"], _brushes["red"], (Screen.PrimaryScreen.Bounds.Width / 2) - 250, (Screen.PrimaryScreen.Bounds.Height / 2) - 150 - (posY / 20 * 50), CarBehindDanger);
                }
                if (carLeft)
                {
                    gfx.DrawTextWithBackground(_fonts["arial"], _brushes["whiteAlpha"], _brushes["redAlpha"], (Screen.PrimaryScreen.Bounds.Width / 2) - 150, (Screen.PrimaryScreen.Bounds.Height / 2) - 165, "Car Left");

                }
                if (carRight)
                {
                    gfx.DrawTextWithBackground(_fonts["arial"], _brushes["whiteAlpha"], _brushes["redAlpha"], (Screen.PrimaryScreen.Bounds.Width / 2) + 75, (Screen.PrimaryScreen.Bounds.Height / 2) - 165, "Car Right");
                }
            }

        }

        private void overlayWindow_DestroyGraphics(object? sender, DestroyGraphicsEventArgs e)
        {
            foreach (var pair in _brushes) pair.Value.Dispose();
            foreach (var pair in _fonts) pair.Value.Dispose();

        }

        private void overlayWindow_SetupGraphics(object? sender, SetupGraphicsEventArgs e)
        {
            var gfx = e.Graphics;
            if (e.RecreateResources)
            {
                foreach (var pair in _brushes) pair.Value.Dispose();

            }
            _brushes["black"] = gfx.CreateSolidBrush(0, 0, 0);
            _brushes["white"] = gfx.CreateSolidBrush(255, 255, 255);
            _brushes["whiteAlpha"] = gfx.CreateSolidBrush(255, 255, 255, 0.75f);
            _brushes["red"] = gfx.CreateSolidBrush(255, 0, 0);
            _brushes["redAlpha"] = gfx.CreateSolidBrush(255, 0, 0, 0.75f);
            _brushes["orange"] = gfx.CreateSolidBrush(255, 140, 0);


            _fonts["arial"] = gfx.CreateFont("Arial", 20);
            _fonts["consolas"] = gfx.CreateFont("Consolas", 20);



        }
    }
}
