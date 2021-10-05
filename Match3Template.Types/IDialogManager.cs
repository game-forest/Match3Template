using Lime;

namespace Match3Template
{
	public interface IDialog
	{
		public Widget Root { get; }

		public string Path { get; }

		void Close();
	}

	public interface IDialogManager
	{
		static IDialogManager Instance { get; set; }
		IDialog Open(string scenePath);
		IDialog GetActiveDialog();
		void CloseDialog(IDialog dialog);
	}

	public interface ICheatManager
	{
		static ICheatManager Instance { get; set; }

		public bool DebugMatch3 { get; }
	}

	public interface IProgress
	{
		public static IProgress Instance { get; set; }

		public int Coins { get; set; }

		public int LastSeenCoins { get; set; }
	}
}
