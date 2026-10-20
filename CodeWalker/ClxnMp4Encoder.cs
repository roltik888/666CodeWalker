using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
namespace CodeWalker {
 internal sealed class ClxnMp4Encoder {
  readonly BlockingCollection<byte[]> frames=new BlockingCollection<byte[]>(2);
  readonly ConcurrentBag<byte[]> pool=new ConcurrentBag<byte[]>();
  readonly Process process;readonly Task worker;readonly string temporary,destination;string details="";
  internal volatile bool Cancelled;internal string Error;internal volatile bool Finished;internal bool Success;
  internal int WrittenFrames;internal Task Completion=>worker;
  internal ClxnMp4Encoder(string encoder,string output,int inputWidth,int inputHeight,int width,int height,int fps){
   destination=Path.GetFullPath(output);temporary=Path.Combine(Path.GetDirectoryName(destination),"."+Path.GetFileNameWithoutExtension(output)+"."+Guid.NewGuid().ToString("N")+".partial.mp4");
   for(int i=0;i<4;i++)pool.Add(new byte[checked(inputWidth*inputHeight*4)]);
   var args="-hide_banner -loglevel error -nostdin -f rawvideo -pixel_format rgba -video_size "+inputWidth+"x"+inputHeight+" -framerate "+fps+" -i pipe:0 -an -vf scale="+width+":"+height+":force_original_aspect_ratio=decrease,pad="+width+":"+height+":(ow-iw)/2:(oh-ih)/2 -c:v libx264 -preset veryfast -crf 20 -threads 2 -pix_fmt yuv420p -movflags +faststart -y \""+temporary+"\"";
   process=new Process{StartInfo=new ProcessStartInfo(encoder,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true}};
   process.ErrorDataReceived+=(s,e)=>{if(!string.IsNullOrEmpty(e.Data))lock(frames){details+=e.Data+Environment.NewLine;if(details.Length>3000)details=details.Substring(details.Length-3000);}};
   try{process.Start();process.BeginErrorReadLine();}catch{process.Dispose();throw;}
   worker=Task.Run(()=>{try{foreach(var frame in frames.GetConsumingEnumerable()){try{if(Cancelled)break;process.StandardInput.BaseStream.Write(frame,0,frame.Length);WrittenFrames++;}finally{pool.Add(frame);}}process.StandardInput.Close();if(!process.WaitForExit(120000)){process.Kill();throw new IOException("Кодировщик не завершил обработку.");}process.WaitForExit();if(Cancelled)return;if(process.ExitCode!=0)throw new IOException("FFmpeg: "+details);if(WrittenFrames==0||!File.Exists(temporary))throw new IOException("Кодировщик не получил кадры.");if(File.Exists(destination))File.Replace(temporary,destination,null);else File.Move(temporary,destination);Success=true;}catch(Exception ex){if(!Cancelled)Error=ex.Message;}finally{try{if(!process.HasExited)process.Kill();}catch{}process.Dispose();if(!Success)try{if(File.Exists(temporary))File.Delete(temporary);}catch{}Finished=true;}});
  }
  internal bool TryRent(out byte[] buffer)=>pool.TryTake(out buffer);
  internal void Return(byte[] buffer){if(buffer!=null)pool.Add(buffer);}
  internal bool TryQueue(byte[] buffer)=>!Finished&&!frames.IsAddingCompleted&&frames.TryAdd(buffer);
  internal void Complete(){if(!frames.IsAddingCompleted)frames.CompleteAdding();}
  internal void Cancel(){Cancelled=true;Complete();try{if(!process.HasExited)process.Kill();}catch{}}
 }
}
