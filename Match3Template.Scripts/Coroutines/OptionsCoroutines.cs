using Match3Template.Dialogs;
using Match3Template.Scripts.Common;

namespace Match3Template.Scripts.Coroutines
{
	public class OptionsCoroutines
	{
		public static async Coroutine Close(Options dialog = null, WaitDialogTaskParameters parameters = null) =>
			await CommonCoroutines.CloseDialog(dialog, parameters, "@BtnOk");
	}
}
