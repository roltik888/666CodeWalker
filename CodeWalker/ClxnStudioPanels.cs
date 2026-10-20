using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SharpDX;
using Point=System.Drawing.Point;
using Color=System.Drawing.Color;
namespace CodeWalker {
 public partial class WorldForm {
  readonly ClxnJournal clxnJournal=new ClxnJournal();
  ClxnFlowCard clxnLogPanel,clxnFlightPanel;TextBox clxnLogText;int clxnLogRevision=-1;
  ListBox clxnKeyList;NumericUpDown clxnKeySeconds,clxnKeyFov;CheckBox clxnSmooth;
  Label clxnFlightInfo,clxnPlaybackInfo;Panel clxnPlaybackBar;TrackBar clxnScrub;bool clxnScrubUpdating;
  Button clxnPauseButton;ContextMenuStrip clxnMore;
  FlowLayoutPanel clxnBrushSettings,clxnActorSettings;
  void ClxnBuildPanels(){
   clxnLogPanel=ClxnSidePanel(255);clxnLogPanel.Controls.Add(ClxnStyle.Label("ЖУРНАЛ СОБЫТИЙ",10));var logActions=ClxnRow();logActions.Controls.Add(ClxnStyle.Button("Сохранить .txt",(s,e)=>{using(var d=new SaveFileDialog{Filter="Журнал|*.txt",FileName="666clxn-log.txt"})if(d.ShowDialog(this)==DialogResult.OK)try{File.WriteAllLines(d.FileName,clxnJournal.Snapshot());clxnMessage="Журнал сохранён.";}catch(Exception ex){clxnMessage="Ошибка экспорта журнала: "+ex.Message;}},145));logActions.Controls.Add(ClxnStyle.Button("Очистить",(s,e)=>clxnJournal.Clear(),145));clxnLogPanel.Controls.Add(logActions);clxnLogText=new TextBox{Width=300,Height=130,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=ClxnStyle.Background,ForeColor=ClxnStyle.Text,BorderStyle=BorderStyle.None};clxnLogPanel.Controls.Add(clxnLogText);
   clxnFlightPanel=ClxnSidePanel(520);clxnFlightPanel.Controls.Add(ClxnStyle.Label("ПРОЛЁТКА / КАДРЫ КАМЕРЫ",10));clxnFlightInfo=ClxnStyle.Label("0 кадров · 0 с",9,ClxnStyle.Muted);clxnFlightPanel.Controls.Add(clxnFlightInfo);clxnFlightPanel.Controls.Add(ClxnStyle.Button("Воспроизвести · F5",(s,e)=>ClxnStartFlight(),300,true));clxnFlightPanel.Controls.Add(ClxnStyle.Button("Экспорт MP4…",(s,e)=>ClxnExportDialog(),300));clxnFlightPanel.Controls.Add(new Label{Text="Поставьте камеру и добавьте два кадра.",AutoSize=true,ForeColor=ClxnStyle.Muted,Margin=new Padding(0,5,0,8)});
   var capture=ClxnRow();capture.Controls.Add(ClxnStyle.Button("＋ Кадр · F4",(s,e)=>ClxnCaptureKey(),145,true));capture.Controls.Add(ClxnStyle.Button("Заменить камерой",(s,e)=>ClxnReplaceKey(),145));clxnFlightPanel.Controls.Add(capture);
   clxnKeyList=new ClxnListBox{Width=300,Height=76,IntegralHeight=false,BackColor=ClxnStyle.Background,ForeColor=ClxnStyle.Text,BorderStyle=BorderStyle.None};clxnKeyList.SelectedIndexChanged+=(s,e)=>ClxnShowKey();clxnKeyList.DoubleClick+=(s,e)=>ClxnVisitKey();clxnFlightPanel.Controls.Add(clxnKeyList);
   var order=ClxnRow();order.Controls.Add(ClxnStyle.Button("Выше",(s,e)=>ClxnMoveKey(-1),90));order.Controls.Add(ClxnStyle.Button("Ниже",(s,e)=>ClxnMoveKey(1),90));order.Controls.Add(ClxnStyle.Button("Удалить",(s,e)=>ClxnDeleteKey(),102));clxnFlightPanel.Controls.Add(order);
   var timing=ClxnRow();timing.Controls.Add(ClxnStyle.Label("До следующего, с",9));clxnKeySeconds=new NumericUpDown{Minimum=.25m,Maximum=600,DecimalPlaces=2,Increment=.25m,Value=4,Width=76,BackColor=ClxnStyle.Background,ForeColor=ClxnStyle.Text};timing.Controls.Add(clxnKeySeconds);timing.Controls.Add(ClxnStyle.Label("FOV °",9));clxnKeyFov=new NumericUpDown{Minimum=15,Maximum=120,Value=60,Width=58,BackColor=ClxnStyle.Background,ForeColor=ClxnStyle.Text};timing.Controls.Add(clxnKeyFov);clxnFlightPanel.Controls.Add(timing);
   var edit=ClxnRow();edit.Controls.Add(ClxnStyle.Button("Применить",(s,e)=>ClxnApplyKey(),145));edit.Controls.Add(ClxnStyle.Button("К этому кадру",(s,e)=>ClxnVisitKey(),145));clxnFlightPanel.Controls.Add(edit);
   clxnSmooth=new ClxnCheckBox{Text="Плавные переходы",AutoSize=true,Checked=ClxnProject.SmoothFlight};clxnSmooth.CheckedChanged+=(s,e)=>{if(clxnSmooth.Checked==ClxnProject.SmoothFlight)return;ClxnProject.Checkpoint();ClxnProject.SmoothFlight=clxnSmooth.Checked;ClxnProject.Commit();};clxnFlightPanel.Controls.Add(clxnSmooth);
   clxnScrub=new TrackBar{Width=300,Height=35,Maximum=1000,TickStyle=TickStyle.None,BackColor=ClxnStyle.Surface};clxnScrub.ValueChanged+=(s,e)=>{if(!clxnScrubUpdating)ClxnSeekFlight(clxnScrub.Value/1000f);};clxnFlightPanel.Controls.Add(clxnScrub);
   clxnPlaybackBar=new ClxnCard{Size=new Size(785,62),Location=new Point(20,16),Visible=false,BackColor=ClxnStyle.Surface};Controls.Add(clxnPlaybackBar);clxnPlaybackInfo=new Label{Location=new Point(16,22),Size=new Size(250,24),ForeColor=ClxnStyle.Text};clxnPlaybackBar.Controls.Add(clxnPlaybackInfo);clxnPauseButton=ClxnStyle.Button("Пауза · F5",(s,e)=>ClxnPauseFlight(),130);clxnPauseButton.Location=new Point(280,10);clxnPlaybackBar.Controls.Add(clxnPauseButton);var stop=ClxnStyle.Button("Завершить · Esc",(s,e)=>ClxnStopFlight(),155);stop.Location=new Point(416,10);clxnPlaybackBar.Controls.Add(stop);var clean=ClxnStyle.Button("Чистый вид · Tab",(s,e)=>clxnPlaybackBar.Visible=false,165);clean.Location=new Point(585,10);clxnPlaybackBar.Controls.Add(clean);
   clxnMore=new ContextMenuStrip{BackColor=ClxnStyle.Surface,ForeColor=ClxnStyle.Text,ShowImageMargin=false};ClxnBuildPerformanceMenu();ClxnBuildZonesMenu();clxnMore.Items.Add("Карта · Обычная / MCL / Капты",null,(s,e)=>ClxnOpenTactical());clxnMore.Items.Add("Отменить · Ctrl+Z",null,(s,e)=>{ClxnFinishStroke();ClxnProject.Undo();});clxnMore.Items.Add("Повторить · Ctrl+Y",null,(s,e)=>{ClxnFinishStroke();ClxnProject.Undo(true);});clxnMore.Items.Add(new ToolStripSeparator());clxnMore.Items.Add("Ресурсы карты…",null,(s,e)=>ClxnImportMap());clxnMore.Items.Add("EMS-концепт",null,(s,e)=>ClxnStartEMS());clxnMore.Items.Add("Инструменты CodeWalker",null,(s,e)=>{ToolsPanel.Visible=!ToolsPanel.Visible;ToolbarPanel.Top=100;ToolbarPanel.Visible=ToolsPanel.Visible;});clxnMore.Items.Add("Где взять образы Majestic?",null,(s,e)=>MessageBox.Show(this,"В официальной wiki Majestic есть каталоги одежды и педов: https://wiki.majestic-rp.ru/ru\n\nЭто справочник внешнего вида. Отдельные 3D-файлы запрашивайте у автора одежды или команды проекта. В редакторе доступны локально установленные модели GTA V; серверная одежда сама не загружается.\n\nДля публичных авторских модов: https://www.gta5-mods.com/player","Персонажи и ресурсы",MessageBoxButtons.OK,MessageBoxIcon.Information));
  }
  FlowLayoutPanel ClxnRow()=>new FlowLayoutPanel{Width=310,Height=47,WrapContents=false,Margin=Padding.Empty};
  ClxnFlowCard ClxnSidePanel(int height){var p=new ClxnFlowCard{Size=new Size(340,height),Location=new Point(ClientSize.Width-360,110),Visible=false,BackColor=ClxnStyle.Surface,ForeColor=ClxnStyle.Text,Font=new Font("Segoe UI",10),Padding=new Padding(14),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};Controls.Add(p);return p;}
  void ClxnToggleLogs(){bool show=!clxnLogPanel.Visible;clxnWardrobe.Visible=false;if(clxnEnvironment!=null)clxnEnvironment.Visible=false;clxnLibrary.Visible=false;clxnFlightPanel.Visible=false;clxnLogPanel.Visible=show;clxnLogPanel.BringToFront();}
  void ClxnToggleFlightPanel(){bool show=!clxnFlightPanel.Visible;clxnWardrobe.Visible=false;if(clxnEnvironment!=null)clxnEnvironment.Visible=false;clxnLibrary.Visible=false;clxnLogPanel.Visible=false;clxnFlightPanel.Visible=show;ClxnRefreshKeys();clxnFlightPanel.BringToFront();}
  void ClxnPanelsTick(){
   if(clxnLogPanel.Visible&&clxnLogRevision!=clxnJournal.Revision){clxnLogRevision=clxnJournal.Revision;clxnLogText.Lines=clxnJournal.Snapshot();clxnLogText.SelectionStart=clxnLogText.TextLength;clxnLogText.ScrollToCaret();}
   int count;float total;lock(ClxnProject.Sync){count=ClxnProject.CameraKeys.Count;total=ClxnFlightMath.Duration(ClxnProject.CameraKeys);}
   clxnFlightInfo.Text=count+" кадров · "+total.ToString("0.##")+" с · последний кадр — финиш";
   clxnPlaybackInfo.Text=(clxnFlightPaused?"ПАУЗА  ":"ПРОЛЁТКА  ")+clxnFlightTime.ToString("0.0")+" / "+clxnFlightLength.ToString("0.0")+" с";clxnPauseButton.Text=clxnFlightPaused?"Продолжить · F5":"Пауза · F5";
   if(clxnFlightPlaying){clxnScrubUpdating=true;clxnScrub.Value=Math.Min(1000,(int)(clxnFlightTime/Math.Max(.001f,clxnFlightLength)*1000));clxnScrubUpdating=false;}
   ClxnExportTick();
   if(clxnFlightEnded){clxnFlightEnded=false;ClxnStopFlight();clxnMessage="Пролётка завершена.";}
  }
 }
}
