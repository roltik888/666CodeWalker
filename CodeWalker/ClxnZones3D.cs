using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SharpDX;
using Point=System.Drawing.Point;
namespace CodeWalker {
 public partial class WorldForm {
  sealed class ClxnZone3D {internal string Name,Mode;internal bool IsCallout;internal int Colour;internal Vector3[] Boundary;internal Vector3 Center;}
  readonly List<ClxnZone3D> clxnZones3D=new List<ClxnZone3D>();
  readonly Dictionary<Vector2,float> clxnZoneHeights=new Dictionary<Vector2,float>();
  readonly Dictionary<Vector2,DateTime> clxnZoneRetries=new Dictionary<Vector2,DateTime>();
  int clxnGroundBudget;string clxnZoneLoadedMode;
  readonly List<ClxnProjectedPin> clxnZoneProjected=new List<ClxnProjectedPin>();
  void ClxnBuildZonesMenu(){
   var community=new ToolStripMenuItem("666clxn · Сообщество");community.DropDownItems.Add("Discord клана ↗",null,(s,e)=>ClxnLinks.Open(this,ClxnLinks.Discord));community.DropDownItems.Add("YouTube создателя · Roltik ↗",null,(s,e)=>ClxnLinks.Open(this,ClxnLinks.YouTube));clxnMore.Items.Add(community);
   var menu=new ToolStripMenuItem("Зоны в 3D-мире");clxnMore.Items.Insert(0,menu);
   foreach(var pair in new[]{new[]{"Обычный вид","none"},new[]{"MCL · границы и позиции","mcl"},new[]{"Капты · площадки и границы","capt"}}){var mode=pair[1];var item=new ToolStripMenuItem(pair[0]){CheckOnClick=false,Tag=mode};item.Click+=(s,e)=>{ClxnFinishStroke();ClxnProject.Checkpoint();lock(RenderSyncRoot){ClxnProject.ZoneMode=mode;clxnZoneLoadedMode=null;}ClxnProject.Commit();foreach(var i in menu.DropDownItems.OfType<ToolStripMenuItem>())i.Checked=i==item;clxnMessage=mode=="none"?"Обычный 3D-мир: зоны скрыты.":"Зоны "+(mode=="mcl"?"MCL":"каптов")+" включены в 3D. Границы уточняют высоту по загруженным коллизиям; названия позиций — из справочника.";};menu.DropDownItems.Add(item);}
   menu.DropDownOpening+=(s,e)=>{foreach(var item in menu.DropDownItems.OfType<ToolStripMenuItem>())item.Checked=(string)item.Tag==ClxnProject.ZoneMode;};
   menu.DropDownItems.Add(new ToolStripSeparator());menu.DropDownItems.Add("Плоская тактическая карта…",null,(s,e)=>ClxnOpenTactical());
  }
  float ClxnZoneGround(Vector3 p){var key=new Vector2(p.X,p.Y);float z;if(clxnZoneHeights.TryGetValue(key,out z))return z;
   DateTime retry;if(clxnZoneRetries.TryGetValue(key,out retry)&&retry>DateTime.UtcNow)return 45;
   if(Vector2.Distance(key,new Vector2(camera.Position.X,camera.Position.Y))<3000&&clxnGroundBudget>0&&space.Inited&&space.BoundsStore!=null){clxnGroundBudget--;var hit=space.RayIntersect(new Ray(new Vector3(p.X,p.Y,1000),-Vector3.UnitZ),1500,collisionmeshlayers);if(hit.Hit){z=hit.Position.Z+.3f;clxnZoneHeights[key]=z;return z;}clxnZoneRetries[key]=DateTime.UtcNow.AddSeconds(2);}
   return 45;
  }
  void ClxnLoadZones3D(){
   if(clxnZoneLoadedMode==ClxnProject.ZoneMode)return;clxnZones3D.Clear();clxnZoneHeights.Clear();clxnZoneRetries.Clear();clxnZoneLoadedMode=ClxnProject.ZoneMode;if(clxnZoneLoadedMode=="none")return;
   try {var root=XElement.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"MapLayers","zones.xml"));Func<XElement,Vector3> pos=e=>new Vector3(float.Parse(e.Attribute("x").Value,CultureInfo.InvariantCulture),float.Parse(e.Attribute("y").Value,CultureInfo.InvariantCulture),0);
    foreach(var z in root.Elements().Where(e=>(string)e.Attribute("mode")==clxnZoneLoadedMode)){
     int color=ColorTranslator.FromHtml((string)z.Attribute("color")).ToArgb();var boundary=new List<Vector3>();Vector3 center;
     if(z.Name.LocalName=="point"){center=pos(z);}
     else {var raw=z.Elements("p").Select(pos).ToArray();if(raw.Length<3)continue;center=new Vector3(raw.Average(p=>p.X),raw.Average(p=>p.Y),0);for(int i=0;i<raw.Length;i++){var a=raw[i];var b=raw[(i+1)%raw.Length];int n=Math.Max(1,(int)Math.Ceiling(Vector3.Distance(a,b)/25));for(int j=0;j<n;j++)boundary.Add(Vector3.Lerp(a,b,(float)j/n));}}
     clxnZones3D.Add(new ClxnZone3D{Name=(string)z.Attribute("name")+((string)z.Attribute("kind")=="callout"?" · "+(string)z.Attribute("area"):""),IsCallout=(string)z.Attribute("kind")=="callout",Mode=clxnZoneLoadedMode,Colour=color,Boundary=boundary.ToArray(),Center=center});
    }
   }catch(Exception ex){clxnMessage="Зоны 3D не загружены: "+ex.Message;}
  }
  void ClxnRenderZones3D(Action<Vector3,Vector3,int,float> line){
   ClxnLoadZones3D();clxnGroundBudget=24;clxnZoneProjected.Clear();
   foreach(var zone in clxnZones3D){if(Vector2.Distance(new Vector2(camera.Position.X,camera.Position.Y),new Vector2(zone.Center.X,zone.Center.Y))>9000)continue;
    for(int i=0;i<zone.Boundary.Length;i++){var a=zone.Boundary[i];var b=zone.Boundary[(i+1)%zone.Boundary.Length];a.Z=ClxnZoneGround(a);b.Z=ClxnZoneGround(b);line(a,b,zone.Colour,3);}
    var anchor=zone.Center;anchor.Z=ClxnZoneGround(anchor)+8;
    if(zone.Boundary.Length==0&&!zone.IsCallout){var bottom=anchor-new Vector3(0,0,8);line(bottom,anchor,zone.Colour,3);float r=8;for(int i=0;i<24;i++){float a=i*(float)Math.PI/12,b=(i+1)*(float)Math.PI/12;line(bottom+new Vector3((float)Math.Cos(a)*r,(float)Math.Sin(a)*r,0),bottom+new Vector3((float)Math.Cos(b)*r,(float)Math.Sin(b)*r,0),zone.Colour,3);}}
    if(!ClxnVisibility.LabelWithinRange(camera.Position,anchor))continue;var rel=anchor-camera.Position;if(Vector3.Dot(rel,camera.ViewDirection)<=0)continue;var sp=camera.ViewProjMatrix.MultiplyW(rel);if(sp.Z<=0||Math.Abs(sp.X)>1||Math.Abs(sp.Y)>1)continue;
    clxnZoneProjected.Add(new ClxnProjectedPin{Id="zone:"+zone.Name,Text=zone.Name,Point=new Point((int)((sp.X*.5f+.5f)*camera.Width),(int)((-.5f*sp.Y+.5f)*camera.Height)),Colour=zone.Colour,Distance=rel.Length()});
   }
  }
 }
}
