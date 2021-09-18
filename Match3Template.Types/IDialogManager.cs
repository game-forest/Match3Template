using Lime;

namespace Match3Template
{
	public interface IDialog
	{
		public Widget Root { get; }

		void Close();
	}

	public interface IDialogManager
	{
		static IDialogManager Instance { get; set; }
		IDialog Open(string scenePath);
		IDialog GetActiveDialog();
		void CloseDialog(IDialog dialog);
	}
}
