using System.IO;
using Lime;
using Yuzu;

namespace Match3Template.Application
{
	public class Progress : IProgress
	{
		[YuzuMember]
		public int Coins { get; set; } = 10;

		[YuzuMember]
		public int LastSeenCoins { get; set; } = 10;
	}

	public class AppData
	{
		public const string Version = "1.0";

		public static AppData Instance;

		[YuzuMember]
		public Progress Progress { get; set; } = new Progress();

		[YuzuMember]
		public float MusicVolume = 1;

		[YuzuMember]
		public float EffectsVolume = 1;

		[YuzuMember]
#if !iOS && !ANDROID
		public Display SimulateDisplay = DisplayInfo.Displays[0];
#else
		public Display SimulateDisplay;
#endif

		[YuzuMember]
		public DeviceOrientation SimulateDeviceOrientation = DeviceOrientation.LandscapeLeft;

		[YuzuMember]
		public bool EnableSplashScreen = true;

		public static string GetDataFilePath()
		{
			return Path.Combine(GetDataDirectory(), "AppData.dat");
		}

		public static string GetDataDirectory()
		{
			return Environment.GetDataDirectory("GameForest", Application.ApplicationName, Version);
		}

		public static void Load()
		{
			var path = GetDataFilePath();
			Instance = null;
			if (File.Exists(path)) {
				try {
					Instance = The.Persistence.ReadObjectFromFile<AppData>(path);
				}
				catch (System.Exception e) {
					The.Log.Warn("Failed to load the application profile: {0}", e.Message);
				}
			}
			if (Instance == null) {
				Instance = new AppData();
			}
			IProgress.Instance = Instance.Progress;
			AudioSystem.SetGroupVolume(AudioChannelGroup.Music, Instance.MusicVolume);
			AudioSystem.SetGroupVolume(AudioChannelGroup.Effects, Instance.EffectsVolume);
		}

		public void Save()
		{
			EffectsVolume = AudioSystem.GetGroupVolume(AudioChannelGroup.Effects);
			MusicVolume = AudioSystem.GetGroupVolume(AudioChannelGroup.Music);

			try {
				The.Persistence.WriteObjectToFile(GetDataFilePath(), this, Persistence.Format.Binary);
			}
			catch (System.Exception e) {
				The.Log.Warn("AppData saving failed: {0}", e.Message);
			}
		}

	}
}
