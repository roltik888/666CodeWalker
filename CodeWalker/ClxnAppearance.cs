using System;
using System.Collections.Generic;
using System.Linq;
using CodeWalker.GameFiles;
using CodeWalker.Rendering;
using CodeWalker.World;
using SharpDX;
namespace CodeWalker {
 public partial class WorldForm {
  float ClxnModelFoot(Ped ped){var feet=ped.Skeleton?.Bones?.Items?.Where(b=>b.Name=="SKEL_L_Foot"||b.Name=="SKEL_R_Foot"||b.Name=="SKEL_L_Toe0"||b.Name=="SKEL_R_Toe0").ToArray();if(feet!=null&&feet.Length>0)return feet.Min(b=>b.AnimTransform.TranslationVector.Z)-.06f;return ped.Drawables.Where(d=>d!=null).Select(d=>d.BoundingBoxMin.Z).DefaultIfEmpty(0).Min();}
  void ClxnRelaxedPose(Ped ped){
   if(ped.AnimClip!=null)return;
   foreach(var name in new[]{"move_m@multiplayer","move_m@generic","move_m@casual@a"}){
    var dict=gameFileCache.GetYcd(JenkHash.GenHash(name));if(dict==null)continue;var timeout=System.Diagnostics.Stopwatch.StartNew();while(!dict.Loaded&&timeout.ElapsedMilliseconds<2500){gameFileCache.TryLoadEnqueue(dict);System.Threading.Thread.Sleep(2);}if(!dict.Loaded)continue;var idle=dict.ClipMap?.Values.FirstOrDefault(c=>(c.Clip?.Name??"").IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0);if(idle!=null){ped.AnimClip=idle;ped.Ycd=dict;return;}
   }
   var own=ped.Ycd?.ClipMap?.Values.FirstOrDefault(c=>(c.Clip?.Name??"").IndexOf("stand",StringComparison.OrdinalIgnoreCase)>=0);if(own!=null)ped.AnimClip=own;
  }
  Texture ClxnSolidTexture(string name,byte r,byte g,byte b){var data=new byte[64];for(int i=0;i<16;i++){data[i*4]=b;data[i*4+1]=g;data[i*4+2]=r;data[i*4+3]=255;}return new Texture{Name=name,Width=4,Height=4,Stride=16,Depth=1,Levels=1,Format=TextureFormat.D3DFMT_A8R8G8B8,Data=new TextureData{FullData=data}};}
  readonly object clxnSkinSync=new object();Texture clxnBlackworkHead,clxnBlackworkBody;
  Texture ClxnSkin(Texture original,bool body){lock(clxnSkinSync){if(body)return clxnBlackworkBody??(clxnBlackworkBody=ClxnBlackwork.Create(original,true));return clxnBlackworkHead??(clxnBlackworkHead=ClxnBlackwork.Create(original,false));}}
  void ClxnApplyRoltikLook(Ped ped){
   var white=ClxnSolidTexture("666clxn_white",235,236,240);
   // The white outfit is separate from exposed skin and hands.
   foreach(int i in new[]{4,6,8,11})ped.Textures[i]=white;
   ped.SetComponentDrawable(1,1,0,0,gameFileCache);if(ped.Drawables[1]==null)throw new InvalidOperationException("Маска Roltik не найдена в игровых ресурсах.");
   ped.Textures[0]=ClxnSkin(ped.Textures[0],false);
   ped.Textures[3]=ClxnSkin(ped.Textures[3],true);
  }
  static void ClxnBox(List<VertexTypePC> output,Vector3 basePosition,Quaternion rotation,Vector3 centre,Vector3 size,uint colour){
   var h=size*.5f;var corners=new[]{new Vector3(-h.X,-h.Y,-h.Z),new Vector3(h.X,-h.Y,-h.Z),new Vector3(h.X,h.Y,-h.Z),new Vector3(-h.X,h.Y,-h.Z),new Vector3(-h.X,-h.Y,h.Z),new Vector3(h.X,-h.Y,h.Z),new Vector3(h.X,h.Y,h.Z),new Vector3(-h.X,h.Y,h.Z)};
   for(int i=0;i<8;i++)corners[i]=basePosition+rotation.Multiply(corners[i]+centre);
   int[] ids={0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};
   for(int i=0;i<ids.Length;i++)output.Add(new VertexTypePC{Position=corners[ids[i]],Colour=colour});
  }
  static uint ClxnSceneColour(byte r,byte g,byte b)=>0xff000000u|((uint)b<<16)|((uint)g<<8)|r;
  readonly List<VertexTypePC> clxnStaticProps=new List<VertexTypePC>();volatile bool clxnScenePropsDirty=true;
  void ClxnRenderSceneProps(){
   if(clxnScenePropsDirty){clxnScenePropsDirty=false;clxnStaticProps.Clear();ClxnPin[] pins;lock(ClxnProject.Sync)pins=ClxnProject.Pins.Where(p=>p.Model=="clxn:ems").ToArray();foreach(var pin in pins)ClxnEMSGeometry(clxnStaticProps,pin);}
   if(clxnStaticProps.Count>0)Renderer.RenderSceneTriangles(clxnStaticProps);
  }
 }
}
