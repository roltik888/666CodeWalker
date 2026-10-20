using System;
using System.Windows.Forms;
using CodeWalker.GameFiles;
using SharpDX;
namespace CodeWalker {
 public partial class WorldForm {
  readonly System.Diagnostics.Stopwatch clxnGroundProbe=System.Diagnostics.Stopwatch.StartNew();bool clxnGroundCached;Vector3 clxnGroundProbePosition;
  bool clxnWalking,clxnThirdPerson;
  string clxnWalkActorId;
  Vector3 clxnPreviousCamera,clxnPreviousRotation,clxnPreviousFeet;
  float clxnWalkFloor;
  void ClxnToggleWalk(){
   if(clxnFlightPlaying){clxnMessage="Сначала завершите пролётку.";return;}
   if(clxnWalking){ClxnLeaveWalk();return;}
   var pin=ClxnSelectedPin();if(pin==null||!pin.IsPed){clxnMessage="Сначала выберите персонажа в «Объектах» или нажмите его подпись.";return;}
   ClxnProject.Checkpoint();if(MapViewEnabled)SetCameraMode("Perspective");ClxnSetTool("move");clxnPreviousCamera=ClxnCameraPosition;clxnPreviousRotation=camera.TargetRotation;clxnWalkActorId=pin.Id;clxnWalkFloor=pin.Position.Z;clxnPreviousFeet=pin.Position;clxnThirdPerson=false;
   SetControlMode(WorldControlMode.Ped);lock(RenderSyncRoot){pedEntity.Position=pin.Position+Vector3.UnitZ*1.7f;pedEntity.CameraEntity.Position=pedEntity.Position;pedEntity.Velocity=Vector3.Zero;pedEntity.MovementSpeed=2.2f;pedEntity.BoostMultiplier=2.5f;camera.TargetDistance=.01f;camera.CurrentDistance=.01f;camera.TargetRotation=new Vector3(pin.Heading*(float)Math.PI/180,0,0);camera.CurrentRotation=camera.TargetRotation;camera.Update(0);}
   clxnGroundCached=false;clxnWalking=true;clxnHud.Visible=false;clxnRail.Visible=false;clxnOptions.Visible=false;clxnLibrary.Visible=false;clxnWardrobe.Visible=false;if(clxnEnvironment!=null)clxnEnvironment.Visible=false;clxnLogPanel.Visible=false;clxnFlightPanel.Visible=false;clxnWalkHint.Visible=true;clxnMessage="Прогулка: WASD — идти, Shift — бежать, F10 — вид со спины, Esc — вернуться.";
  }
  void ClxnLeaveWalk(){
   if(!clxnWalking)return;clxnWalking=false;SetControlMode(WorldControlMode.Free);lock(RenderSyncRoot){camera.TargetRotation=clxnPreviousRotation;ClxnCameraNavigation.Restore(camera,clxnPreviousCamera);}clxnHud.Visible=true;clxnRail.Visible=true;clxnOptions.Visible=true;clxnWalkHint.Visible=false;ClxnProject.Commit();clxnMessage="✓ Прогулка завершена. Положение персонажа сохранено в сцене.";
  }
  void ClxnWalkView(){clxnThirdPerson=!clxnThirdPerson;lock(RenderSyncRoot){camera.TargetDistance=clxnThirdPerson?3.5f:.01f;camera.CurrentDistance=camera.TargetDistance;}}
  void ClxnUpdateWalking(float elapsed){
   if(!clxnWalking)return;
   var feet=pedEntity.Position-Vector3.UnitZ*1.7f;
   if(ClxnEMSBlock(feet)){feet.X=clxnPreviousFeet.X;feet.Y=clxnPreviousFeet.Y;pedEntity.Velocity.X=0;pedEntity.Velocity.Y=0;}
   var ray=new Ray(new Vector3(feet.X,feet.Y,clxnWalkFloor+1.5f),-Vector3.UnitZ);Vector3 floor,n;
   bool reuse=clxnGroundCached&&clxnGroundProbe.ElapsedMilliseconds<50&&Vector3.DistanceSquared(new Vector3(feet.X,feet.Y,0),clxnGroundProbePosition)<.25f;
   bool found;if(reuse){floor=new Vector3(feet.X,feet.Y,clxnWalkFloor);n=Vector3.UnitZ;found=true;}else{found=ClxnVisualGround(ray,8,out floor,out n);clxnGroundProbe.Restart();clxnGroundProbePosition=new Vector3(feet.X,feet.Y,0);}
   if(!found&&space.Inited){var hit=space.RayIntersect(ray,8,collisionmeshlayers);if(hit.Hit){floor=hit.Position;found=true;}}
   clxnGroundCached=found;
   if(found){clxnWalkFloor=floor.Z;feet.Z=floor.Z+.025f;pedEntity.Position=feet+Vector3.UnitZ*1.7f;pedEntity.Velocity.Z=0;pedEntity.CameraEntity.Position=pedEntity.Position;pedEntity.OnGround=true;}
   clxnPreviousFeet=feet;
   lock(ClxnProject.Sync){var actor=ClxnProject.Pins.Find(p=>p.Id==clxnWalkActorId);if(actor!=null){actor.Position=feet;actor.Heading=(float)(Math.Atan2(camera.ViewDirection.X,camera.ViewDirection.Y)*-180/Math.PI);}}
  }
 }
}

