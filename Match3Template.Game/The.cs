using System.Threading;
using Match3Template.Application;
using Match3Template.Dialogs;
using Lime;
using Match3Template.Debug;

namespace Match3Template
{
	public static class The
	{
		public static Application.Application App => Application.Application.Instance;
		public static WindowWidget World => App.World;
		public static IWindow Window => World.Window;
		public static SoundManager SoundManager => SoundManager.Instance;
		public static AppData AppData => AppData.Instance;
		public static Profile Profile => Profile.Instance;
		public static DialogManager DialogManager => DialogManager.Instance;
		public static Logger Log => Logger.Instance;
		public static Persistence Persistence => persistence.Value;

		private static readonly ThreadLocal<Persistence> persistence = new ThreadLocal<Persistence>(() => new Persistence());

		public static Cheats Cheats => Cheats.Instance;
	}
}
