using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace CodeWalker {
 internal sealed class ClxnToast:Label {
  readonly Timer motion=new Timer{Interval=25};float progress=1;
  internal ClxnToast(){DoubleBuffered=true;BackColor=Color.FromArgb(27,30,37);ForeColor=Color.White;Padding=new Padding(16,9,16,9);AutoEllipsis=true;motion.Tick+=(s,e)=>{if(!Visible||!ClxnMotion.Enabled||!(FindForm()?.ContainsFocus??false)){progress=1;motion.Stop();Invalidate();return;}progress=Math.Min(1,progress+.095f);Invalidate();if(progress>=1)motion.Stop();};}
  protected override void OnTextChanged(EventArgs e){base.OnTextChanged(e);progress=ClxnMotion.Enabled?0:1;motion.Start();Invalidate();}
  protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<8||Height<8)return;using(var shape=ClxnShape.Round(Width,Height,16)){var old=Region;Region=new Region(shape);old?.Dispose();}}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BackColor);using(var shape=ClxnShape.Round(Width,Height,16))using(var border=new Pen(Color.FromArgb(76,76,76)))g.DrawPath(border,shape);float t=1-(1-progress)*(1-progress);var bounds=new Rectangle(Padding.Left,Padding.Top+(int)((1-t)*6),Math.Max(1,Width-Padding.Horizontal),Math.Max(1,Height-Padding.Vertical));TextRenderer.DrawText(g,Text,Font,bounds,ClxnShape.Mix(Color.FromArgb(95,95,95),Color.White,t),TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis|TextFormatFlags.VerticalCenter);if(progress<1)using(var pen=new Pen(Color.FromArgb(140,140,140),2))g.DrawLine(pen,18,Height-3,18+(Width-36)*t,Height-3);}
  protected override void Dispose(bool disposing){if(disposing)motion.Dispose();base.Dispose(disposing);}
 }
}
