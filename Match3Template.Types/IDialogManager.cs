using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Match3Template
{
	public interface IDialogManager
	{
		static IDialogManager Instance { get; set; }
		void Open(string scenePath);
		void CloseActiveDialog();
	}
}
