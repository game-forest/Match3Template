using System.Collections.Generic;
using System.Linq;
using Match3Template.Application;
using Lime;
using System;

namespace Match3Template.Debug
{
	public class Cheats : ICheatManager
	{
		private readonly List<string> debugInfoStrings = new List<string>();
		public bool Enabled { get; set; }
		public bool IsDebugInfoVisible { get; set; }

		private static WindowInput Input => The.Window.Input;

		public bool DebugMatch3 { get; set; } = true;
		public static Cheats Instance { get; internal set; } = new Cheats();

		private static RainbowDash.Menu currentMenu;

		public Cheats()
		{
			if (ICheatManager.Instance != null) {
				throw new InvalidOperationException();
			}
			ICheatManager.Instance = this;
			Enabled = true;
			IsDebugInfoVisible = false;
			DebugMatch3 = false;
		}

		public void ProcessCheatKeys()
		{
			if (Enabled) {
				if (Input.WasKeyPressed(Key.F1) || Input.WasTouchBegan(3)) {
					ShowMenu();
				}
#if WIN
				if (Input.WasKeyPressed(Key.F10)) {
					TexturePool.Instance.DiscardAllTextures();
				}
				if (Input.WasKeyPressed(Key.F11)) {
					DisplayInfo.SetNextDisplay();
				}
				if (Input.WasKeyPressed(Key.F12)) {
					DisplayInfo.SetNextDeviceOrientation();
				}
#endif
			}
		}

		public bool WasKeyPressed(Key key)
		{
			return Enabled && Input.WasKeyPressed(key);
		}

		public bool IsKeyPressed(Key key)
		{
			return Enabled && Input.IsKeyPressed(key);
		}

		public bool WasTripleTouch()
		{
			var isTouchJustStarted = Input.WasTouchBegan(0) || Input.WasTouchBegan(1) || Input.WasTouchBegan(2);
			return Enabled && IsTripleTouch() && isTouchJustStarted;
		}

		public bool IsTripleTouch()
		{
			return Enabled && Input.IsTouching(0) && Input.IsTouching(1) && Input.IsTouching(2);
		}

		public RainbowDash.Menu ShowMenu()
		{
			if (currentMenu != null) {
				return currentMenu;
			}

			var menu = new RainbowDash.Menu(The.World, Layers.CheatsMenu);
			menu.Root.Components.Add(new OverdrawForegroundComponent());
			var section = menu.Section();

			InitialFill(menu);
			The.DialogManager.Top.FillDebugMenuItems(menu);

			menu.Show();
			currentMenu = menu;
			menu.Hidden += () => {
				currentMenu = null;
			};
			return menu;
		}

		private void InitialFill(RainbowDash.Menu menu)
		{
			var debugSection = menu.Section("Debug");

			debugSection.Item("Toggle Debug Info", () =>
				IsDebugInfoVisible = !IsDebugInfoVisible
			);

			debugSection.Item("Debug Match3", () => DebugMatch3 = !DebugMatch3);

			debugSection.Item("Enable Splash Screen", () => {
				The.AppData.EnableSplashScreen = true;
				The.AppData.Save();
			}, () => !The.AppData.EnableSplashScreen);

			debugSection.Item("Disable Splash Screen", () => {
				The.AppData.EnableSplashScreen = false;
				The.AppData.Save();
			}, () => The.AppData.EnableSplashScreen);
#if PROFILER
			debugSection.Item("Toggle overdraw visualization", () => {
				Lime.Profiler.Graphics.Overdraw.Enabled = !Lime.Profiler.Graphics.Overdraw.Enabled;
			});
			debugSection.Item("Toggle overdraw metric", () => {
				var value = Lime.Profiler.Graphics.Overdraw.MetricRequired = !Lime.Profiler.Graphics.Overdraw.MetricRequired;
				if (value) {
					Lime.Profiler.Graphics.Overdraw.MetricCreated += Overdraw_MetricCreated;
				} else {
					Lime.Profiler.Graphics.Overdraw.MetricCreated -= Overdraw_MetricCreated;
				}
			});
#endif // PROFILER
			RemoteScripting.FillDebugMenuItems(menu);
		}

#if PROFILER
		private static void Overdraw_MetricCreated(float averageOverdraw, int pixelCount)
		{
			overdrawPixelCount = pixelCount;
			overdrawAverageOverdraw = averageOverdraw;
		}

		private static int overdrawPixelCount = 0;
		private static float overdrawAverageOverdraw = 0;
#endif // PROFILER

		public void AddDebugInfo(string info)
		{
			debugInfoStrings.Add(info);
		}

		public void RenderDebugInfo(int drawCallCount)
		{
			if (!IsDebugInfoVisible) {
				return;
			}

			Renderer.Transform1 = Matrix32.Identity;
			Renderer.Blending = Blending.Alpha;
			Renderer.Shader = ShaderId.Diffuse;
			IFont font = FontPool.Instance[null];
			float height = 25.0f * The.World.Scale.X;

			float x = 5;
			float y = 0;

			var fields = new List<string> {
				$"Fps: {The.Window.FPS}",
				$"Window size: {The.Window.ClientSize}",
				$"World size: {The.World.Size}",
				$"Draw calls: {drawCallCount}",
			};
#if PROFILER
			if (Lime.Profiler.Graphics.Overdraw.MetricRequired) {
				fields.Add($"Overdraw pixel count: {overdrawPixelCount}");
				fields.Add($"Overdraw average: {overdrawAverageOverdraw}");
			}
#endif // PROFILER

			fields.Add("Active dialogs:");
			foreach (var dialog in Dialogs.DialogManager.Instance.ActiveDialogs) {
				fields.Add($"\t - {dialog.Root}");
			}

			var text = string.Join("\n", fields.Concat(debugInfoStrings));

			Renderer.DrawTextLine(font, new Vector2(x + 1, y + 1), text, height, new Color4(0, 0, 0), 0); // shadow
			Renderer.DrawTextLine(font, new Vector2(x, y), text, height, new Color4(255, 255, 255), 0);

			debugInfoStrings.Clear();
		}
	}
}
