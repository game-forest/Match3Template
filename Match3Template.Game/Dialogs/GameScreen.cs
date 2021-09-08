using Lime;
using Match3Template.Types;

namespace Match3Template.Dialogs
{
	[ScenePath("Shell/GameScreen")]
	public class GameScreen : Dialog
	{
		private Board board;
		public GameScreen()
		{
			SoundManager.PlayMusic("Ingame");
			var exitButton = Root["BtnExit"];
			exitButton.Clicked = ReturnToMenu;
			board = Board.CreateBoard(Root);
		}

		protected override void Update(float delta)
		{
			if (Input.WasKeyPressed(Key.Escape)) {
				ReturnToMenu();
			}
		}

		protected override bool HandleAndroidBackButton()
		{
			ReturnToMenu();
			return true;
		}

		private void ReturnToMenu()
		{
			var confirmation = new Confirmation("Are you sure?");
			confirmation.OkClicked += () => CrossFadeInto("Shell/MainMenu");
			DialogManager.Open(confirmation);
		}
	}
}
