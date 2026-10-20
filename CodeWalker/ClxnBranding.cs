using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace CodeWalker {
 internal static class ClxnBrand {
  internal static readonly Image Logo=LoadLogo();internal static readonly Icon Icon=LoadIcon();
  static Image LoadLogo(){using(var s=typeof(ClxnBrand).Assembly.GetManifestResourceStream("666clxn.Brand.Logo"))using(var image=Image.FromStream(s))return new Bitmap(image);}
  static Icon LoadIcon(){using(var s=typeof(ClxnBrand).Assembly.GetManifestResourceStream("666clxn.Brand.Icon"))using(var icon=new Icon(s))return (Icon)icon.Clone();}
  internal static void WatchWindows(){var known=new HashSet<Form>();Application.Idle+=(s,e)=>{foreach(Form f in Application.OpenForms.Cast<Form>().ToArray()){if(!known.Add(f))continue;f.Icon=Icon;ClxnTheme.Native(f);ClxnTheme.Apply(f);f.Disposed+=(sender,args)=>known.Remove(f);}};}
 }
 internal sealed class ClxnLogo:Control {
  internal ClxnLogo(){DoubleBuffered=true;Size=new Size(64,64);AccessibleName="Логотип 666 CLXN";}
  protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<4||Height<4)return;using(var p=ClxnShape.Round(Width,Height,14)){var old=Region;Region=new Region(p);old?.Dispose();}}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;e.Graphics.DrawImage(ClxnBrand.Logo,ClientRectangle);}
 }
 internal static class ClxnTheme {
  static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ComboBox,object> comboSkins=new System.Runtime.CompilerServices.ConditionalWeakTable<ComboBox,object>();
  static string PathName=>Path.Combine(ClxnHomeForm.DataFolder,"theme.txt");
  internal static bool Load(){try{return File.Exists(PathName)&&File.ReadAllText(PathName).Trim()=="dark";}catch{return false;}}
  internal static void Toggle(){var old=new[]{ClxnStyle.Background,ClxnStyle.Surface,ClxnStyle.Border,ClxnStyle.Accent,ClxnStyle.Muted,ClxnStyle.Text,ClxnStyle.Mint};ClxnStyle.Dark=!ClxnStyle.Dark;try{Directory.CreateDirectory(ClxnHomeForm.DataFolder);File.WriteAllText(PathName,ClxnStyle.Dark?"dark":"light");}catch{}var next=new[]{ClxnStyle.Background,ClxnStyle.Surface,ClxnStyle.Border,ClxnStyle.Accent,ClxnStyle.Muted,ClxnStyle.Text,ClxnStyle.Mint};foreach(Form f in Application.OpenForms.Cast<Form>().ToArray()){Native(f);Apply(f,old,next);if(f is WorldForm world)world.ClxnThemeUpdated();} }
  [System.Runtime.InteropServices.DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
  internal static void Native(Form f){if(f is ExploreForm explorer)explorer.ClxnSetTheme();if(f is Project.ProjectForm project)project.ClxnSetTheme();if(f.IsHandleCreated)try{int dark=ClxnStyle.Dark?1:0;DwmSetWindowAttribute(f.Handle,20,ref dark,4);}catch{}}
  internal static void Apply(Control c,Color[] old=null,Color[] next=null){
   if(c is ClxnToast){c.BackColor=Color.FromArgb(27,30,37);c.ForeColor=Color.White;}
   else{c.BackColor=Map(c.BackColor,old,next,c is Form?ClxnStyle.Background:ClxnStyle.Surface);c.ForeColor=Map(c.ForeColor,old,next,ClxnStyle.Text);}
   if(c is TextBoxBase||c is ComboBox||c is UpDownBase||c is ListBox){c.BackColor=ClxnStyle.Background;c.ForeColor=ClxnStyle.Text;}
   if(c is ComboBox combo){combo.FlatStyle=FlatStyle.Flat;object marker;if(!comboSkins.TryGetValue(combo,out marker)){comboSkins.Add(combo,new object());combo.DrawMode=DrawMode.OwnerDrawFixed;combo.DrawItem+=(sender,e)=>{var selected=(e.State&DrawItemState.Selected)!=0;var background=selected?ClxnStyle.Accent:ClxnStyle.Background;using(var brush=new SolidBrush(background))e.Graphics.FillRectangle(brush,e.Bounds);var text=e.Index>=0&&e.Index<combo.Items.Count?combo.Items[e.Index].ToString():combo.Text;TextRenderer.DrawText(e.Graphics,text,combo.Font,new Rectangle(e.Bounds.X+5,e.Bounds.Y,e.Bounds.Width-8,e.Bounds.Height),combo.Enabled?(selected?ClxnStyle.OnAccent:ClxnStyle.Text):ClxnStyle.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};}}
   if(c is TreeView||c is ListView){c.BackColor=ClxnStyle.Background;c.ForeColor=ClxnStyle.Text;}
   if(c is PropertyGrid grid){grid.ViewBackColor=ClxnStyle.Background;grid.ViewForeColor=ClxnStyle.Text;grid.HelpBackColor=ClxnStyle.Surface;grid.HelpForeColor=ClxnStyle.Text;grid.CategoryForeColor=ClxnStyle.Muted;grid.LineColor=ClxnStyle.Border;}
   if(c is ToolStrip menu){menu.Renderer=new ToolStripProfessionalRenderer(new ClxnMenuColours());foreach(ToolStripItem item in menu.Items){item.BackColor=ClxnStyle.Surface;item.ForeColor=ClxnStyle.Text;}}
   foreach(Control child in c.Controls)Apply(child,old,next);c.Invalidate();
  }
  static Color Map(Color value,Color[] old,Color[] next,Color fallback){if(value==Color.Transparent)return value;if(old!=null)for(int i=0;i<old.Length;i++)if(value.ToArgb()==old[i].ToArgb())return next[i];if(value.IsSystemColor)return fallback;return value;}
  sealed class ClxnMenuColours:ProfessionalColorTable {public override Color MenuItemSelected=>ClxnStyle.Hover;public override Color ToolStripDropDownBackground=>ClxnStyle.Surface;public override Color ImageMarginGradientBegin=>ClxnStyle.Surface;public override Color ImageMarginGradientMiddle=>ClxnStyle.Surface;public override Color ImageMarginGradientEnd=>ClxnStyle.Surface;public override Color MenuBorder=>ClxnStyle.Border;}
 }
 public partial class WorldForm {
  internal void ClxnThemeUpdated(){ClxnTheme.Apply(clxnMore);ClxnRefreshButtons();}
  bool clxnUiCollapsed;ClxnCard clxnRestoreBar;readonly HashSet<Control> clxnVisibleBeforeCollapse=new HashSet<Control>();
  void ClxnBuildPresentation(){
   var logo=new ClxnLogo{Location=new Point(14,8),Size=new Size(60,60)};clxnHud.Controls.Add(logo);
   clxnRestoreBar=new ClxnCard{Location=new Point(20,16),Size=new Size(268,62),Visible=false,BackColor=ClxnStyle.Surface};clxnRestoreBar.Controls.Add(new ClxnLogo{Location=new Point(8,8),Size=new Size(46,46)});var restore=ClxnStyle.Button("Показать интерфейс · F2",(s,e)=>ClxnCollapse(false),198);restore.Location=new Point(60,10);clxnRestoreBar.Controls.Add(restore);Controls.Add(clxnRestoreBar);
   clxnMore.Items.Insert(0,new ToolStripMenuItem("Светлая / тёмная тема",null,(s,e)=>ClxnTheme.Toggle()));clxnMore.Items.Insert(1,new ToolStripMenuItem("Свернуть интерфейс · F2",null,(s,e)=>ClxnCollapse(true)));clxnMore.Items.Add("Логотипы файлов Windows…",null,(s,e)=>{try{ClxnFileBrand.Install();clxnMessage="Логотипы файлов установлены. Проводник обновляет значки.";}catch(Exception ex){clxnMessage="Не удалось установить значки: "+ex.Message;}});
   ClxnTheme.Apply(this);ClxnTheme.Apply(clxnMore);Icon=ClxnBrand.Icon;
  }
  Control[] ClxnFloatingPanels()=>new Control[]{clxnLibrary,clxnWardrobe,clxnEnvironment,clxnLogPanel,clxnFlightPanel};
  void ClxnCollapse(bool collapse){if(clxnWalking||clxnFlightPlaying)return;if(collapse==clxnUiCollapsed)return;ClxnFinishStroke();ClxnSetTool("move");clxnUiCollapsed=collapse;if(collapse){clxnVisibleBeforeCollapse.Clear();foreach(var p in ClxnFloatingPanels()){if(p.Visible)clxnVisibleBeforeCollapse.Add(p);p.Visible=false;}}else foreach(var p in clxnVisibleBeforeCollapse)if(!p.IsDisposed)p.Visible=true;ClxnPresentationTick();Focus();}
  void ClxnPresentationTick(){if(clxnRestoreBar==null)return;bool editor=!clxnWalking&&!clxnFlightPlaying;clxnRestoreBar.Visible=editor&&clxnUiCollapsed;if(editor){clxnHud.Visible=clxnRail.Visible=clxnOptions.Visible=clxnHint.Visible=!clxnUiCollapsed;if(clxnUiCollapsed){foreach(var p in ClxnFloatingPanels())p.Visible=false;foreach(var label in clxnPinLabels)label.Visible=false;ToolsPanel.Visible=ToolbarPanel.Visible=false;clxnRestoreBar.BringToFront();}}}
 }
}
