using System;

namespace Match3Template.Win
{
	public class Application
	{
		[STAThread]
		public static void Main(string[] args)
		{
			Lime.Application.Initialize(new Lime.ApplicationOptions());
			Match3Template.Application.Application.Initialize();
			Lime.Application.Run();
		}
	}
}