using System;
using System.Collections.Generic;
using System.Linq;

namespace Match3Template.Dialogs
{
	public class DialogManager
	{
		public static DialogManager Instance { get; } = new DialogManager();

		public DialogManager()
		{
			var baseType = typeof(Dialog);
			foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
				foreach (var type in assembly.GetTypes()) {
					if (typeof(Dialog).IsAssignableFrom(type)) {
						var atrs = type.GetCustomAttributes(typeof(ScenePathAttribute), false);
						if (atrs.Length > 0) {
							var path = (atrs[0] as ScenePathAttribute).ScenePath;
							Overrides.Add(path, () => (Dialog)Activator.CreateInstance(type));
						}
					}
				}
			}
		}

		public List<Dialog> ActiveDialogs { get; } = new List<Dialog>();
		public Dialog Top => ActiveDialogs.FirstOrDefault();

		public Dictionary<string, Func<Dialog>> Overrides { get; } = new Dictionary<string, Func<Dialog>>();

		public void Open(string scenePath)
		{
			var dialog = Overrides.ContainsKey(scenePath) ? Overrides[scenePath]() : new Dialog(scenePath);
			Open(dialog);
		}

		public void Open(Dialog dialog)
		{
			dialog.Attach(The.World);
			ActiveDialogs.Insert(0, dialog);
		}

		public void Remove(Dialog dialog)
		{
			ActiveDialogs.Remove(dialog);
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class ScenePathAttribute : Attribute
	{
		public string ScenePath { get; set;}
		public ScenePathAttribute(string scenePath) { ScenePath = scenePath;} 
	}
}
