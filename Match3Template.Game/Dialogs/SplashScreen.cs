namespace Match3Template.Dialogs
{
	[ScenePath("Shell/Splash")]
	public class SplashScreen : Dialog
	{
		public SplashScreen()
		{
			Root.RunAnimation("Start", "Appear");
			Root.Animations.Find("Appear").Stopped += () => CrossFadeInto("Shell/MainMenu");
		}
	}
}
