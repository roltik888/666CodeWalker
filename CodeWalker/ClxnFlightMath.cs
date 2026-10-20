using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
namespace CodeWalker {
 internal sealed class ClxnCameraKey {
  internal string Name="Кадр";
  internal Vector3 Position,Rotation;
  internal float Seconds=4,Fov=1;
  public override string ToString()=>Name+"  ·  "+Seconds.ToString("0.##")+" с";
  internal ClxnCameraKey Copy()=>new ClxnCameraKey{Name=Name,Position=Position,Rotation=Rotation,Seconds=Seconds,Fov=Fov};
 }
 internal static class ClxnFlightMath {
  internal static float Duration(IList<ClxnCameraKey> keys)=>keys.Take(Math.Max(0,keys.Count-1)).Sum(k=>k.Seconds);
  internal static float Angle(float a,float b,float t){float delta=b-a;while(delta>Math.PI)delta-=(float)(Math.PI*2);while(delta< -Math.PI)delta+=(float)(Math.PI*2);return a+delta*t;}
  internal static void Evaluate(IList<ClxnCameraKey> keys,float time,bool smooth,out Vector3 position,out Vector3 rotation,out float fov){
   if(keys.Count==0)throw new ArgumentException("Нет кадров камеры.");int index=0;time=Math.Max(0,time);while(index<keys.Count-2&&time>=keys[index].Seconds){time-=keys[index].Seconds;index++;}
   var a=keys[index];var b=keys[Math.Min(index+1,keys.Count-1)];float t=Math.Min(1,time/Math.Max(.25f,a.Seconds));float eased=smooth?t*t*(3-2*t):t;
   position=Vector3.Lerp(a.Position,b.Position,eased);rotation=new Vector3(Angle(a.Rotation.X,b.Rotation.X,eased),a.Rotation.Y+(b.Rotation.Y-a.Rotation.Y)*eased,Angle(a.Rotation.Z,b.Rotation.Z,eased));fov=a.Fov+(b.Fov-a.Fov)*eased;
  }
 }
}
