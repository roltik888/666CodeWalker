using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeWalker.World;
using SharpDX;
using Color=System.Drawing.Color;

namespace CodeWalker {
 public partial class WorldForm {
  ComboBox clxnPedCombo;
  NumericUpDown clxnActorHeading,clxnPinHeading;
  Label clxnColourHex;
  readonly List<ClxnSwatch> clxnSwatches=new List<ClxnSwatch>();
  readonly System.Threading.SemaphoreSlim clxnPedLoader=new System.Threading.SemaphoreSlim(2);
  readonly Dictionary<string,Task<Ped>> clxnActorLoads=new Dictionary<string,Task<Ped>>();
  readonly HashSet<string> clxnActorNotices=new HashSet<string>();
  static readonly string[] ClxnPedModels={"clxn:roltik","mp_m_freemode_01","s_m_m_paramedic_01","s_m_y_cop_01","player_zero","player_one"};
  void ClxnSelectColour(Color c) {
   ClxnFinishStroke();ClxnColour=Color.FromArgb(255,c.R,c.G,c.B).ToArgb();
   if(clxnColourHex!=null){clxnColourHex.Text="#"+c.R.ToString("X2")+c.G.ToString("X2")+c.B.ToString("X2");clxnColourHex.ForeColor=c;}
   foreach(var swatch in clxnSwatches){swatch.Selected=swatch.Ink.ToArgb()==ClxnColour;swatch.Invalidate();}
   clxnMessage="Цвет кисти: "+(clxnColourHex?.Text??"")+". Новые штрихи используют этот цвет.";
  }
  void ClxnSpawnActor(Vector3 point) {
   lock(ClxnProject.Sync)if(ClxnProject.Pins.Count(p=>p.IsPed)>=64){clxnMessage="В сцене уже 64 персонажа. Удалите лишних в списке объектов.";return;}
   var index=clxnPedCombo.SelectedIndex;var chosen=index>=0?ClxnPedModels[index]:clxnPedCombo.Text.Trim();if(!System.Text.RegularExpressions.Regex.IsMatch(chosen,@"^[a-zA-Z0-9_:.-]{1,80}$")){clxnMessage="Выберите образ из списка или введите имя установленной модели, например player_zero.";return;}var pin=ClxnProject.AddPin(chosen=="clxn:roltik"?"Roltik":"Персонаж · "+clxnPedCombo.Text,point,ClxnColour);
   lock(ClxnProject.Sync){pin.Model=chosen;pin.Heading=(float)clxnActorHeading.Value;}
   ClxnProject.Commit();clxnSelectedId=pin.Id;clxnMessage="Персонаж добавлен. Модель загружается…";ClxnRefreshLibrary();
  }
  void ClxnRenderActors() {
   if(!initialised || gameFileCache.PedsInitDict==null)return;
   ClxnPin[] actors;lock(ClxnProject.Sync)actors=ClxnProject.Pins.Where(p=>p.IsPed).Select(p=>new ClxnPin { Id=p.Id,Model=p.Model,Pose=clxnWalking&&p.Id==clxnWalkActorId?"stand":p.Pose,Position=p.Position,Heading=p.Heading,Outfit=p.Outfit.Select(o=>o.Copy()).ToList() }).Take(64).ToArray();
   var activeKeys=actors.ToDictionary(a=>a.Id,a=>a.Id+"|"+a.AppearanceKey);
   foreach(var actor in actors){
    string key=activeKeys[actor.Id];Task<Ped> task;
    if(!clxnActorLoads.TryGetValue(key,out task)){
     var model=actor.Model;task=Task.Run(async ()=>{await clxnPedLoader.WaitAsync();try{var ped=new Ped();ped.Init(actor.IsRoltik?"mp_m_freemode_01":model,gameFileCache);if(ped.InitData==null)throw new InvalidOperationException("Модель "+model+" отсутствует в игровых файлах.");ped.LoadDefaultComponents(gameFileCache);ClxnRelaxedPose(ped);ClxnApplyPose(ped,actor.Pose);if(actor.IsRoltik)ClxnApplyRoltikLook(ped);var clothingError=ClxnApplyOutfit(ped,actor.Outfit);if(clothingError!=null)clxnOutfitErrors[key]=clothingError;if(ped.Drawables.All(d=>d==null))throw new InvalidOperationException("Нет геометрии модели "+model);return ped;}finally{clxnPedLoader.Release();}});clxnActorLoads.Add(key,task);
    }
    if(task.IsFaulted){if(clxnActorNotices.Add(key))clxnMessage=task.Exception.GetBaseException().Message;continue;}
    if(!task.IsCompleted){var previous=clxnActorLoads.Where(k=>k.Key.StartsWith(actor.Id+"|",StringComparison.Ordinal)&&k.Value.Status==TaskStatus.RanToCompletion).Select(k=>k.Value.Result).FirstOrDefault();if(previous!=null)Renderer.RenderPed(previous);continue;}
    if(clxnActorNotices.Add(key))clxnMessage="✓ Персонаж готов. Положение и поворот сохраняются в сцене.";
    if(clxnWalking&&actor.Id==clxnWalkActorId&&!clxnThirdPerson)continue;var loaded=task.Result;ClxnUpdatePoseClock(loaded,actor.Pose);var low=ClxnModelFoot(loaded);loaded.Position=new Vector3(actor.Position.X,actor.Position.Y,actor.Pose=="sit"?actor.Position.Z-Math.Max(-2.5f,Math.Min(2.5f,low))+.025f:ClxnGroundMath.RootHeight(actor.Position.Z,low));loaded.Rotation=Quaternion.RotationAxis(Vector3.UnitZ,actor.Heading*(float)Math.PI/180);loaded.UpdateEntity();Renderer.RenderPed(loaded);
   }
   foreach(var key in clxnActorLoads.Keys.Where(k=>!activeKeys.Values.Contains(k)).ToArray())if(clxnActorLoads[key].IsCompleted&&actors.All(a=>!key.StartsWith(a.Id+"|",StringComparison.Ordinal)||clxnActorLoads.TryGetValue(activeKeys[a.Id],out var active)&&active.IsCompleted)){clxnActorLoads.Remove(key);clxnOutfitErrors.TryRemove(key,out _);clxnActorNotices.Remove(key);}
  }
  void ClxnImportMap() {
   using(var d=new OpenFileDialog { Title="Подключить проект карты с вашими ресурсами",Filter="Проект карты CodeWalker|*.cwproj" })if(d.ShowDialog(this)==DialogResult.OK)try{ShowProjectForm();ProjectForm.OpenProject(d.FileName);ClxnProject.MapProjectPath=d.FileName;ClxnProject.Commit();clxnMessage="Подключена внешняя карта: "+Path.GetFileName(d.FileName);}catch(Exception ex){clxnMessage="Ресурсы карты не подключены: "+ex.Message;}
  }
  void ClxnRestoreSceneView() {
   ClxnApplyEnvironment();
   if(ClxnProject.ViewPosition.HasValue){lock(RenderSyncRoot){camera.TargetRotation=ClxnProject.ViewRotation;ClxnCameraNavigation.Restore(camera,ClxnProject.ViewPosition.Value);}}
   if(!string.IsNullOrEmpty(ClxnProject.MapProjectPath)){
    if(File.Exists(ClxnProject.MapProjectPath)){ShowProjectForm();ProjectForm.OpenProject(ClxnProject.MapProjectPath);}
    else clxnMessage="Проект открыт. Пакет карты не найден: "+ClxnProject.MapProjectPath;
   }
  }
 }
}


