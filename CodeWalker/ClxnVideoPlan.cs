using System;
namespace CodeWalker {
 internal static class ClxnVideoPlan {
  internal static int FrameCount(float seconds,int fps){if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0||seconds>1800)throw new ArgumentOutOfRangeException(nameof(seconds),"Экспорт поддерживает пролётки до 30 минут.");if(fps!=24&&fps!=30&&fps!=60)throw new ArgumentOutOfRangeException(nameof(fps));return Math.Max(1,(int)Math.Ceiling(Math.Round(seconds,3)*fps));}
  internal static float TimeForFrame(int frame,int fps)=>Math.Max(0,frame)/(float)fps;
 }
}
