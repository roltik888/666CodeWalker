using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeWalker.World;
using SharpDX;
namespace CodeWalker {
 public partial class WorldForm {
  ComboBox clxnVehicleCombo; FlowLayoutPanel clxnVehicleSettings;
  readonly Dictionary<string,Task<Vehicle>> clxnVehicleLoads=new Dictionary<string,Task<Vehicle>>();
  readonly System.Threading.SemaphoreSlim clxnVehicleLoader=new System.Threading.SemaphoreSlim(2);
  readonly HashSet<string> clxnVehicleNotices=new HashSet<string>();bool clxnClearPending;
  void ClxnSpawnVehicle(Vector3 point){
   lock(ClxnProject.Sync)if(ClxnProject.Pins.Count(p=>p.IsVehicle)>=32){clxnMessage="Лимит сцены: 32 машины.";return;}
   string model=clxnVehicleCombo.SelectedIndex==0?"oracle":clxnVehicleCombo.SelectedIndex==1?"felon":clxnVehicleCombo.Text.Trim().ToLowerInvariant();
   if(!System.Text.RegularExpressions.Regex.IsMatch(model,@"^[a-z0-9_]{1,64}$")){clxnMessage="Выберите машину или введите имя установленной модели.";return;}
   var pin=ClxnProject.AddPin("Машина · "+model,point,ClxnColour);lock(ClxnProject.Sync){pin.Model="vehicle:"+model;pin.Heading=(float)clxnActorHeading.Value;}ClxnProject.Commit();clxnSelectedId=pin.Id;clxnMessage="Машина добавлена. Загружаются игровые ресурсы…";
  }
  void ClxnRenderVehicles(){
   if(!initialised||gameFileCache.VehiclesInitDict==null)return;
   ClxnPin[] cars;lock(ClxnProject.Sync)cars=ClxnProject.Pins.Where(p=>p.IsVehicle).Take(32).Select(p=>new ClxnPin{Id=p.Id,Model=p.Model,Position=p.Position,Heading=p.Heading}).ToArray();
   foreach(var car in cars){var key=car.Id+"|"+car.Model;Task<Vehicle> task;
    if(!clxnVehicleLoads.TryGetValue(key,out task)){var model=car.Model.Substring(8);task=Task.Run(async()=>{await clxnVehicleLoader.WaitAsync();try{var v=new Vehicle();v.Init(model,gameFileCache,true,45000);if(v.Yft?.Fragment?.Drawable==null){v.Init(model,gameFileCache,false,45000);if(v.Yft?.Fragment?.Drawable==null)throw new InvalidOperationException("Нет установленной модели машины: "+model);}return v;}finally{clxnVehicleLoader.Release();}});clxnVehicleLoads.Add(key,task);}
    if(task.IsFaulted){if(clxnVehicleNotices.Add(key))clxnMessage=task.Exception.GetBaseException().Message;continue;}if(!task.IsCompleted)continue;
    var vehicle=task.Result;float bottom=vehicle.Yft.Fragment.Drawable.BoundingBoxMin.Z;vehicle.Position=car.Position+new Vector3(0,0,-bottom+.025f);vehicle.Rotation=Quaternion.RotationAxis(Vector3.UnitZ,car.Heading*(float)Math.PI/180);vehicle.UpdateEntity();Renderer.RenderVehicle(vehicle);
    if(clxnVehicleNotices.Add(key))clxnMessage="✓ Машина готова. Координаты и поворот — в Объектах.";
   }
   foreach(var key in clxnVehicleLoads.Keys.Where(k=>cars.All(c=>k!=c.Id+"|"+c.Model)).ToArray())if(clxnVehicleLoads[key].IsCompleted)clxnVehicleLoads.Remove(key);
  }
  void ClxnClearScene(){
   if(clxnExporting){clxnClearPending=true;ClxnCancelExport();return;}if(clxnWalking)ClxnLeaveWalk();if(clxnFlightPlaying)ClxnStopFlight();ClxnFinishStroke();
   lock(RenderSyncRoot){ClxnProject.ClearAll();clxnSelectedId=null;clxnPreviewHit=false;lock(clxnProjectionSync)clxnProjected.Clear();}
   Markers.Clear();clxnPinName.Text=clxnPinNote.Text="";clxnPinHeading.Value=0;foreach(var coordinate in clxnXYZ)coordinate.Value=0;clxnWardrobe.Visible=false;ClxnSetTool("move");ClxnRefreshLibrary();ClxnRefreshKeys();clxnMessage="Сцена очищена: рисунки, метки, модели, одежда и кадры пролёта. Ctrl+Z — вернуть всё.";
  }
 }
}
