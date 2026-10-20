using CodeWalker.World;
using SharpDX;
namespace CodeWalker {
 internal static class ClxnCameraNavigation {
  internal static void Restore(Camera camera,Vector3 position) {
   camera.CurrentRotation=camera.TargetRotation;
   camera.CurrentDistance=camera.TargetDistance;
   camera.Update(0);
   if(camera.FollowEntity!=null)camera.FollowEntity.Position=position-camera.LocalLookAt;
   camera.Update(0);
  }
 }
}
