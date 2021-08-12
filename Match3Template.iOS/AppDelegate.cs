using AVFoundation;
using Foundation;
using UIKit;

namespace Match3Template.iOS
{
	[Register ("Match3TemplateAppDelegate")]
	public class Match3TemplateAppDelegate : Lime.AppDelegate
	{
		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
		{
			AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Ambient);
			Lime.Application.Initialize(new Lime.ApplicationOptions {
				DecodeAudioInSeparateThread = false,
			});

			Match3Template.Application.Application.Initialize();
			return true;
		}

		public static void Main(string[] args)
		{
			UIApplication.Main(args, null, "Match3TemplateAppDelegate");
		}
	}
}


