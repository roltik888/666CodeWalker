using System;
using System.Collections.Generic;
using System.Linq;
using CodeWalker.GameFiles;
using SharpDX;
namespace CodeWalker {
 public partial class WorldForm {
  readonly Dictionary<DrawableGeometry,Vector3[]> clxnMeshVertices=new Dictionary<DrawableGeometry,Vector3[]>();
  bool ClxnVisualGround(Ray worldRay,float limit,out Vector3 point,out Vector3 normal) {
   point=Vector3.Zero;normal=Vector3.UnitZ;bool hit=false;float nearest=limit;
   foreach(var item in Renderer.RenderedDrawables) {
    var drawable=item.Drawable;if(drawable==null||drawable.Skeleton!=null)continue;var entity=item.Entity;var position=entity?.Position??Vector3.Zero;var orientation=entity?.Orientation??Quaternion.Identity;var scale=entity?.Scale??Vector3.One;
    if(Math.Abs(scale.X)<.0001f||Math.Abs(scale.Y)<.0001f||Math.Abs(scale.Z)<.0001f)continue;
    var inv=Quaternion.Invert(orientation);var ray=new Ray(inv.Multiply(worldRay.Position-position)/scale,inv.Multiply(worldRay.Direction)/scale);
    var box=new BoundingBox(drawable.BoundingBoxMin,drawable.BoundingBoxMax);float boxDistance;if(!ray.Intersects(ref box,out boxDistance)||boxDistance>=nearest)continue;
    var models=drawable.DrawableModels?.High??drawable.DrawableModels?.Med??drawable.DrawableModels?.Low??drawable.DrawableModels?.VLow;if(models==null)continue;
    foreach(var model in models)foreach(var geometry in model.Geometries??new DrawableGeometry[0]) {
     var data=geometry.VertexData??geometry.VertexBuffer?.Data1??geometry.VertexBuffer?.Data2;var indices=geometry.IndexBuffer?.Indices;if(data?.Info==null||indices==null)continue;
     Vector3[] vertices;if(!clxnMeshVertices.TryGetValue(geometry,out vertices)){if(data.Info.GetComponentType(0)!=VertexComponentType.Float3)continue;vertices=new Vector3[geometry.VerticesCount];for(int i=0;i<vertices.Length;i++)vertices[i]=data.GetVector3(i,0);if(clxnMeshVertices.Count>256)clxnMeshVertices.Clear();clxnMeshVertices[geometry]=vertices;}
     for(int i=0;i+2<indices.Length;i+=3){int ia=indices[i],ib=indices[i+1],ic=indices[i+2];if(ia>=vertices.Length||ib>=vertices.Length||ic>=vertices.Length)continue;float t;if(!ClxnGroundMath.Triangle(ray,vertices[ia],vertices[ib],vertices[ic],out t)||t>=nearest)continue;var n=Vector3.Cross(orientation.Multiply((vertices[ib]-vertices[ia])*scale),orientation.Multiply((vertices[ic]-vertices[ia])*scale));if(n.LengthSquared()<.000001f)continue;n=Vector3.Normalize(n);if(Math.Abs(n.Z)<.5f)continue;if(n.Z<0)n=-n;nearest=t;normal=n;point=worldRay.Position+worldRay.Direction*t;hit=true;}
    }
   }
   ClxnPin[] sites;lock(ClxnProject.Sync)sites=ClxnProject.Pins.Where(p=>p.Model=="clxn:ems").ToArray();
   foreach(var site in sites){var triangles=new List<CodeWalker.Rendering.VertexTypePC>();ClxnEMSGeometry(triangles,site);for(int i=0;i+2<triangles.Count;i+=3){var a=triangles[i].Position;var b=triangles[i+1].Position;var c=triangles[i+2].Position;var n=Vector3.Normalize(Vector3.Cross(b-a,c-a));if(Math.Abs(n.Z)<.5f)continue;float t;if(ClxnGroundMath.Triangle(worldRay,a,b,c,out t)&&t<nearest){nearest=t;point=worldRay.Position+worldRay.Direction*t;normal=n.Z<0?-n:n;hit=true;}}}
   return hit;
  }
 }
}

