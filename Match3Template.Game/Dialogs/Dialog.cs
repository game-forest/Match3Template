using System;
using System.Collections.Generic;
using Match3Template.Application;
using Lime;
using System.IO;

namespace Match3Template.Dialogs
{
	public class Dialog : IDialog, IDisposable
	{
		public Widget Root { get; private set; }
		public DialogState State { get; protected set; }

		public string Path { get; private set; }

		public Dialog(string path)
		{
			Path = path;
			Root = Node.Load<Widget>(Path);
			Root.Updating += Update;
			ApplyLocalization();
		}

		public Dialog()
		{
			var a = (ScenePathAttribute)this.GetType().GetCustomAttributes(typeof(ScenePathAttribute), false)[0];
			Path = a.ScenePath;
			Root = Node.Load<Widget>(Path);
			Root.Updating += Update;
			ApplyLocalization();
		}

		public void Attach(Widget widget)
		{
			Root.Layer = Layer;
			Root.PushToNode(widget);
			Root.ExpandToContainer();
			Lime.Application.InvokeOnMainThread(Root.Input.RestrictScope);

			DisplayInfo.BeforeOrientationOrResolutionChanged += OnBeforeOrientationOrResolutionChanged;
			DisplayInfo.OrientationOrResolutionChanged += OnOrientationOrResolutionChanged;

			Show(ShowAnimationName.Marker, ShowAnimationName.Animation);
			Root.Update(0);
		}

		protected virtual int Layer => Layers.Interface;

		protected virtual (string Marker, string Animation) ShowAnimationName => ("Show", "Appear");

		protected virtual (string Marker, string Animation) HideAnimationName => ("Hide", "Appear");

		protected virtual void Update(float delta) { }

		private void Show(string marker, string animation)
		{
			Root.Tasks.Add(ShowTask(marker, animation));
		}

		private IEnumerator<object> ShowTask(string marker, string animation)
		{
			State = DialogState.Showing;
			Orientate();
			if (animation != null && Root.TryRunAnimation(marker, animation)) {
				while (Root.Animations.Find(animation).IsRunning) {
					yield return null;
				}
			}
			State = DialogState.Shown;
		}

		protected virtual void OnBeforeOrientationOrResolutionChanged()
		{
			Root.ExpandToContainer();
			Orientate();
		}

		protected virtual void OnOrientationOrResolutionChanged() { }

		protected virtual void Orientate()
		{
			var markerName = DisplayInfo.IsLandscapeOrientation() ? "Landscape" : "Portrait";
			if (DisplayInfo.IsTabletDisplay()) {
				markerName = "Tablet" + markerName;
			}
			foreach (var node in Root.Descendants) {
				node.TryRunAnimation(markerName, "Resolutions");
			}
		}

		public void CrossFadeInto(string scenePath)
		{
			The.World.Tasks.Add(CrossfadeIntoTask(scenePath, true, true));
		}

		private IEnumerator<object> CrossfadeIntoTask(string scenePath, bool fadeIn, bool fadeOut)
		{
			var crossfade = new ScreenCrossfade();
			crossfade.Attach();
			crossfade.CaptureInput();
			if (fadeIn)
				yield return crossfade.FadeInTask();
			CloseImmediately();
			DialogManager.Open(scenePath);
			crossfade.ReleaseInput();
			if (fadeOut) {
				yield return crossfade.FadeOutTask();
			}
			crossfade.Detach();
			crossfade.Dispose();
		}

		private void ApplyLocalization()
		{
			var animationName = string.IsNullOrEmpty(Lime.Application.CurrentLanguage) ? "EN" : (Lime.Application.CurrentLanguage);
			foreach (var node in Root.Descendants) {
				if (!node.TryRunAnimation(animationName)) {
					node.TryRunAnimation("other");
				}
			}
		}

		private void UnlinkAndDispose()
		{
			Root.UnlinkAndDispose();
			DisplayInfo.BeforeOrientationOrResolutionChanged -= OnBeforeOrientationOrResolutionChanged;
			DisplayInfo.OrientationOrResolutionChanged -= OnOrientationOrResolutionChanged;
		}

		public void Close()
		{
			BeginClose();
			Root.Tasks.Add(CloseTask(HideAnimationName.Marker, HideAnimationName.Animation));
		}

		public void CloseImmediately()
		{
			BeginClose();
			UnlinkAndDispose();
			State = DialogState.Closed;
		}

		private void BeginClose()
		{
			State = DialogState.Closing;
			Closing();
			DialogManager.Remove(this);
		}

		protected virtual void Closing() { }

		private IEnumerator<object> CloseTask(string marker, string animation)
		{
			State = DialogState.Closing;
			if (animation != null && Root.TryRunAnimation(marker, animation)) {
				while (Root.Animations.Find(animation).IsRunning) {
					yield return null;
				}
			}
			UnlinkAndDispose();
			State = DialogState.Closed;
		}

		public IEnumerator<object> WaitForDisappear()
		{
			yield return Task.WaitWhile(() => Root.Parent != null);
		}

		protected virtual bool HandleAndroidBackButton()
		{
			if (!IsTopDialog) {
				return false;
			}

			Close();
			return true;
		}

		public void Dispose()
		{
			CloseImmediately();
		}

		public bool IsClosed => State == DialogState.Closed;

		public bool IsTopDialog => DialogManager.Top == this;

		public virtual void FillDebugMenuItems(RainbowDash.Menu menu) { }

		protected Application.Application App => The.App;
		protected WindowWidget World => The.World;
		protected IWindow Window => The.Window;
		protected WindowInput Input => Window.Input;
		protected SoundManager SoundManager => The.SoundManager;
		protected AppData AppData => The.AppData;
		protected Profile Profile => The.Profile;
		protected DialogManager DialogManager => The.DialogManager;
		protected Logger Log => The.Log;
	}

	public enum DialogState
	{
		Showing,
		Shown,
		Closing,
		Closed
	}
}
