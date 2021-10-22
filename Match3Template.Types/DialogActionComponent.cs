using Lime;
using System;
using System.Collections.Generic;
using System.Linq;

using Yuzu;

#if TANGERINE
using System.ComponentModel.Composition;
using Tangerine.Core;
using Tangerine.UI;
#endif // TANGERINE

namespace Match3Template.Types
{
#if TANGERINE
	public static class TangerinePlugin
	{
		[Export(nameof(Orange.OrangePlugin.Initialize))]
		public static void Initialize()
		{
			var editorRegister = Tangerine.UI.Inspector.InspectorPropertyRegistry.Instance;
			editorRegister.Items.Insert(0, new Tangerine.UI.Inspector.InspectorPropertyRegistry.RegistryItem(
				(c) => c.PropertyName == "NewDialogAnimation",
				(c) => {
					Func<string> getter = () => {
						var component = (DialogAction)c.Objects.First();
						return component.ScenePath;
					};
					return new AnimationStringPropertyEditor(
						editorParams: c,
						multiline: true,
						nodeProvider: () => {
							var component = (DialogAction)c.Objects.First();
							try {
								return Node.Load(component.ScenePath);
							} catch {
								return null;
							}
						},
						needInvalidateNode: new DelegateDataflowProvider<string>(getter).DistinctUntilChanged()
					);
				}
			));
			editorRegister.Items.Insert(0, new Tangerine.UI.Inspector.InspectorPropertyRegistry.RegistryItem(
				(c) => c.PropertyName == "ActiveDialogAnimation",
				(c) => new AnimationStringPropertyEditor(
					editorParams: c,
					multiline: true,
					nodeProvider: () => {
						return Document.Current.RootNode;
					},
					needInvalidateNode: new DelegateDataflowProvider<string>(() => string.Empty).DistinctUntilChanged()
				)
			));
		}
	}

	public class AnimationStringPropertyEditor : CommonPropertyEditor<string>
	{
		private readonly EditBox editBox;
		private Node node;

		public AnimationStringPropertyEditor(
			IPropertyEditorParams editorParams,
			bool multiline,
			Func<Node> nodeProvider,
			IDataflowProvider<string> needInvalidateNode
		) : base(editorParams)
		{
			if (EditorParams.Objects.Skip(1).Any()) {
				EditorContainer.AddNode(CreateWarning("Edit of triggers isn't supported for multiple selection."));
				return;
			}
			var button = new ThemedButton {
				Text = "...",
				MinMaxWidth = 20,
				LayoutCell = new LayoutCell(Alignment.Center)
			};
			EditorContainer.AddNode(editBox = editorParams.EditBoxFactory());
			EditorContainer.AddNode(Spacer.HSpacer(4));
			EditorContainer.AddNode(button);
			EditorContainer.AddNode(Spacer.HStretch());
			editBox.Submitted += text => {
				var newValue = FilterTriggers(text);
				editBox.Text = newValue;
				SetProperty(newValue);
			};
			button.AddChangeWatcher(needInvalidateNode, (v) => node = nodeProvider());
			button.Clicked += () => {
				if (node == null) {
					return;
				}
				var value = CoalescedPropertyValue().GetValue().Value;
				var currentTriggers = string.IsNullOrEmpty(value) ?
					new HashSet<string>() :
					value.Split(',').Select(el => el.Trim()).ToHashSet();
				var window = new TriggerSelectionDialog(
					node,
					currentTriggers,
					s => {
						s = FilterTriggers(s);
						SetProperty(s);
						editBox.Text = s;
					}
				);
			};
			editBox.AddLateChangeWatcher(CoalescedPropertyValue(), v => editBox.Text = v.Value);
			Invalidate();
		}

		public void Invalidate()
		{
			var value = CoalescedPropertyValue().GetValue().Value;
			if (node != null && editBox != null) {
				editBox.Text = FilterTriggers(value);
			}
		}

		private Dictionary<string, HashSet<string>> GetAvailableTriggers()
		{
			var triggers = new Dictionary<string, HashSet<string>>();
			foreach (var a in node.Animations) {
				foreach (var m in a.Markers.Where(i => !string.IsNullOrEmpty(i.Id))) {
					var id = a.Id != null ? m.Id + '@' + a.Id : m.Id;
					var key = a.Id ?? "";
					if (!triggers.Keys.Contains(key)) {
						triggers[key] = new HashSet<string>();
					}
					if (!triggers[key].Contains(id)) {
						triggers[key].Add(id);
					}
				}
			}
			return triggers;
		}

		private Widget CreateWarning(string message)
		{
			return new Widget {
				Layout = new HBoxLayout(),
				Nodes = {
					new ThemedSimpleText {
						Text = message,
						Padding = Theme.Metrics.ControlsPadding,
						LayoutCell = new LayoutCell(Alignment.Center),
						VAlignment = VAlignment.Center,
						ForceUncutText = false
					}
				},
				Presenter = new WidgetFlatFillPresenter(Theme.Colors.WarningBackground)
			};
		}

		private string FilterTriggers(string text)
		{
			var newValue = "";
			if (!string.IsNullOrEmpty(text)) {
				var triggersToSet = text.Split(',').ToList();
				var triggers = GetAvailableTriggers();
				foreach (var key in triggers.Keys) {
					foreach (var trigger in triggersToSet) {
						if (triggers[key].Contains(trigger.Trim(' '))) {
							newValue += trigger.Trim(' ') + ',';
							break;
						}
					}
				}
				if (!string.IsNullOrEmpty(newValue)) {
					newValue = newValue.Trim(',');
				}
			}
			return newValue;
		}
	}
#endif // TANGERINE

