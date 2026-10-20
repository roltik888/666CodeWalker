using System;
using SharpDX;
namespace CodeWalker {
 public partial class WorldForm {
  void ClxnFocusObject(ClxnPin pin){if(pin==null)return;if(string.IsNullOrEmpty(pin.Model)){ClxnNavigate(pin.Position);return;}float heading=pin.Heading*(float)Math.PI/180;var q=Quaternion.RotationAxis(Vector3.UnitZ,heading);var offset=pin.Model=="clxn:ems"?new Vector3(0,-26,15):pin.IsVehicle?new Vector3(0,-8,3.2f):new Vector3(0,-4,1.8f);lock(RenderSyncRoot){camera.TargetRotation=new Vector3(heading,pin.Model=="clxn:ems"?.5f:.12f,0);ClxnCameraNavigation.Restore(camera,pin.Position+q.Multiply(offset));}}
 }
}
