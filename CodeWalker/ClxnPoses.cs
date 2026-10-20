using System;
using System.Linq;
using System.Windows.Forms;
using CodeWalker.GameFiles;
using CodeWalker.World;
namespace CodeWalker {
 public partial class WorldForm {
  ComboBox clxnPoseCombo;Button clxnPoseApply;
  readonly System.Diagnostics.Stopwatch clxnPoseClock=System.Diagnostics.Stopwatch.StartNew();
  readonly System.Collections.Concurrent.ConcurrentDictionary<string,string> clxnOutfitErrors=new System.Collections.Concurrent.ConcurrentDictionary<string,string>();
  void ClxnBuildPoseControls(){clxnLibrary.Controls.Add(ClxnStyle.Label("Поза выбранного персонажа",9,ClxnStyle.Muted));var row=ClxnRow();clxnPoseCombo=new ComboBox{Width=190,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=ClxnStyle.Background,ForeColor=ClxnStyle.Text};clxnPoseCombo.Items.AddRange(new object[]{"Стоять","Сидеть · по образцу фото","Танцевать"});clxnPoseCombo.SelectedIndex=0;row.Controls.Add(clxnPoseCombo);clxnPoseApply=ClxnStyle.Button("Применить",(s,e)=>{var p=ClxnSelectedPin();if(p==null||!p.IsPed)return;ClxnProject.Checkpoint();lock(ClxnProject.Sync)p.Pose=clxnPoseCombo.SelectedIndex==1?"sit":clxnPoseCombo.SelectedIndex==2?"dance":"stand";ClxnProject.Commit();clxnMessage="Поза сохранена. Анимация загружается…";},104);clxnPoseApply.Height=29;clxnPoseApply.Margin=Padding.Empty;row.Controls.Add(clxnPoseApply);clxnLibrary.Controls.Add(row);}
  void ClxnShowPose(){if(clxnPoseCombo==null)return;var p=ClxnSelectedPin();clxnPoseCombo.Enabled=clxnPoseApply.Enabled=p?.IsPed??false;if(!clxnPoseCombo.ContainsFocus)clxnPoseCombo.SelectedIndex=p?.Pose=="sit"?1:p?.Pose=="dance"?2:0;}
  void ClxnApplyPose(Ped ped,string pose){
   if(pose!="stand"){
    string dictionary=pose=="sit"?"amb@prop_human_seat_chair@male@generic@base":"anim@amb@nightclub@mini@dance@dance_solo@male@var_a@";
    string clipName=pose=="sit"?"base":"high_center";
    var dict=gameFileCache.GetYcd(JenkHash.GenHash(dictionary));var clock=System.Diagnostics.Stopwatch.StartNew();while(dict!=null&&!dict.Loaded&&clock.ElapsedMilliseconds<15000){gameFileCache.TryLoadEnqueue(dict);System.Threading.Thread.Sleep(2);}
    if(dict==null||!dict.Loaded)throw new InvalidOperationException("Не найдена анимация "+pose+" в установленной GTA V.");
    ClipMapEntry entry;dict.ClipMap.TryGetValue(JenkHash.GenHash(clipName),out entry);if(entry==null)entry=dict.ClipMap.Values.FirstOrDefault(c=>(c.Clip?.Name??"").IndexOf(clipName,StringComparison.OrdinalIgnoreCase)>=0);if(entry==null)throw new InvalidOperationException("Нет клипа "+clipName+" в "+dictionary);ped.AnimClip=entry;ped.Ycd=dict;
   }
   if(ped.AnimClip!=null)ped.AnimClip=new ClipMapEntry{Hash=ped.AnimClip.Hash,Clip=ped.AnimClip.Clip,EnableRootMotion=false,OverridePlayTime=true};ped.EnableRootMotion=false;
  }
  void ClxnUpdatePoseClock(Ped ped,string pose){if(ped.AnimClip==null)return;ped.AnimClip.PlayTime=clxnExporting?(ClxnExportPreparing?0:ClxnVideoPlan.TimeForFrame(clxnExportFrame,clxnExportFps)):(float)clxnPoseClock.Elapsed.TotalSeconds;}
 }
}
