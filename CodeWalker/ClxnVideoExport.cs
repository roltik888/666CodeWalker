using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Point=System.Drawing.Point;
using Color=System.Drawing.Color;
namespace CodeWalker {
 public partial class WorldForm {
  protected override bool ShowWithoutActivation=>Program.ExportVideoPath!=null;
  bool clxnAutoExportStarted;volatile bool clxnExportWaitingActors;
  volatile bool clxnExporting,clxnExportCancel;bool clxnExportFramesDone;int clxnExportFrame,clxnExportFrames,clxnExportFps,clxnExportInputWidth,clxnExportInputHeight,clxnExportLastUpdate=-1,clxnExportWidth,clxnExportHeight;
  Texture2D clxnExportResolved,clxnExportStaging;byte[] clxnExportPending;ClxnMp4Encoder clxnEncoder;
  readonly Stopwatch clxnExportFrameWait=new Stopwatch();bool clxnExportFramesStarted;volatile bool clxnExportWaitingDetails;
  readonly Stopwatch clxnExportWarmup=new Stopwatch();Size clxnExportOldSize;FormWindowState clxnExportOldState;string clxnExportPath,clxnExportFailure;
  void ClxnExportDialog(){
   if(clxnExporting)return;if(!initialised){clxnMessage="Дождитесь загрузки мира.";return;}lock(ClxnProject.Sync)if(ClxnProject.CameraKeys.Count<2){clxnMessage="Добавьте минимум два кадра камеры для экспорта.";return;}
   using(var dialog=new Form{Text="Экспорт пролётки в MP4",ClientSize=new Size(390,295),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false}){
    ClxnStyle.Apply(dialog);dialog.StartPosition=FormStartPosition.CenterParent;var layout=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),FlowDirection=FlowDirection.TopDown,WrapContents=false};dialog.Controls.Add(layout);layout.Controls.Add(ClxnStyle.Label("MP4 · H.264 · чистый 3D-кадр",12));layout.Controls.Add(ClxnStyle.Label("Разрешение",9,ClxnStyle.Muted));var resolution=new ComboBox{Width=340,DropDownStyle=ComboBoxStyle.DropDownList};resolution.Items.AddRange(new object[]{"1280 × 720 — быстрее","1920 × 1080 — подробнее"});resolution.SelectedIndex=0;layout.Controls.Add(resolution);layout.Controls.Add(ClxnStyle.Label("Кадров в секунду",9,ClxnStyle.Muted));var fps=new ComboBox{Width=340,DropDownStyle=ComboBoxStyle.DropDownList};fps.Items.AddRange(new object[]{"24","30","60"});fps.SelectedIndex=1;layout.Controls.Add(fps);layout.Controls.Add(new Label{Text="Кадры считаются по времени сцены.\nПанели и курсор не записываются. Без звука.",AutoSize=true,ForeColor=ClxnStyle.Muted,Margin=new Padding(0,10,0,6)});var save=ClxnStyle.Button("Выбрать файл и экспортировать",(s,e)=>dialog.DialogResult=DialogResult.OK,340,true);layout.Controls.Add(save);if(dialog.ShowDialog(this)!=DialogResult.OK)return;
    using(var file=new SaveFileDialog{Filter="Видео MP4|*.mp4",FileName="666clxn-flight.mp4",AddExtension=true,DefaultExt="mp4"})if(file.ShowDialog(this)==DialogResult.OK)ClxnStartExport(file.FileName,resolution.SelectedIndex==0?1280:1920,resolution.SelectedIndex==0?720:1080,int.Parse(fps.Text));
   }
  }
  void ClxnStartExport(string path,int width,int height,int fps){
   var encoderPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tools","ffmpeg.exe");if(!File.Exists(encoderPath)){clxnMessage="Не найден tools/ffmpeg.exe. Распакуйте архив приложения целиком.";ClxnAutoExportFinish(false);return;}
   if(width<320||width>3840||height<240||height>2160||width%2!=0||height%2!=0){clxnMessage="Неверный размер видео.";ClxnAutoExportFinish(false);return;}
   lock(ClxnProject.Sync)if(ClxnProject.CameraKeys.Count<2){clxnMessage="Для экспорта нужны два кадра камеры.";ClxnAutoExportFinish(false);return;}
   if(clxnFlightPlaying)ClxnStopFlight();if(clxnWalking)ClxnLeaveWalk();
   clxnExportOldSize=ClientSize;clxnExportOldState=WindowState;
   try{float duration;lock(ClxnProject.Sync)duration=ClxnFlightMath.Duration(ClxnProject.CameraKeys);int frames=ClxnVideoPlan.FrameCount(duration,fps);clxnExportOldSize=ClientSize;clxnExportOldState=WindowState;WindowState=FormWindowState.Normal;ClientSize=new Size(width,height);
    clxnEncoder=null;clxnExportWidth=width;clxnExportHeight=height;clxnExportFailure=null;
    ClxnStartFlight();lock(RenderSyncRoot){clxnExportPath=path;clxnExportFps=fps;clxnExportFrames=frames;clxnExportFrame=0;clxnExportLastUpdate=-1;clxnExportFramesDone=false;clxnExportCancel=false;clxnExportPending=null;clxnExporting=true;clxnExportWaitingActors=true;Renderer.DXMan.BackgroundSleepMilliseconds=1;clxnExportFramesStarted=false;clxnExportWaitingDetails=false;clxnExportWarmup.Restart();}
    clxnPauseButton.Enabled=false;clxnPlaybackBar.Visible=true;clxnMessage="Экспорт MP4: подготовка ресурсов сцены…";
   }catch(Exception ex){clxnEncoder?.Cancel();if(clxnFlightPlaying)ClxnStopFlight();ClientSize=clxnExportOldSize;WindowState=clxnExportOldState;clxnMessage="Экспорт не запущен: "+ex.Message;ClxnAutoExportFinish(false);}
  }
  void ClxnCancelExport(){if(!clxnExporting)return;clxnExportCancel=true;clxnEncoder?.Cancel();clxnMessage="Экспорт отменяется…";}
  bool ClxnExportPreparing=>clxnExporting&&(clxnExportWaitingActors||clxnExportWarmup.Elapsed.TotalSeconds<clxnFlightKeys.Length*.75+3);
  void ClxnPositionExportCamera(){if(clxnExportCancel)return;if(ClxnExportPreparing){int index=(int)(clxnExportWarmup.Elapsed.TotalSeconds/.75);var key=clxnFlightKeys[index<clxnFlightKeys.Length?index:0];ClxnApplyFlightCamera(key.Position,key.Rotation,key.Fov);return;}clxnFlightTime=ClxnVideoPlan.TimeForFrame(Math.Min(clxnExportFrame,clxnExportFrames-1),clxnExportFps);Vector3 p,r;float f;ClxnFlightMath.Evaluate(clxnFlightKeys,clxnFlightTime,clxnFlightSmooth,out p,out r,out f);ClxnApplyFlightCamera(p,r,f);}
  float ClxnExportElapsed(float realElapsed){if(!string.IsNullOrEmpty(ClxnProject.SkyWeather))Renderer.SetTimeOfDay(ClxnProject.SkyHour);if(!clxnExporting||ClxnExportPreparing)return realElapsed;if(clxnExportFrame==clxnExportLastUpdate)return 0;clxnExportLastUpdate=clxnExportFrame;return clxnExportFrame==0?0:1f/clxnExportFps;}
  void ClxnCaptureExportFrame(DeviceContext context){
   if(!clxnExporting||clxnExportFramesDone||clxnExportCancel)return;
   clxnExportWaitingActors=clxnActorLoads.Values.Any(t=>!t.IsCompleted)||clxnVehicleLoads.Values.Any(t=>!t.IsCompleted);
   if(clxnExportWaitingActors&&clxnExportWarmup.Elapsed.TotalSeconds>60){clxnMessage="Модели не загрузились за минуту. Проверьте журнал и повторите экспорт.";clxnExportCancel=true;clxnEncoder?.Cancel();return;}
   if(ClxnExportPreparing)return;
   if(!clxnExportFramesStarted){clxnExportFramesStarted=true;clxnExportFrameWait.Restart();}
   clxnExportWaitingDetails=DateTime.UtcNow.Ticks-System.Threading.Interlocked.Read(ref clxnAssetWorkTicks)<TimeSpan.TicksPerMillisecond*120&&clxnExportFrameWait.ElapsedMilliseconds<5000;
   if(clxnExportWaitingDetails)return;
   try{var captureSource=Renderer.DXMan.backbuffer;var captureDescription=captureSource.Description;
    if(clxnEncoder==null){clxnExportInputWidth=captureDescription.Width;clxnExportInputHeight=captureDescription.Height;clxnEncoder=new ClxnMp4Encoder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tools","ffmpeg.exe"),clxnExportPath,clxnExportInputWidth,clxnExportInputHeight,clxnExportWidth,clxnExportHeight,clxnExportFps);}
    if(clxnEncoder.Finished)return;if(captureDescription.Width!=clxnExportInputWidth||captureDescription.Height!=clxnExportInputHeight)throw new InvalidOperationException("Размер окна изменён во время экспорта. Запустите экспорт заново.");
    if(clxnExportPending==null){if(!clxnEncoder.TryRent(out clxnExportPending))return;var source=Renderer.DXMan.backbuffer;var desc=source.Description;if(clxnExportStaging==null){desc.SampleDescription=new SampleDescription(1,0);desc.BindFlags=BindFlags.None;desc.OptionFlags=ResourceOptionFlags.None;desc.Usage=ResourceUsage.Default;desc.CpuAccessFlags=CpuAccessFlags.None;clxnExportResolved=new Texture2D(Renderer.DXMan.device,desc);desc.Usage=ResourceUsage.Staging;desc.CpuAccessFlags=CpuAccessFlags.Read;clxnExportStaging=new Texture2D(Renderer.DXMan.device,desc);}
     if(source.Description.SampleDescription.Count>1)context.ResolveSubresource(source,0,clxnExportResolved,0,Format.R8G8B8A8_UNorm);else context.CopyResource(source,clxnExportResolved);context.CopyResource(clxnExportResolved,clxnExportStaging);
     var data=context.MapSubresource(clxnExportStaging,0,MapMode.Read,SharpDX.Direct3D11.MapFlags.None);try{int bytes=clxnExportInputWidth*4;for(int y=0;y<clxnExportInputHeight;y++)Marshal.Copy(IntPtr.Add(data.DataPointer,y*data.RowPitch),clxnExportPending,y*bytes,bytes);}finally{context.UnmapSubresource(clxnExportStaging,0);}
    }
    if(clxnEncoder.TryQueue(clxnExportPending)){clxnExportPending=null;clxnExportFrame++;clxnExportFrameWait.Restart();if(clxnExportFrame>=clxnExportFrames){clxnExportFramesDone=true;clxnEncoder.Complete();}}
   }catch(Exception ex){clxnExportFailure=ex.Message;clxnMessage="Ошибка захвата видео: "+ex.Message;clxnExportCancel=true;clxnEncoder?.Cancel();}
  }
  void ClxnDisposeExportBuffers(){clxnExportStaging?.Dispose();clxnExportStaging=null;clxnExportResolved?.Dispose();clxnExportResolved=null;if(clxnExportPending!=null){clxnEncoder?.Return(clxnExportPending);clxnExportPending=null;}}
  void ClxnAutoExportFinish(bool success){if(Program.ExportVideoPath==null||!Program.ExitAfterExport)return;try{File.WriteAllLines(Program.ExportVideoPath+".log.txt",clxnJournal.Snapshot());}catch{}Environment.ExitCode=success?0:1;BeginInvoke(new Action(Close));}
  void ClxnExportTick(){if(!clxnExporting){if(Program.ExportVideoPath!=null&&!clxnAutoExportStarted&&initialised&&clxnSceneRestored){clxnAutoExportStarted=true;ClxnStartExport(Program.ExportVideoPath,Program.ExportWidth,Program.ExportHeight,Program.ExportFps);}return;}clxnPlaybackInfo.Text=clxnExportCancel?"Отмена экспорта…":ClxnExportPreparing?"MP4 · подготовка сцены…":clxnExportWaitingDetails?"MP4 · загрузка деталей сцены…":clxnExportFramesDone?"MP4 · завершение файла…":"MP4 · "+clxnExportFrame+" / "+clxnExportFrames+" · "+(clxnExportFrame*100/Math.Max(1,clxnExportFrames))+"%";
   if(clxnEncoder==null&&!clxnExportCancel)return;if(clxnEncoder!=null&&!clxnEncoder.Finished)return;bool success=clxnEncoder?.Success??false;string error=clxnExportFailure??clxnEncoder?.Error;lock(RenderSyncRoot){clxnExporting=false;Renderer.DXMan.BackgroundSleepMilliseconds=100;ClxnDisposeExportBuffers();}clxnPauseButton.Enabled=true;ClxnStopFlight();ClientSize=clxnExportOldSize;WindowState=clxnExportOldState;clxnMessage=success?"MP4 сохранён: "+clxnExportPath:error!=null?"Ошибка экспорта MP4: "+error:"Экспорт отменён. Исходная сцена сохранена.";ClxnAutoExportFinish(success);
  }
 }
}
