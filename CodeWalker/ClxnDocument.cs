using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SharpDX;

namespace CodeWalker
{
    internal sealed class ClxnPin
    {
        internal string Id = Guid.NewGuid().ToString("N");
        internal string Name = "Новая точка", Note = "";
        internal string Model = "";
        internal string Pose="stand";
        internal bool IsRoltik => Model=="clxn:roltik"||Model=="clxn:white"||Model=="clxn:white-blue";
        internal bool IsVehicle => Model.StartsWith("vehicle:",StringComparison.Ordinal);
        internal bool IsPed => !string.IsNullOrEmpty(Model)&&Model!="clxn:ems"&&!IsVehicle;
        internal List<ClxnClothingPart> Outfit=new List<ClxnClothingPart>();
        internal string AppearanceKey => Model+"|"+Pose+"|"+string.Join(";",Outfit.Select(p=>p.ToXml().ToString()));
        internal float Heading;
        internal Vector3 Position;
        internal int Colour = unchecked((int)0xffa795ff);
        public override string ToString() => Name;
    }
    internal sealed class ClxnClothingPart {
        internal int Component,Drawable,Texture,Alternative;
        internal string DrawablePath="",TexturePath="";
        internal XElement ToXml()=>new XElement("part",new XAttribute("component",Component),new XAttribute("drawable",Drawable),new XAttribute("texture",Texture),new XAttribute("alt",Alternative),new XAttribute("ydd",DrawablePath),new XAttribute("ytd",TexturePath));
        internal ClxnClothingPart Copy()=>(ClxnClothingPart)MemberwiseClone();
        internal static List<ClxnClothingPart> Read(XElement e){var parts=(e?.Elements("part")??Enumerable.Empty<XElement>()).Select(p=>new ClxnClothingPart{Component=(int)p.Attribute("component"),Drawable=(int)p.Attribute("drawable"),Texture=(int)p.Attribute("texture"),Alternative=(int?)p.Attribute("alt")??0,DrawablePath=(string)p.Attribute("ydd")??"",TexturePath=(string)p.Attribute("ytd")??""}).ToList();if(parts.Count>12||parts.Select(p=>p.Component).Distinct().Count()!=parts.Count||parts.Any(p=>p.Component<0||p.Component>11||p.Drawable<0||p.Drawable>10000||p.Texture<0||p.Texture>10000||p.Alternative<0||p.Alternative>255||p.DrawablePath.Length>4096||p.TexturePath.Length>4096))throw new FormatException("Неверные параметры одежды.");return parts;}
    }
    internal sealed class ClxnStroke
    {
        internal int Colour;
        internal float Width = 4;
        internal List<Vector3> Points = new List<Vector3>();
    }
    internal sealed class ClxnDocument
    {
        internal readonly List<ClxnPin> Pins = new List<ClxnPin>();
        internal readonly List<ClxnStroke> Strokes = new List<ClxnStroke>();
        internal readonly List<ClxnCameraKey> CameraKeys=new List<ClxnCameraKey>();
        internal bool SmoothFlight=true;
        internal string ZoneMode="none";
        internal string SkyWeather="",SkyCloud="";internal float SkyHour=12;internal bool SkyVisible=true,SkyClouds=true,SkyGray=false;
        internal readonly object Sync = new object();
        internal event Action Changed;
        internal string SavePath = Path.Combine(ClxnHomeForm.DataFolder, "workspace.clxn");
        internal string LastError;
        internal Vector3? ViewPosition;
        internal Vector3 ViewRotation;
        internal string MapProjectPath = "";
        string trackedPath, lastDisk;
        readonly Stack<string> undo = new Stack<string>(), redo = new Stack<string>();
        internal static string F(float f) => f.ToString("0.###", CultureInfo.InvariantCulture);
        internal static string Coords(Vector3 v) => F(v.X) + ", " + F(v.Y) + ", " + F(v.Z);
        static XElement V(Vector3 v) => new XElement("v", new XAttribute("x", F(v.X)), new XAttribute("y", F(v.Y)), new XAttribute("z", F(v.Z)));
        static Vector3 ReadV(XElement e)
        {
            var v = new Vector3((float)e.Attribute("x"), (float)e.Attribute("y"), (float)e.Attribute("z"));
            if (float.IsNaN(v.X) || float.IsInfinity(v.X) || float.IsNaN(v.Y) || float.IsInfinity(v.Y) || float.IsNaN(v.Z) || float.IsInfinity(v.Z) || Math.Abs(v.X)>1000000 || Math.Abs(v.Y)>1000000 || Math.Abs(v.Z)>1000000) throw new FormatException("Неверные координаты.");
            return v;
        }
        internal string Serialize()
        {
            lock (Sync) return new XDocument(new XElement("clxn", new XAttribute("version", 1),
                new XElement("mapProject",MapProjectPath),
                new XElement("zoneMode",ZoneMode),
                new XElement("environment",new XAttribute("weather",SkyWeather),new XAttribute("cloud",SkyCloud),new XAttribute("hour",F(SkyHour)),new XAttribute("sky",SkyVisible),new XAttribute("clouds",SkyClouds),new XAttribute("gray",SkyGray)),
                new XElement("flight",new XAttribute("smooth",SmoothFlight),CameraKeys.Select(k=>new XElement("key",new XAttribute("name",k.Name),new XAttribute("seconds",F(k.Seconds)),new XAttribute("fov",F(k.Fov)),V(k.Position),new XElement("rotation",new XAttribute("x",F(k.Rotation.X)),new XAttribute("y",F(k.Rotation.Y)),new XAttribute("z",F(k.Rotation.Z)))))),
                ViewPosition.HasValue?new XElement("view",V(ViewPosition.Value),new XElement("rotation",new XAttribute("x",F(ViewRotation.X)),new XAttribute("y",F(ViewRotation.Y)),new XAttribute("z",F(ViewRotation.Z)))):null,
                new XElement("pins", Pins.Select(p => new XElement("pin", new XAttribute("id", p.Id), new XAttribute("name", p.Name), new XAttribute("colour", p.Colour), new XAttribute("model",p.Model),new XAttribute("pose",p.Pose),new XAttribute("heading",F(p.Heading)),new XElement("note", p.Note),new XElement("outfit",p.Outfit.Select(o=>o.ToXml())), V(p.Position)))),
                new XElement("strokes", Strokes.Select(s => new XElement("stroke", new XAttribute("colour", s.Colour), new XAttribute("width", F(s.Width)), s.Points.Select(V)))))).ToString();
        }
        internal void Deserialize(string xml)
        {
            var root = XDocument.Parse(xml).Root;
            if (root.Name != "clxn" || (int?)root.Attribute("version") != 1) throw new FormatException("Неизвестный формат проекта 666clxn.");
            var pins = root.Element("pins").Elements("pin").Select(p=>new ClxnPin { Id=(string)p.Attribute("id"), Name=(string)p.Attribute("name") ?? "Точка", Note=(string)p.Element("note") ?? "", Model=(string)p.Attribute("model")??"",Pose=(string)p.Attribute("pose")??"stand",Heading=(float?)p.Attribute("heading")??0,Colour=(int)p.Attribute("colour"), Position=ReadV(p.Element("v")),Outfit=ClxnClothingPart.Read(p.Element("outfit")) }).ToList();
            foreach(var pin in pins)if(pin.IsRoltik&&(pin.Name=="666 · белый / blackwork"||pin.Name=="666clxn · белый образ"||pin.Name=="Персонаж · 666 · белый / blackwork"||pin.Name=="Персонаж · 666clxn · белый образ"))pin.Name="Roltik";
            if(pins.Any(p=>string.IsNullOrEmpty(p.Id))||pins.Select(p=>p.Id).Distinct().Count()!=pins.Count)throw new FormatException("Повторяющиеся или пустые ID объектов.");
            if(pins.Any(p=>p.Pose!="stand"&&p.Pose!="sit"&&p.Pose!="dance"))throw new FormatException("Неизвестная поза персонажа.");
            if(pins.Any(p=>float.IsNaN(p.Heading)||float.IsInfinity(p.Heading)))throw new FormatException("Неверный поворот персонажа.");
            Vector3? view=root.Element("view")!=null?(Vector3?)ReadV(root.Element("view").Element("v")):null;
            var rotation=root.Element("view")?.Element("rotation");var viewRotation=rotation!=null?ReadV(rotation):Vector3.Zero;
            var strokes = root.Element("strokes").Elements("stroke").Select(s=>new ClxnStroke { Colour=(int)s.Attribute("colour"), Width=Math.Max(1,Math.Min(20,(float?)s.Attribute("width")??4)), Points=s.Elements("v").Select(ReadV).ToList() }).ToList();
            if(strokes.Any(s=>float.IsNaN(s.Width)))throw new FormatException("Неверная толщина линии.");
            if (pins.Count > 10000 || strokes.Sum(s=>s.Points.Count)>200000) throw new FormatException("Проект слишком большой.");
            var zoneMode=(string)root.Element("zoneMode")??"none";if(zoneMode!="none"&&zoneMode!="mcl"&&zoneMode!="capt")throw new FormatException("Неизвестный режим зон.");
            var env=root.Element("environment");var skyGray=(bool?)env?.Attribute("gray")??false;var skyVisible=(bool?)env?.Attribute("sky")??true;var skyClouds=(bool?)env?.Attribute("clouds")??true;var skyHour=(float?)env?.Attribute("hour")??12;var skyWeather=(string)env?.Attribute("weather")??"";var skyCloud=(string)env?.Attribute("cloud")??"";if(float.IsNaN(skyHour)||float.IsInfinity(skyHour)||skyHour<0||skyHour>=24||skyWeather.Length>80||skyCloud.Length>120)throw new FormatException("Неверные параметры неба.");
            var flight=root.Element("flight");var keys=(flight?.Elements("key")??Enumerable.Empty<XElement>()).Select(k=>new ClxnCameraKey{Name=(string)k.Attribute("name")??"Кадр",Position=ReadV(k.Element("v")),Rotation=ReadV(k.Element("rotation")),Seconds=(float?)k.Attribute("seconds")??4,Fov=(float?)k.Attribute("fov")??1}).ToList();
            if(keys.Count>500||keys.Any(k=>float.IsNaN(k.Seconds)||float.IsInfinity(k.Seconds)||k.Seconds<.25f||k.Seconds>600||float.IsNaN(k.Fov)||float.IsInfinity(k.Fov)||k.Fov<.2f||k.Fov>2.5f))throw new FormatException("Неверные параметры пролётки.");
            lock(Sync) { ZoneMode=zoneMode;SkyWeather=skyWeather;SkyCloud=skyCloud;SkyHour=skyHour;SkyGray=skyGray;SkyVisible=skyVisible;SkyClouds=skyClouds;CameraKeys.Clear();CameraKeys.AddRange(keys);SmoothFlight=(bool?)flight?.Attribute("smooth")??true;Pins.Clear(); Pins.AddRange(pins); Strokes.Clear(); Strokes.AddRange(strokes);ViewPosition=view;ViewRotation=viewRotation;MapProjectPath=(string)root.Element("mapProject")??""; }
        }
        internal void LoadDefault()
        {
            try {
                if (File.Exists(SavePath)) Deserialize(File.ReadAllText(SavePath));
                else {
                    var legacy = Path.Combine(ClxnHomeForm.DataFolder, "locations.xml");
                    if (File.Exists(legacy)) Pins.AddRange(XDocument.Load(legacy).Root.Elements("point").Select(p=>new ClxnPin { Name=(string)p.Attribute("name"), Position=ReadV(p) }).ToList());
                }
                TrackPath(SavePath);
            } catch (Exception ex) { LastError = "Проект не загружен: " + ex.Message; SavePath=Path.Combine(ClxnHomeForm.DataFolder,"recovery-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".clxn"); }
        }
        internal void TrackPath(string path) { trackedPath=Path.GetFullPath(path);lastDisk=File.Exists(path)?File.ReadAllText(path):null; }
        internal void Checkpoint() { lock(Sync) { undo.Push(Serialize()); redo.Clear(); } }
        internal void Commit()
        {
            try { Save(SavePath); LastError = null; } catch(Exception ex) { LastError = "Ошибка сохранения: " + ex.Message; }
            Changed?.Invoke();
        }
        internal void Save(string path)
        {
            if(string.Equals(trackedPath,Path.GetFullPath(path),StringComparison.OrdinalIgnoreCase) && (File.Exists(path)?File.ReadAllText(path):null)!=lastDisk)
                throw new IOException("Файл изменён в другом окне. Сохраните проект под новым именем или откройте файл заново.");
            var dir = Path.GetDirectoryName(Path.GetFullPath(path)); Directory.CreateDirectory(dir);
            var tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(tmp, Serialize());
            if (File.Exists(path)) File.Replace(tmp, path, path + ".bak"); else File.Move(tmp,path);
            TrackPath(path);
        }
        internal void Undo(bool forward=false)
        {
            lock(Sync) {
                var from = forward ? redo : undo; var to = forward ? undo : redo;
                if (from.Count==0) return; to.Push(Serialize()); Deserialize(from.Pop());
            }
            Commit();
        }
        internal void ClearAll(){Checkpoint();lock(Sync){Pins.Clear();Strokes.Clear();CameraKeys.Clear();ViewPosition=null;ViewRotation=Vector3.Zero;SmoothFlight=true;}Commit();}
        internal ClxnPin AddPin(string name, Vector3 position, int colour)
        {
            Checkpoint(); var p = new ClxnPin { Name=name, Position=position, Colour=colour };
            lock(Sync) Pins.Add(p); Commit(); return p;
        }
    }
}
