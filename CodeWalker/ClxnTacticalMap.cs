using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
namespace CodeWalker {
 public partial class WorldForm {
  ClxnTacticalMap clxnTactical;
  void ClxnOpenTactical(){if(clxnTactical==null||clxnTactical.IsDisposed){try{clxnTactical=new ClxnTacticalMap();clxnTactical.Show(this);}catch(Exception ex){clxnMessage="Карта не открыта: "+ex.Message;}}else{clxnTactical.Show();clxnTactical.Activate();}}
 }
 internal sealed class ClxnTacticalMap:Form {
  readonly MapCanvas map; readonly Label info;
  internal ClxnTacticalMap(){Text="666clxn · Карты Majestic";Icon=ClxnBrand.Icon;Size=new Size(1040,780);MinimumSize=new Size(640,480);StartPosition=FormStartPosition.CenterParent;BackColor=ClxnStyle.Background;ForeColor=ClxnStyle.Text;
   var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=64,Padding=new Padding(10),WrapContents=false,AutoScroll=true};Controls.Add(top);
   var select=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=235,Margin=new Padding(3,8,8,3)};select.Items.AddRange(new object[]{"Обычная карта","Зоны MCL · Majestic","Капт-карта · Majestic"});top.Controls.Add(select);
   top.Controls.Add(ClxnStyle.Button("Вся карта",(s,e)=>map.Fit(),100));top.Controls.Add(ClxnStyle.Button("В 3D-мир",(s,e)=>Hide(),110));
   info=new Label{Dock=DockStyle.Bottom,Height=48,Padding=new Padding(12,7,8,0),ForeColor=ClxnStyle.Muted,Text="Колесо — масштаб · Зажмите ЛКМ — перемещение · Наведите курсор на зону для названия\nMCL: majestic.wiki · Капты: mitsuki-hub.ru/spots · Привязка по изображениям"};Controls.Add(info);
   map=new MapCanvas(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"MapLayers"));var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Margin=Padding.Empty,Padding=Padding.Empty};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,64));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,60));Controls.Add(layout);top.Dock=DockStyle.Fill;info.Dock=DockStyle.Fill;map.Dock=DockStyle.Fill;map.Margin=Padding.Empty;layout.Controls.Add(top,0,0);layout.Controls.Add(map,0,1);layout.Controls.Add(info,0,2);layout.BringToFront();map.HoverName+=name=>{info.Text=string.IsNullOrEmpty(name)?"Колесо — масштаб · ЛКМ — перемещение · Двойной щелчок — вся карта\nMCL: majestic.wiki · Капты: mitsuki-hub.ru/spots · Привязка по изображениям":name+"\nКоординаты GTA V · MCL: majestic.wiki · Капты: Mitsuki Hub";};select.SelectedIndexChanged+=(s,e)=>{map.Mode=select.SelectedIndex;map.Invalidate();};select.SelectedIndex=0;Shown+=(s,e)=>map.Fit();
  }
  sealed class MapCanvas:Control {
   readonly Image atlas;readonly XElement[] zones,points;float scale=1;bool fitted;Size lastSize;PointF offset;Point drag;bool dragging;internal int Mode;internal event Action<string> HoverName;
   static float F(XAttribute a)=>float.Parse(a.Value,CultureInfo.InvariantCulture);
   static PointF World(float x,float y)=>new PointF((2731.5f+.66f*x)/4,(5521.5f-.66f*y)/4);
   PointF ScreenPoint(PointF p)=>new PointF(offset.X+p.X*scale,offset.Y+p.Y*scale);
   PointF[] Polygon(XElement z)=>z.Elements("p").Select(p=>ScreenPoint(World(F(p.Attribute("x")),F(p.Attribute("y"))))).ToArray();
   bool Active(XElement z)=>Mode>0&&(string)z.Attribute("mode")== (Mode==1?"mcl":"capt");
   internal MapCanvas(string path){DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);atlas=Image.FromFile(Path.Combine(path,"atlas.jpg"));var root=XElement.Load(Path.Combine(path,"zones.xml"));zones=root.Elements("zone").ToArray();points=root.Elements("point").ToArray();BackColor=Color.FromArgb(15,35,48);MouseWheel+=Wheel;MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){drag=e.Location;dragging=true;Capture=true;}};MouseUp+=(s,e)=>{dragging=false;Capture=false;};MouseMove+=MapMouseMove;MouseDoubleClick+=(s,e)=>Fit();MouseEnter+=(s,e)=>Focus();}
   protected override void OnResize(EventArgs e){base.OnResize(e);if(!fitted){Fit();return;}offset.X+=(Width-lastSize.Width)/2f;offset.Y+=(Height-lastSize.Height)/2f;lastSize=Size;Invalidate();}
   internal void Fit(){if(atlas==null||Width<1||Height<1)return;fitted=true;lastSize=Size;scale=Math.Min((float)Width/atlas.Width,(float)Height/atlas.Height);offset=new PointF((Width-atlas.Width*scale)/2,(Height-atlas.Height*scale)/2);Invalidate();}
   void Wheel(object s,MouseEventArgs e){float next=Math.Max(.08f,Math.Min(12,scale*(e.Delta>0?1.2f:1/1.2f)));offset=new PointF(e.X-(e.X-offset.X)*next/scale,e.Y-(e.Y-offset.Y)*next/scale);scale=next;Invalidate();}
   void MapMouseMove(object s,MouseEventArgs e){if(dragging){offset.X+=e.X-drag.X;offset.Y+=e.Y-drag.Y;drag=e.Location;Invalidate();return;}string name="";foreach(var z in zones.Where(Active)){var p=Polygon(z);if(p.Length<3)continue;using(var path=new GraphicsPath()){path.AddPolygon(p);if(path.IsVisible(e.Location))name=(string)z.Attribute("name");}}foreach(var p in points.Where(p=>Active(p)&&((string)p.Attribute("kind")!="callout"||scale>.9f))){var q=ScreenPoint(World(F(p.Attribute("x")),F(p.Attribute("y"))));if(Math.Abs(q.X-e.X)<12&&Math.Abs(q.Y-e.Y)<12)name=(string)p.Attribute("name")+" · "+p.Attribute("x").Value+", "+p.Attribute("y").Value;}HoverName?.Invoke(name);}
   protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.DrawImage(atlas,offset.X,offset.Y,atlas.Width*scale,atlas.Height*scale);foreach(var z in zones.Where(Active)){var p=Polygon(z);if(p.Length<3)continue;var c=ColorTranslator.FromHtml((string)z.Attribute("color"));using(var fill=new SolidBrush(Color.FromArgb(55,c)))using(var edge=new Pen(c,2)){g.FillPolygon(fill,p);g.DrawPolygon(edge,p);}if(scale>.65f){var center=new PointF(p.Average(q=>q.X),p.Average(q=>q.Y));TextRenderer.DrawText(g,(string)z.Attribute("name"),Font,new Point((int)center.X,(int)center.Y),Color.White,Color.FromArgb(30,30,30));}}
    foreach(var p in points.Where(p=>Active(p)&&((string)p.Attribute("kind")!="callout"||scale>.9f))){var q=ScreenPoint(World(F(p.Attribute("x")),F(p.Attribute("y"))));g.FillEllipse(Brushes.MediumPurple,q.X-6,q.Y-6,12,12);g.DrawEllipse(Pens.White,q.X-6,q.Y-6,12,12);if(scale>.9f)TextRenderer.DrawText(g,(string)p.Attribute("name"),Font,new Point((int)q.X+9,(int)q.Y-8),Color.White,Color.FromArgb(30,30,30));}}
   protected override void Dispose(bool disposing){if(disposing)atlas.Dispose();base.Dispose(disposing);}
  }
 }
}
