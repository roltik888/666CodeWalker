using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using CodeWalker.GameFiles;
using CodeWalker.Utils;
using Color=System.Drawing.Color;
namespace CodeWalker {
 internal static class ClxnBlackwork {
  internal static Texture Create(Texture original,bool body){
   int width=original?.Width??512,height=original?.Height??512;
   if(width<16||width>2048||height<16||height>2048){width=height=512;original=null;}
   byte[] pixels=null;try{if(original!=null)pixels=DDSIO.GetPixels(original,0);}catch{}
   if(pixels==null||pixels.Length!=width*height*4){pixels=new byte[width*height*4];for(int i=0;i<pixels.Length;i+=4){pixels[i]=159;pixels[i+1]=185;pixels[i+2]=211;pixels[i+3]=255;}}
   // Keep the original face, pores and shadows. Warm only skin-coloured pixels.
   for(int i=0;i<pixels.Length;i+=4){float blue=pixels[i],green=pixels[i+1],red=pixels[i+2];if(red>green*.96f&&red>blue*1.06f&&red>55){pixels[i]=(byte)Math.Min(255,blue*.88f+10);pixels[i+1]=(byte)Math.Min(255,green*.96f+8);pixels[i+2]=(byte)Math.Min(255,red*.98f+11);}}
   using(var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb)){
    var data=bitmap.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{for(int y=0;y<height;y++)Marshal.Copy(pixels,y*width*4,IntPtr.Add(data.Scan0,y*data.Stride),width*4);}finally{bitmap.UnlockBits(data);}
    using(var g=Graphics.FromImage(bitmap)){g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(width/512f,height/512f);using(var ink=new SolidBrush(Color.FromArgb(body?225:205,17,18,20)))using(var pen=new Pen(Color.FromArgb(body?230:205,17,18,20),body?5:2)){
     if(body){
      for(int side=0;side<2;side++){float x=side*256;for(int row=0;row<5;row++){float y=18+row*96;using(var blade=new GraphicsPath()){blade.AddBezier(x+22,y+75,x+130,y-35,x+188,y+100,x+230,y+4);blade.AddBezier(x+230,y+4,x+195,y+115,x+113,y+24,x+22,y+75);g.FillPath(ink,blade);}g.DrawBezier(pen,x+25,y+84,x+89,y+10,x+171,y+119,x+235,y+15);g.DrawEllipse(pen,x+91,y+30,38,38);g.DrawLine(pen,x+110,y+21,x+110,y+77);g.DrawLine(pen,x+80,y+48,x+140,y+48);}
       g.FillRectangle(ink,x+12,188,232,12);g.FillRectangle(ink,x+12,208,232,5);g.FillRectangle(ink,x+12,404,232,10);
       using(var font=new Font("Arial",31,FontStyle.Bold,GraphicsUnit.Pixel))g.DrawString("666",font,ink,x+77,421);
      }
     }else{
      // Side-of-head ornaments and neck bands; the central facial features remain clear.
      for(int side=0;side<2;side++){float x=side==0?38:420;for(int j=0;j<4;j++){float y=90+j*28;g.DrawLines(pen,new[]{new PointF(x,y),new PointF(x+28,y+13),new PointF(x+10,y+27)});}g.DrawEllipse(pen,x+5,105,22,65);}
      g.FillRectangle(ink,24,450,464,9);g.FillRectangle(ink,24,468,464,4);for(int i=0;i<14;i++)g.FillPolygon(ink,new[]{new PointF(27+i*33,444),new PointF(42+i*33,419),new PointF(56+i*33,444)});
     }
    }}
    var result=bitmap.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{for(int y=0;y<height;y++)Marshal.Copy(IntPtr.Add(result.Scan0,y*result.Stride),pixels,y*width*4,width*4);}finally{bitmap.UnlockBits(result);}
   }
   return new Texture{Name="666clxn_blackwork_"+(body?"body":"head"),Width=(ushort)width,Height=(ushort)height,Depth=1,Levels=1,Stride=(ushort)(width*4),Format=TextureFormat.D3DFMT_A8R8G8B8,Data=new TextureData{FullData=pixels}};
  }
 }
}
