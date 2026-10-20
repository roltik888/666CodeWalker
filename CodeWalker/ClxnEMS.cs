using System;
using System.Collections.Generic;
using System.Linq;
using CodeWalker.Rendering;
using SharpDX;
namespace CodeWalker {
 public partial class WorldForm {
  void ClxnPlaceEMS(Vector3 p){var pin=ClxnProject.AddPin("EMS · реконструкция 666clxn",p,unchecked((int)0xffff6891));pin.Model="clxn:ems";pin.Heading=(float)clxnActorHeading.Value;ClxnProject.Commit();clxnSelectedId=pin.Id;ClxnRefreshLibrary();clxnMessage="EMS-концепт поставлен. Это приблизительная реконструкция, не ресурс Majestic.";}
  void ClxnStartEMS(){ClxnSetTool("ems");ClxnNavigate(new Vector3(1830,3660,64));lock(RenderSyncRoot){camera.TargetRotation=new Vector3(0,.7f,0);camera.CurrentRotation=camera.TargetRotation;}clxnMessage="Sandy Shores: выберите свободную площадку и щёлкните ЛКМ для EMS-концепта.";}
  void ClxnEMSGeometry(List<VertexTypePC> output,ClxnPin pin){
   var q=Quaternion.RotationAxis(Vector3.UnitZ,pin.Heading*(float)Math.PI/180);Action<Vector3,Vector3,uint> box=(c,s,col)=>ClxnBox(output,pin.Position,q,c,s,col);
   uint white=ClxnSceneColour(214,223,229),red=ClxnSceneColour(202,47,79),glass=ClxnSceneColour(39,87,109),dark=ClxnSceneColour(49,57,66),concrete=ClxnSceneColour(130,138,144);
   box(new Vector3(0,0,.15f),new Vector3(19,14,.3f),concrete);box(new Vector3(0,2,2.2f),new Vector3(16,7,3.8f),white);box(new Vector3(0,2,4.2f),new Vector3(16.5f,7.5f,.25f),dark);
   box(new Vector3(0,-1.55f,1.45f),new Vector3(2.1f,.11f,2.5f),glass);box(new Vector3(0,-3,3.45f),new Vector3(6,3.7f,.3f),white);
   box(new Vector3(-2.7f,-4.5f,1.8f),new Vector3(.18f,.18f,3.4f),dark);box(new Vector3(2.7f,-4.5f,1.8f),new Vector3(.18f,.18f,3.4f),dark);
   box(new Vector3(-5,-1.56f,2),new Vector3(4,.12f,1.5f),glass);box(new Vector3(5,-1.56f,2),new Vector3(4,.12f,1.5f),glass);
   box(new Vector3(0,-1.58f,.85f),new Vector3(16,.15f,.22f),red);box(new Vector3(0,-1.6f,3.3f),new Vector3(16,.15f,.25f),red);
   box(new Vector3(-6.7f,-1.7f,2.9f),new Vector3(1,.09f,.25f),red);box(new Vector3(-6.7f,-1.7f,2.9f),new Vector3(.25f,.09f,1),red);
   // A raised red cross and white roof panels make the site identifiable from the editor camera.
   box(new Vector3(0,2,4.36f),new Vector3(3.6f,.8f,.05f),red);box(new Vector3(0,2,4.36f),new Vector3(.8f,3.6f,.05f),red);
   for(int i=-3;i<=3;i++)box(new Vector3(i*2.5f,-6,.32f),new Vector3(.10f,1.7f,.02f),white);
  }
  bool ClxnEMSBlock(Vector3 point){lock(ClxnProject.Sync)foreach(var p in ClxnProject.Pins.Where(p=>p.Model=="clxn:ems")){var local=Quaternion.RotationAxis(Vector3.UnitZ,-p.Heading*(float)Math.PI/180).Multiply(point-p.Position);if(Math.Abs(local.X)<8.35f&&local.Y> -1.85f&&local.Y<5.85f&&local.Z<3.9f)return true;}return false;}
 }
}

