using Lime;

namespace Match3Template.Dialogs
{
	[ScenePath("Shell/MainMenu")]
	public class MainMenu : Dialog
	{
		public MainMenu()
		{
			SoundManager.PlayMusic("Theme");
		}

		protected override bool HandleAndroidBackButton()
		{
			return false;
		}

		protected override void Update(float delta)
		{
			if (Input.WasKeyPressed(Key.Escape)) {
				Lime.Application.Exit();
			}
		}
	}
}
