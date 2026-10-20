using System;
using SharpDX;
namespace CodeWalker {
 internal static class ClxnGroundMath {
  internal static bool Triangle(Ray ray,Vector3 a,Vector3 b,Vector3 c,out float distance) {
   distance=0;var e1=b-a;var e2=c-a;var p=Vector3.Cross(ray.Direction,e2);float det=Vector3.Dot(e1,p);if(Math.Abs(det)<.0000001f)return false;float inv=1/det;var t=ray.Position-a;float u=Vector3.Dot(t,p)*inv;if(u<0||u>1)return false;var q=Vector3.Cross(t,e1);float v=Vector3.Dot(ray.Direction,q)*inv;if(v<0||u+v>1)return false;distance=Vector3.Dot(e2,q)*inv;return distance>.001f;
  }
  internal static float RootHeight(float surface,float modelMinZ)=>surface-Math.Min(0,Math.Max(-1.5f,modelMinZ))+.025f;
 }
}
