using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using CodeWalker.Rendering;
using CodeWalker.GameFiles;
using SharpDX;

namespace CodeWalker
{
    public partial class WorldForm
    {
        internal readonly ClxnDocument ClxnProject = new ClxnDocument();
        internal string ClxnTool = "move";
        internal int ClxnColour = unchecked((int)0xff000000);
        internal float ClxnPlaneZ;
        internal bool ClxnVisible = true;
        internal bool ClxnSurface = true;
        internal float ClxnBrushPixels = 4;
        ClxnStroke clxnActiveStroke;
        bool clxnPreviewHit;
        volatile bool clxnPointerOnMap;
        readonly Stopwatch clxnPreviewClock=Stopwatch.StartNew();
        Vector3 clxnPreviewPosition, clxnPreviewNormal=Vector3.UnitZ;
        string clxnCurrentMessage="Нажмите «Кисть» и рисуйте ЛКМ прямо на поверхности карты.";
        string clxnMessage {get=>clxnCurrentMessage;set{clxnCurrentMessage=value;clxnJournal.Add(value);}}
        string clxnSelectedId;
        internal Vector3 ClxnCameraPosition { get { lock(RenderSyncRoot) return camera.Position; } }
        internal Vector3 ClxnFocusPosition { get { lock(RenderSyncRoot) return camera.FollowEntity?.Position ?? camera.Position; } }
        internal void ClxnOpenStudio() { ClxnToggleLibrary(); }
        internal void ClxnMapMode() { SetCameraMode("2D Map"); }
        internal void ClxnEndDrawing() { ClxnFinishStroke(); }
        internal void ClxnSetTool(string tool) {
            ClxnFinishStroke();ClxnTool=tool;clxnPreviewHit=false;
            clxnMessage=tool=="ems"?"Выберите свободную площадку: ЛКМ — поставить EMS-концепт. Это реконструкция, не Majestic.":tool=="draw"?"Кисть: удерживайте ЛКМ на поверхности. ПКМ — повернуть камеру. Esc — обзор.":tool=="pin"?"Метка: наведите курсор на поверхность и щёлкните ЛКМ.":tool=="vehicle"?"Машина: выберите модель и щёлкните по земле. Поворот задаётся в Объектах.":tool=="actor"?"Персонаж: выберите модель и поворот, затем щёлкните ЛКМ по земле.":"Обзор: WASD — движение, ЛКМ — вращение. F7 — сохранить точку камеры.";
            ClxnRefreshButtons();ClxnContextLayout();
        }
        internal void ClxnNavigate(Vector3 position) {
            if(MapViewEnabled) SetCameraMode("Perspective");
            lock(RenderSyncRoot) {
                ClxnCameraNavigation.Restore(camera,position);
            }
            clxnMessage="Камера перемещена: "+ClxnDocument.Coords(position);
        }
        internal void ClxnCapturePin() {
            var pin=ClxnProject.AddPin("ТП камеры " + DateTime.Now.ToString("HH:mm:ss"),ClxnCameraPosition,ClxnColour);
            clxnSelectedId=pin.Id;clxnMessage="✓ Точка камеры сохранена. F6 — открыть список точек.";
            if(clxnLibrary!=null && clxnLibrary.Visible)ClxnSelectPin(pin.Id);else ClxnRefreshLibrary();
        }
        bool ClxnHandleKey(KeyEventArgs e) {
            var shortcut=ClxnShortcuts.ToolForKey((int)e.KeyData);if(shortcut!=null&&!clxnWalking&&!clxnFlightPlaying&&!ClxnTextEntryFocused()){if(clxnUiCollapsed)ClxnCollapse(false);ClxnSetTool(shortcut);Focus();e.Handled=true;e.SuppressKeyPress=true;return true;}
if(clxnUiCollapsed&&(e.KeyCode==Keys.F3||e.KeyCode==Keys.F6||e.KeyCode==Keys.F8))ClxnCollapse(false);
            if(clxnFlightPlaying){if(e.KeyCode==Keys.Escape)ClxnStopFlight();else if(e.KeyCode==Keys.F5)ClxnPauseFlight();else if(e.KeyCode==Keys.Tab)clxnPlaybackBar.Visible=!clxnPlaybackBar.Visible;e.Handled=true;e.SuppressKeyPress=true;return true;}
            if(!ClxnTextEntryFocused()&&(e.KeyCode==Keys.F3||e.KeyCode==Keys.F4||e.KeyCode==Keys.F5)){if(e.KeyCode==Keys.F3)ClxnToggleFlightPanel();else if(e.KeyCode==Keys.F4)ClxnCaptureKey();else ClxnStartFlight();e.Handled=true;e.SuppressKeyPress=true;return true;}
            if(clxnWalking && (e.KeyCode==Keys.Escape||e.KeyCode==Keys.F9||e.KeyCode==Keys.P)){ClxnLeaveWalk();e.Handled=true;e.SuppressKeyPress=true;return true;}
            if(clxnWalking && e.KeyCode==Keys.F10){ClxnWalkView();e.Handled=true;e.SuppressKeyPress=true;return true;}
            if(clxnWalking && (e.KeyCode==Keys.F6||e.KeyCode==Keys.F8)){e.Handled=true;e.SuppressKeyPress=true;return true;}
            if(ClxnTextEntryFocused()&&e.KeyCode!=Keys.F9)return false;
            if(e.KeyCode==Keys.F9)ClxnToggleWalk();
            else if(e.KeyCode==Keys.F6) ClxnToggleLibrary();
            else if(e.Control && e.KeyCode==Keys.S)ClxnSaveProject();
            else if(e.Control && e.KeyCode==Keys.O)ClxnOpenProject();
            else if(e.KeyCode==Keys.F7) ClxnCapturePin();
            else if(e.KeyCode==Keys.F8) ClxnSetTool(ClxnTool=="draw"?"move":"draw");
            else if(e.KeyCode==Keys.Escape && ClxnTool!="move") ClxnSetTool("move");
            else if(e.Control && (e.KeyCode==Keys.Z || e.KeyCode==Keys.Y) && !(iseditmode && ProjectForm!=null && ClxnTool=="move")) { ClxnFinishStroke();ClxnProject.Undo(e.KeyCode==Keys.Y); }
            else return false;
            e.Handled=true;e.SuppressKeyPress=true;return true;
        }
        bool ClxnTryPoint(System.Drawing.Point screen,out Vector3 position,out Vector3 normal) {
            position=Vector3.Zero;normal=Vector3.UnitZ;
            if(!initialised || camera.Width<=0 || camera.Height<=0) { clxnMessage="Дождитесь загрузки мира.";return false; }
            lock(RenderSyncRoot) {
                float x=2f*screen.X/camera.Width-1f,y=1f-2f*screen.Y/camera.Height;
                var near=camera.ViewProjInvMatrix.MultiplyW(new Vector3(x,y,1));
                var far=camera.ViewProjInvMatrix.MultiplyW(new Vector3(x,y,0));
                var direction=Vector3.Normalize(far-near);
                var origin=camera.Position+(camera.IsMapView||camera.IsOrthographic?near:Vector3.Zero);
                if(float.IsNaN(direction.X)||float.IsNaN(direction.Z))return false;
                if(ClxnSurface) {
                    if(!space.Inited || space.BoundsStore==null){clxnMessage="Коллизии мира загружаются. Приблизьтесь к поверхности.";return false;}
                    var hit=space.RayIntersect(new Ray(origin,direction),10000f,collisionmeshlayers);
                    if(ClxnTool=="actor"||ClxnTool=="vehicle"||ClxnTool=="ems") {
                        Vector3 visualPoint,visualNormal;
                        if(ClxnVisualGround(new Ray(origin,direction),hit.Hit?hit.HitDist+.1f:10000,out visualPoint,out visualNormal)) { position=visualPoint+visualNormal*.025f;normal=visualNormal;return true; }
                    }
                    if(!hit.Hit){clxnMessage="Под курсором нет загруженной поверхности. Наведите на землю или здание.";return false;}
                    normal=hit.Normal.LengthSquared()>.001f?Vector3.Normalize(hit.Normal):Vector3.UnitZ;
                    position=hit.Position+normal*.045f;
                    if(clxnMessage.StartsWith("Под курсором")||clxnMessage.StartsWith("Коллизии"))clxnMessage=ClxnTool=="draw"?"Поверхность найдена. Удерживайте ЛКМ, чтобы рисовать.":"Поверхность найдена. Нажмите ЛКМ, чтобы поставить метку.";
                    return true;
                }
                if(Math.Abs(direction.Z)<.00001f){clxnMessage="Плоскость Z параллельна взгляду. Поверните камеру вниз.";return false;}
                float t=(ClxnPlaneZ-origin.Z)/direction.Z;
                if(t<0||t>100000){clxnMessage="Плоскость Z вне поля зрения. Измените высоту или поверните камеру.";return false;}
                position=origin+direction*t;return true;
            }
        }
        bool ClxnMouseDown(MouseEventArgs e) {
            if(clxnFlightPlaying)return true;
            if(e.Button==MouseButtons.Right && ClxnTool!="move") { ClxnFinishStroke();MouseRButtonDown=true;UpdateMousePosition(e);return true; }
            if(clxnWalking||e.Button!=MouseButtons.Left)return false;
            if(ClxnTool=="move") {
                string id=ClxnHitLabel(e.Location);if(id!=null){ClxnSelectPin(id);return true;}return false;
            }
            Vector3 p,n;
            if(!ClxnTryPoint(e.Location,out p,out n)){clxnPreviewHit=false;return true;}
            clxnPreviewHit=true;clxnPreviewPosition=p;clxnPreviewNormal=n;
            if(ClxnTool=="vehicle") {if(n.Z<.5f){clxnMessage="Выберите горизонтальную поверхность для машины.";return true;}ClxnSpawnVehicle(p);}
            else if(ClxnTool=="actor") { if(n.Z<.5f){clxnMessage="Для персонажа выберите землю, пол или крышу, а не стену.";return true;} ClxnSpawnActor(p); }
            else if(ClxnTool=="ems") { ClxnPlaceEMS(p); }
            else if(ClxnTool=="pin") {
                var pin=ClxnProject.AddPin("Метка "+(ClxnProject.Pins.Count+1),p,ClxnColour);
                clxnSelectedId=pin.Id;clxnMessage="✓ Метка поставлена. Нажмите её подпись, чтобы переименовать.";ClxnRefreshLibrary();
            } else {
                ClxnProject.Checkpoint();clxnActiveStroke=new ClxnStroke{Colour=ClxnColour,Width=ClxnBrushPixels};
                clxnActiveStroke.Points.Add(p);lock(ClxnProject.Sync)ClxnProject.Strokes.Add(clxnActiveStroke);Capture=true;
            }
            return true;
        }
        bool ClxnMouseMove(MouseEventArgs e) {
            if(clxnFlightPlaying)return true;
            if(ClxnTool=="move")return false;
            if(MouseRButtonDown) { RotateCam(e.X-MouseX,(e.Y-MouseY)*(MouseInvert?-1:1));return true; }
            Vector3 p,n;clxnPreviewHit=ClxnTryPoint(e.Location,out p,out n);
            if(clxnPreviewHit){clxnPreviewPosition=p;clxnPreviewNormal=n;}
            if(clxnActiveStroke!=null && clxnPreviewHit)lock(ClxnProject.Sync){
                var points=clxnActiveStroke.Points;
                if(points.Count<20000 && Vector3.DistanceSquared(points[points.Count-1],p)>.0025f)points.Add(p);
            }
            if(clxnActiveStroke!=null && !clxnPreviewHit) { ClxnFinishStroke();clxnMessage="Рисование прервано: поверхность не найдена. Начните новый штрих на поверхности."; }
            // Right-button camera movement remains available while drawing tools are selected.
            return true;
        }
        bool ClxnMouseUp(MouseEventArgs e) {
            if(e.Button==MouseButtons.Right && ClxnTool!="move"){MouseRButtonDown=false;return true;}
            if(e.Button==MouseButtons.Left && ClxnTool!="move"){ClxnFinishStroke();return true;}
            return false;
        }
        void ClxnFinishStroke() {
            if(clxnActiveStroke==null)return;
            lock(ClxnProject.Sync){if(clxnActiveStroke.Points.Count<2)ClxnProject.Strokes.Remove(clxnActiveStroke);clxnActiveStroke=null;}
            Capture=false;ClxnProject.Commit();clxnMessage="✓ Рисунок сохранён. Ctrl+Z — отменить.";
        }
        static uint ClxnRgba(int colour){uint c=unchecked((uint)colour);return(c&0xff00ff00u)|((c&0xff0000u)>>16)|((c&0xffu)<<16);}
        float ClxnWorldPixel(Vector3 p) {
            if(camera.IsMapView||camera.IsOrthographic)return camera.OrthographicSize/Math.Max(1,camera.Height);
            return Math.Max(.0001f,Vector3.Distance(p,camera.Position)*(float)Math.Tan(camera.FieldOfView*.5f)*2f/Math.Max(1,camera.Height));
        }
        volatile bool clxnAnnotationsDirty=true;List<VertexTypePC> clxnCachedAnnotations;readonly List<VertexTypePC> clxnAnnotationBuffer=new List<VertexTypePC>();
        Vector3 clxnCachedCamera;float clxnCachedFov,clxnCachedHeight,clxnCachedOrtho;string clxnCachedSelection;
        void ClxnRender() {
            if(ClxnTool!="move" && clxnPointerOnMap && clxnPreviewClock.ElapsedMilliseconds>=40) {
                Vector3 p,n;clxnPreviewHit=ClxnTryPoint(MouseLastPoint,out p,out n);
                if(clxnPreviewHit){clxnPreviewPosition=p;clxnPreviewNormal=n;}clxnPreviewClock.Restart();
            }
            if(!ClxnVisible||(clxnFlightPlaying&&ClxnProject.ZoneMode=="none"))return;
            if(ClxnProject.ZoneMode=="none"&&ClxnTool=="move"&&!clxnWalking&&!clxnAnnotationsDirty&&clxnCachedAnnotations!=null&&clxnCachedCamera==camera.Position&&clxnCachedFov==camera.FieldOfView&&clxnCachedHeight==camera.Height&&clxnCachedOrtho==(camera.IsOrthographic||camera.IsMapView?camera.OrthographicSize:0)&&clxnCachedSelection==clxnSelectedId){if(clxnCachedAnnotations.Count>0)Renderer.RenderAnnotationTriangles(clxnCachedAnnotations);ClxnUpdateProjectedPins();return;}
            var triangles=clxnAnnotationBuffer;triangles.Clear();
            Action<Vector3,Vector3,int,float> line=(a,b,c,pixels)=>{
                var delta=b-a;if(delta.LengthSquared()<.0000001f)return;
                var midpoint=(a+b)*.5f;var side=Vector3.Cross(delta,camera.Position-midpoint);
                if(side.LengthSquared()<.0000001f)side=Vector3.Cross(delta,Vector3.UnitY);
                if(side.LengthSquared()<.0000001f)return;
                side=Vector3.Normalize(side)*Math.Max(.003f,ClxnWorldPixel(midpoint)*pixels*.5f);
                uint col=ClxnRgba(c);
                triangles.Add(new VertexTypePC{Position=a-side,Colour=col});triangles.Add(new VertexTypePC{Position=a+side,Colour=col});triangles.Add(new VertexTypePC{Position=b+side,Colour=col});triangles.Add(new VertexTypePC{Position=a-side,Colour=col});triangles.Add(new VertexTypePC{Position=b+side,Colour=col});triangles.Add(new VertexTypePC{Position=b-side,Colour=col});
            };
            ClxnRenderZones3D(line);
            Action<Vector3,Vector3,float,int,float> ring=(p,n,r,c,w)=>{
                var u=Vector3.Normalize(n.GetPerpVec())*r;var v=Vector3.Normalize(Vector3.Cross(n,u))*r;
                for(int i=0;i<32;i++){float a=i*(float)Math.PI/16,b=(i+1)*(float)Math.PI/16;line(p+u*(float)Math.Cos(a)+v*(float)Math.Sin(a),p+u*(float)Math.Cos(b)+v*(float)Math.Sin(b),c,w);}
            };
            lock(ClxnProject.Sync) {
                foreach(var stroke in ClxnProject.Strokes)for(int i=1;i<stroke.Points.Count;i++)line(stroke.Points[i-1],stroke.Points[i],stroke.Colour,stroke.Width);
                foreach(var pin in ClxnProject.Pins){if(clxnWalking&&pin.Id==clxnWalkActorId)continue;var p=pin.Position;float r=ClxnWorldPixel(p)*9;
                    ring(p,Vector3.UnitZ,r,pin.Colour,2.5f);line(p,p+Vector3.UnitZ*r*3,pin.Colour,3);
                    if(pin.Id==clxnSelectedId)ring(p,Vector3.UnitZ,r*1.5f,unchecked((int)0xffffffff),2);
                }
            }
            if(ClxnTool!="move" && clxnPreviewHit && clxnPointerOnMap){var p=clxnPreviewPosition;float r=ClxnWorldPixel(p)*12;ring(p,clxnPreviewNormal,r,ClxnColour,2);line(p-clxnPreviewNormal*r*.5f,p+clxnPreviewNormal*r*.5f,ClxnColour,2);}
            if(ClxnTool=="move"&&!clxnWalking){clxnCachedAnnotations=triangles;clxnCachedCamera=camera.Position;clxnCachedFov=camera.FieldOfView;clxnCachedHeight=camera.Height;clxnCachedOrtho=camera.IsOrthographic||camera.IsMapView?camera.OrthographicSize:0;clxnCachedSelection=clxnSelectedId;clxnAnnotationsDirty=false;}else clxnAnnotationsDirty=true;
            if(triangles.Count>0)Renderer.RenderAnnotationTriangles(triangles);
            ClxnUpdateProjectedPins();
        }
    }
}



