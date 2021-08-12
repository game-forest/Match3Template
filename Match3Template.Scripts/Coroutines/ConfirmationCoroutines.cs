using Match3Template.Dialogs;
using Match3Template.Scripts.Common;

namespace Match3Template.Scripts.Coroutines
{
	public class ConfirmationCoroutines
	{
		public static async Coroutine Confirm(Confirmation dialog = null, WaitDialogTaskParameters parameters = null) =>
			await CommonCoroutines.CloseDialog(dialog, parameters, "@BtnOk");

		public static async Coroutine Decline(Confirmation dialog = null, WaitDialogTaskParameters parameters = null) =>
			await CommonCoroutines.CloseDialog(dialog, parameters, "@BtnCancel");
	}
}
