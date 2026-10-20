using System;
using System.Diagnostics;
using System.Windows.Forms;
namespace CodeWalker {
 internal static class ClxnLinks {
  internal const string Discord="https://discord.gg/666clxn";
  internal const string YouTube="https://www.youtube.com/@roltik888";
  internal static void Open(IWin32Window owner,string url){try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(owner,"Не удалось открыть браузер. Ссылка: "+url+"\n"+ex.Message,"666clxn",MessageBoxButtons.OK,MessageBoxIcon.Information);}}
 }
}
