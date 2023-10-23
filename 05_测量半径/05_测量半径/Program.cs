using System;
using System.Windows.Forms;

namespace _05_测量半径
{
	// Token: 0x02000003 RID: 3
	internal static class Program
	{
		// Token: 0x0600000E RID: 14 RVA: 0x00002BDC File Offset: 0x00000DDC
		[STAThread]
		private static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new FrmMain());
		}
	}
}
