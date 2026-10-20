using SharpDX;
namespace CodeWalker { internal static class ClxnVisibility {internal static bool LabelWithinRange(Vector3 camera,Vector3 position){var distance=Vector3.DistanceSquared(camera,position);return !float.IsNaN(distance)&&distance<=250000f;}}}
