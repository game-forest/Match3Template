using Lime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yuzu;

namespace Match3Template.Types
{
	[TangerineRegisterComponent]
	[MutuallyExclusiveDerivedComponents]
	[TangerineMenuPath("Logic/")]
	[TangerineTooltip("Opens provided dialog.")]
    [TangerineAllowedParentTypes(typeof(Button))]
	public class DialogActionComponent : NodeComponent
	{
		[TangerineFileProperty(new[] { ".tan" })]
		[YuzuMember]
        public string ScenePath { get; set; }

        [YuzuMember]
        public bool CloseDialog { get; set; } = true;

		public DialogActionComponent()
        {

        }

        protected override void OnOwnerChanged(Node oldOwner)
        {
            base.OnOwnerChanged(oldOwner);
            (Owner as Button).Clicked += () => {
				if (string.IsNullOrEmpty(ScenePath)) {
					IDialogManager.Instance.CloseActiveDialog();
					return;
				}
				IDialogManager.Instance.Open(ScenePath);
			};
        }
    }
}

