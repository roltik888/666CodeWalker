using System;
using System.IO;
using System.Windows.Forms;
namespace CodeWalker {
 public partial class WorldForm {
  long clxnAssetWorkTicks=DateTime.UtcNow.Ticks;
  int clxnPerformance=1;ToolStripMenuItem[] clxnPerformanceItems;
  void ClxnBuildPerformanceMenu(){try{int saved;if(int.TryParse(File.ReadAllText(Path.Combine(ClxnHomeForm.DataFolder,"performance.txt")),out saved)&&saved>=0&&saved<=2)clxnPerformance=saved;}catch{}
   var menu=new ToolStripMenuItem("Производительность");clxnPerformanceItems=new ToolStripMenuItem[3];var names=new[]{"Быстрее — меньше дальность и без травы","Баланс — средняя дальность","Детальнее — полная дальность и трава"};for(int i=0;i<3;i++){int profile=i;var item=new ToolStripMenuItem(names[i]);item.Checked=i==clxnPerformance;item.Click+=(s,e)=>ClxnApplyPerformance(profile,true);menu.DropDownItems.Add(item);clxnPerformanceItems[i]=item;}clxnMore.Items.Add(menu);clxnMore.Items.Add("Записать статистику в журнал",null,(s,e)=>{clxnJournal.Add(Renderer.GetStatusText());clxnLogPanel.Visible=true;clxnFlightPanel.Visible=clxnLibrary.Visible=false;});clxnMore.Items.Add(new ToolStripSeparator());
  }
  void ClxnApplyPerformance(int profile,bool persist){clxnPerformance=profile;lock(RenderSyncRoot){Renderer.renderworldDetailDistMult=profile==0?.55f:profile==1?.8f:1f;Renderer.renderworldLodDistMult=profile==0?.65f:profile==1?.9f:1f;Renderer.rendergrass=profile==2;}if(clxnPerformanceItems!=null)for(int i=0;i<clxnPerformanceItems.Length;i++)clxnPerformanceItems[i].Checked=i==profile;if(persist){try{Directory.CreateDirectory(ClxnHomeForm.DataFolder);File.WriteAllText(Path.Combine(ClxnHomeForm.DataFolder,"performance.txt"),profile.ToString());}catch(Exception ex){clxnJournal.Add("Профиль применён, но не сохранён: "+ex.Message);}clxnMessage="Профиль производительности: "+new[]{"Быстрее","Баланс","Детальнее"}[profile]+".";}}
 }
}