	[TangerineRegisterComponent]
	public class DialogActionComponent1 : DialogActionComponent
	{

	}

	[TangerineRegisterComponent]
	public class DialogActionComponent2 : DialogActionComponent
	{

	}

	[TangerineRegisterComponent]
	public class DialogActionComponent3 : DialogActionComponent
	{

	}

	[TangerineRegisterComponent]
	public class DialogActionComponent4 : DialogActionComponent
	{

	}

	[TangerineRegisterComponent]
	[TangerineMenuPath("Logic/")]
	[TangerineTooltip("Opens provided dialog.")]
	[TangerineAllowedParentTypes(typeof(Node))]
	public class DialogActionComponentList : NodeComponent
	{
		[YuzuMember]
		public AnimableList<DialogAction> Actions { get; private set; } =
			new AnimableList<DialogAction>();

		public DialogActionComponentList()
		{

		}

		protected override void OnOwnerChanged(Node oldOwner)
		{
			base.OnOwnerChanged(oldOwner);
			Actions.Owner = Owner;
		}

		protected override void OnBuilt()
		{
			base.OnBuilt();
			foreach (var action in Actions) {
				action.OnBuilt(Owner);
			}
		}
	}

	// [AllowMultipleComponents]
	[TangerineRegisterComponent]
	// [AllowOnlyOneComponent]
	[TangerineMenuPath("Logic/")]
	[TangerineTooltip("Opens provided dialog.")]
	[TangerineAllowedParentTypes(typeof(Node))]
	public class DialogActionComponent : NodeComponent
	{
		[YuzuMember]
		public DialogAction Action { get; set; } = new DialogAction();
		public DialogActionComponent()
		{

		}

		protected override void OnBuilt()
		{
			base.OnBuilt();
			Action.OnBuilt(Owner);
		}
	}


	public class DialogAction : Animable
	{
		[YuzuMember]
		public string ButtonName { get; set; }

		[TangerineFileProperty(new[] { "tan" })]
		[YuzuMember]
		public string ScenePath { get; set; }

		[YuzuMember]
		public bool CloseActiveDialog { get; set; } = true;

		[YuzuMember]
		public string NewDialogAnimation { get; set; }

		[YuzuMember]
		public string ActiveDialogAnimation { get; set; }

		public DialogAction()
		{

		}

		public void OnBuilt(Node Owner)
		{
			var button = Owner as Button;
			if (!string.IsNullOrEmpty(ButtonName)) {
				button = Owner.TryFind<Button>(ButtonName);
			}
			if (button == null) {
				System.Console.WriteLine($"Neither `{Owner}` nor `{ButtonName}` is a button.");
				return;
			}
			button.Clicked += OpenDialog;
		}

		private void OpenDialog()
		{
			WidgetContext.Current.Root.Tasks.Add(OpenDialogTask());
		}

		private IEnumerator<object> OpenDialogTask()
		{
			var m = IDialogManager.Instance;
			var activeDialog = m.GetActiveDialog();
			var scenePath = ScenePath;
			var newDialogAnimation = NewDialogAnimation;
			var closeActiveDialog = CloseActiveDialog;
			if (activeDialog != null) {
				if (!string.IsNullOrEmpty(ActiveDialogAnimation)) {
					var animations = RunAnimationFromTriggerString(activeDialog.Root, ActiveDialogAnimation);
					foreach (var animation in animations) {
						yield return animation;
					}
				}
				if (closeActiveDialog) {
					m.CloseDialog(activeDialog);
				}
			}
			if (!string.IsNullOrEmpty(scenePath)) {
				IDialog dialog;
				if (m.GetActiveDialog() == null || m.GetActiveDialog().Path != scenePath) {
					dialog = m.Open(scenePath);
				} else {
					dialog = m.GetActiveDialog();
				}
				RunAnimationFromTriggerString(dialog.Root, newDialogAnimation);
			}
		}

		private static List<Animation> RunAnimationFromTriggerString(Node node, string trigger)
		{
			List<Animation> runAnimations = new List<Animation>();
			if (string.IsNullOrEmpty(trigger)) {
				return runAnimations;
			}
			TriggerMultipleAnimations(trigger);
			return runAnimations;

			void TriggerMultipleAnimations(string trigger)
			{
				if (trigger.Contains(',')) {
					foreach (var s in trigger.Split(',')) {
						TriggerAnimation(s.Trim());
					}
				} else {
					TriggerAnimation(trigger);
				}
			}

			void TriggerAnimation(string markerWithOptionalAnimationId)
			{
				if (markerWithOptionalAnimationId.Contains('@')) {
					var s = markerWithOptionalAnimationId.Split('@');
					if (s.Length == 2) {
						var markerId = s[0];
						var animationId = s[1];
						if (node.Animations.TryFind(animationId, out var animation)) {
							if (animation.TryRun(markerId)) {
								runAnimations.Add(animation);
							}
						}
					}
				} else {
					if (node.TryRunAnimation(markerWithOptionalAnimationId, null)) {
						runAnimations.Add(node.DefaultAnimation);
					}
				}
			}
		}
	}
}

