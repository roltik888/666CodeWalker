using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace CodeWalker {
 internal static class ClxnShape {
  internal static GraphicsPath Round(int w,int h,int radius=22){var p=new GraphicsPath();int r=Math.Min(radius*2,Math.Min(w-1,h-1));if(r<2){p.AddRectangle(new Rectangle(0,0,Math.Max(1,w),Math.Max(1,h)));return p;}p.AddArc(0,0,r,r,180,90);p.AddArc(w-r-1,0,r,r,270,90);p.AddArc(w-r-1,h-r-1,r,r,0,90);p.AddArc(0,h-r-1,r,r,90,90);p.CloseFigure();return p;}
  internal static Color Mix(Color a,Color b,float t)=>Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));
 }
 internal sealed class ClxnCard:Panel {
  internal ClxnCard(){DoubleBuffered=true;Padding=new Padding(14);}
  protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<30||Height<30)return;using(var path=ClxnShape.Round(Width,Height)){var old=Region;Region=new Region(path);old?.Dispose();}}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Width<30||Height<30)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=ClxnShape.Round(Width,Height)){using(var fill=new SolidBrush(ClxnStyle.Surface))g.FillPath(fill,path);using(var border=new Pen(ClxnStyle.Border,1))g.DrawPath(border,path);}}

 }
 internal sealed class ClxnActionButton:Button {
  readonly Timer animation=new Timer{Interval=16};bool hot,down;float blend,selectedBlend,ripple;Point rippleOrigin;
  internal ClxnActionButton(){DoubleBuffered=true;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;MouseEnter+=(s,e)=>{hot=true;animation.Start();};MouseLeave+=(s,e)=>{hot=false;down=false;animation.Start();};MouseDown+=(s,e)=>{down=true;ripple=1;rippleOrigin=PointToClient(Cursor.Position);animation.Start();Invalidate();};MouseUp+=(s,e)=>{down=false;Invalidate();};BackColorChanged+=(s,e)=>animation.Start();animation.Tick+=(s,e)=>{if(!ClxnMotion.Enabled){blend=hot?1:0;selectedBlend=BackColor==ClxnStyle.Accent?1:0;ripple=0;Invalidate();animation.Stop();return;}ripple*=.86f;var target=hot?1f:0f;var active=BackColor==ClxnStyle.Accent?1f:0f;blend+=(target-blend)*.2f;selectedBlend+=(active-selectedBlend)*.18f;Invalidate();if(Math.Abs(target-blend)<.005f&&Math.Abs(active-selectedBlend)<.005f&&ripple<.005f)animation.Stop();};}
  protected override void Dispose(bool disposing){if(disposing)animation.Dispose();base.Dispose(disposing);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent?.BackColor??ClxnStyle.Surface);int inset=down?3:1;g.TranslateTransform(inset,inset);using(var path=ClxnShape.Round(Math.Max(2,Width-inset*2),Math.Max(2,Height-inset*2),12)){Color fill=ClxnShape.Mix(ClxnShape.Mix(ClxnStyle.Surface,ClxnStyle.Hover,blend),ClxnStyle.Accent,selectedBlend);using(var brush=new SolidBrush(fill))g.FillPath(brush,path);using(var pen=new Pen(Focused?ClxnStyle.Accent:ClxnStyle.Border,1))g.DrawPath(pen,path);}g.ResetTransform();if(ripple>.01f){using(var clip=ClxnShape.Round(Width,Height,12)){g.SetClip(clip);float radius=(1-ripple)*Width;using(var b=new SolidBrush(Color.FromArgb((int)(ripple*35),selectedBlend>.5f?Color.White:Color.Black)))g.FillEllipse(b,rippleOrigin.X-radius,rippleOrigin.Y-radius,radius*2,radius*2);g.ResetClip();}}TextRenderer.DrawText(g,Text,Font,new Rectangle(0,down?1:0,Width,Height),Enabled?(selectedBlend>.5f?ClxnStyle.OnAccent:ClxnStyle.Text):ClxnStyle.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);}

 }
 internal sealed class ClxnFlowCard:FlowLayoutPanel {
  internal ClxnFlowCard(){DoubleBuffered=true;}
  protected override Point ScrollToControl(Control activeControl)=>AutoScrollPosition;
  protected override void OnMouseWheel(MouseEventArgs e){if(!AutoScroll){base.OnMouseWheel(e);return;}AutoScrollPosition=new Point(0,Math.Max(0,-AutoScrollPosition.Y-e.Delta/120*48));if(e is HandledMouseEventArgs h)h.Handled=true;}
  protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);if(Visible)ClxnMotion.Reveal(this);}
  protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<30||Height<30)return;using(var path=ClxnShape.Round(Width,Height)){var old=Region;Region=new Region(path);old?.Dispose();}}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=ClxnShape.Round(Width,Height))using(var pen=new Pen(ClxnStyle.Border))e.Graphics.DrawPath(pen,path);}
 }
 internal sealed class ClxnListBox:ListBox {
  internal ClxnListBox(){DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=22;}
  protected override void OnDrawItem(DrawItemEventArgs e){if(e.Index<0||e.Index>=Items.Count)return;bool selected=(e.State&DrawItemState.Selected)!=0;using(var b=new SolidBrush(selected?ClxnStyle.Accent:BackColor))e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,Items[e.Index].ToString(),Font,new Rectangle(e.Bounds.X+6,e.Bounds.Y,e.Bounds.Width-10,e.Bounds.Height),selected?ClxnStyle.OnAccent:ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);}
 }
 internal sealed class ClxnCheckBox:CheckBox {
  internal ClxnCheckBox(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent?.BackColor??ClxnStyle.Surface);g.SmoothingMode=SmoothingMode.AntiAlias;int y=(Height-14)/2;using(var box=ClxnShape.Round(14,14,3)){g.TranslateTransform(1,y);using(var b=new SolidBrush(Checked?ClxnStyle.Accent:ClxnStyle.Surface))g.FillPath(b,box);using(var p=new Pen(ClxnStyle.Border))g.DrawPath(p,box);if(Checked)using(var p=new Pen(ClxnStyle.OnAccent,1.7f))g.DrawLines(p,new[]{new Point(3,7),new Point(6,10),new Point(11,4)});g.ResetTransform();}TextRenderer.DrawText(g,Text,Font,new Rectangle(21,0,Math.Max(1,Width-21),Height),Enabled?ForeColor:ClxnStyle.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);}
 }
 internal sealed class ClxnSwatch:Button {
  internal Color Ink {get;set;}internal bool Selected;
  internal ClxnSwatch(Color colour){Ink=colour;Size=new Size(30,30);Margin=new Padding(0,4,8,0);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;AccessibleName="Цвет "+ColorTranslator.ToHtml(colour);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(ClxnStyle.Surface);if(Selected)for(int i=4;i>0;i--)using(var p=new Pen(Color.FromArgb(22,Ink),i*2))g.DrawEllipse(p,4,4,22,22);using(var b=new SolidBrush(Ink))g.FillEllipse(b,6,6,18,18);if(Selected)using(var p=new Pen(Ink,1.5f))g.DrawEllipse(p,1,1,27,27);}
 }
}
