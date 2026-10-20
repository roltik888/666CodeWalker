using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace CodeWalker {
 internal static class ClxnMotion {
  internal static bool Enabled=true;
  static readonly Dictionary<Control,int> panels=new Dictionary<Control,int>();
  static readonly Timer timer=new Timer{Interval=25};
  static ClxnMotion(){timer.Tick+=(s,e)=>{foreach(var p in panels.Keys.ToArray()){if(p.IsDisposed||!p.Visible||p.Parent==null){panels.Remove(p);continue;}int target=p.Parent.ClientSize.Width-p.Width-20;int delta=p.Left-target;if(!Enabled||Math.Abs(delta)<2){p.Left=target;panels.Remove(p);}else p.Left=target+(int)(delta*.74f);}if(panels.Count==0)timer.Stop();};}
  internal static void Reveal(Control p){if(!Enabled||p.Parent==null||!p.Visible)return;p.Left=p.Parent.ClientSize.Width-p.Width+64;panels[p]=0;timer.Start();}
 }
 internal sealed class ClxnAmbientRibbon:Control {
  readonly Timer timer=new Timer{Interval=50};float phase;
  internal ClxnAmbientRibbon(){DoubleBuffered=true;SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;timer.Tick+=(s,e)=>{if(!Visible||!ClxnMotion.Enabled||!(FindForm()?.ContainsFocus??false))return;phase+=.07f;Invalidate();};timer.Start();Enabled=false;}
  protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;for(int line=0;line<6;line++){var points=new PointF[32];for(int i=0;i<points.Length;i++)points[i]=new PointF(i*Width/31f,Height*.5f+(float)Math.Sin(i*.18f+phase+line*.4f)*(5+line*2));using(var p=new Pen(Color.FromArgb(35+line*10,90,90,90),1))g.DrawCurve(p,points);}for(int i=0;i<7;i++){float angle=phase*.55f+i*.897f;float px=Width*.5f+(float)Math.Cos(angle)*Width*.43f;float py=Height*.5f+(float)Math.Sin(angle*1.4f)*Height*.33f;float size=2+(float)Math.Sin(angle)*.6f;using(var brush=new SolidBrush(Color.FromArgb(65+i*8,75,75,75)))g.FillEllipse(brush,px,py,size,size);}float x=(float)((Math.Sin(phase*.5f)+1)*.5f)*(Width-10);using(var b=new SolidBrush(Color.FromArgb(130,75,75,75)))g.FillEllipse(b,x,Height/2f-2,4,4);}
 }
 internal sealed class ClxnWheelRouter:IMessageFilter {
  readonly Form form;internal ClxnWheelRouter(Form f){form=f;}
  public bool PreFilterMessage(ref Message m){if(m.Msg!=0x20A||!form.ContainsFocus)return false;foreach(var card in form.Controls.OfType<ClxnFlowCard>()){if(!card.Visible||!card.AutoScroll||!card.RectangleToScreen(card.ClientRectangle).Contains(Cursor.Position))continue;int delta=(short)((m.WParam.ToInt64()>>16)&0xffff);card.AutoScrollPosition=new Point(0,Math.Max(0,-card.AutoScrollPosition.Y-delta/120*48));return true;}return false;}
 }
 public partial class WorldForm {
  ClxnAmbientRibbon clxnRibbon;
  void ClxnBuildMotion(){var wheel=new ClxnWheelRouter(this);Application.AddMessageFilter(wheel);FormClosed+=(s,e)=>Application.RemoveMessageFilter(wheel);var ribbon=clxnRibbon=new ClxnAmbientRibbon{Location=new Point(285,18),Size=new Size(160,38),Anchor=AnchorStyles.Top|AnchorStyles.Left};clxnHud.Controls.Add(ribbon);ribbon.SendToBack();var motion=new ToolStripMenuItem("Анимации интерфейса"){Checked=ClxnMotion.Enabled,CheckOnClick=true};motion.CheckedChanged+=(s,e)=>{ClxnMotion.Enabled=motion.Checked;clxnHud.Invalidate();};clxnMore.Items.Insert(0,motion);}
 }
}
