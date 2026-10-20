using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Color = System.Drawing.Color;

namespace CodeWalker
{
    internal static class ClxnStyle
    {
        internal static bool Dark=ClxnTheme.Load();
        internal static Color Background=>Dark?Color.FromArgb(14,17,23):Color.FromArgb(235,237,241);
        internal static Color Surface=>Dark?Color.FromArgb(25,29,37):Color.FromArgb(250,251,253);
        internal static Color Border=>Dark?Color.FromArgb(53,61,75):Color.FromArgb(211,217,226);
        internal static Color Accent=>Dark?Color.FromArgb(226,232,244):Color.FromArgb(30,35,45);
        internal static Color Muted=>Dark?Color.FromArgb(150,160,179):Color.FromArgb(105,115,133);
        internal static Color Text=>Dark?Color.FromArgb(234,239,249):Color.FromArgb(24,31,43);
        internal static Color Mint=>Muted;
        internal static Color OnAccent=>Dark?Color.FromArgb(20,25,34):Color.White;
        internal static Color Hover=>Dark?Color.FromArgb(47,56,71):Color.FromArgb(224,230,240);
        internal static void Apply(Form f) {
            f.Icon=ClxnBrand.Icon;f.BackColor=Background; f.ForeColor=Text; f.Font=new Font("Segoe UI",10);
            f.StartPosition=FormStartPosition.CenterScreen; f.AutoScaleMode=AutoScaleMode.Dpi;
        }
        internal static Label Label(string text,int size=10,Color? colour=null) => new Label { Text=text,BackColor=Color.Transparent,AutoSize=true,Font=new Font("Segoe UI",size),ForeColor=colour??Text,Margin=new Padding(0,6,0,6) };
        internal static Button Button(string title,EventHandler click,int width=260,bool accent=false) {
            var b=new ClxnActionButton { Text=title,Width=width,Height=40,FlatStyle=FlatStyle.Flat,BackColor=accent?Accent:Surface,ForeColor=accent?OnAccent:Text,Cursor=Cursors.Hand,Margin=new Padding(0,4,8,4),TextAlign=ContentAlignment.MiddleCenter };
            b.FlatAppearance.BorderColor=accent?Accent:Border; b.FlatAppearance.MouseOverBackColor=accent?Color.FromArgb(188,174,255):Color.FromArgb(39,46,64); b.Click+=click; return b;
        }
        internal static TextBox Input(string text="",int width=260) => new TextBox { Text=text,Width=width,BackColor=Surface,ForeColor=Text,BorderStyle=BorderStyle.FixedSingle,Margin=new Padding(0,4,0,8) };
    }
    internal sealed class ClxnHero : Panel
    {
        readonly Timer motion=new Timer{Interval=40};float phase;
        internal ClxnHero() { DoubleBuffered=true;motion.Tick+=(s,e)=>{if(Visible&&ClxnMotion.Enabled&&(FindForm()?.ContainsFocus??false)){phase+=.025f;Invalidate();}};motion.Start(); }
        protected override void Dispose(bool disposing){if(disposing)motion.Dispose();base.Dispose(disposing);}
        protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width>30&&Height>30)using(var path=ClxnShape.Round(Width,Height,26)){var old=Region;Region=new Region(path);old?.Dispose();}}
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e); var g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var gradient=new LinearGradientBrush(ClientRectangle,ClxnStyle.Surface,ClxnStyle.Background,20))g.FillRectangle(gradient,ClientRectangle);
            using(var pen=new Pen(Color.FromArgb(40,ClxnStyle.Muted),1)) {
                for(int i=0;i<17;i++) {
                    var pts=new PointF[90]; for(int j=0;j<90;j++)pts[j]=new PointF(Width*0.88f+j*9,25+i*17+(float)Math.Sin(j*0.13+i*0.25+phase)*34);
                    g.DrawCurve(pen,pts);
                }
            }
            using(var pen=new Pen(ClxnStyle.Mint,2)) { g.DrawEllipse(pen,Width-135,75+(float)Math.Sin(phase)*10,18,18); g.DrawLine(pen,Width-126,54,Width-126,70); g.DrawLine(pen,Width-126,98,Width-126,114); }
        }
    }
    internal sealed class ClxnHomeForm : Form
    {
        internal static string DataFolder=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"666clxn");
        internal ClxnHomeForm() {
            Text="666clxn | Scene Studio 0.22"; ClientSize=new Size(1080,760); MinimumSize=new Size(940,720); ClxnStyle.Apply(this);
            var root=new TableLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(32,22,32,24),ColumnCount=1,RowCount=4 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,45)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,230)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,53)); Controls.Add(root);
            var nav=new Panel { Dock=DockStyle.Fill }; nav.Controls.Add(new Label { Text="666clxn   /   SCENE STUDIO",Font=new Font("Segoe UI",12,FontStyle.Bold),AutoSize=true,Location=new Point(0,3) });
            var theme=ClxnStyle.Button("◐  Тема",(s,e)=>ClxnTheme.Toggle(),105);theme.Dock=DockStyle.Right;nav.Controls.Add(theme);
            var version=ClxnStyle.Label("●  LOCAL WORKSPACE   /   v0.22",9,ClxnStyle.Mint); version.Location=new Point(385,9); nav.Controls.Add(version); root.Controls.Add(nav,0,0);
            var hero=new ClxnHero { Dock=DockStyle.Fill,Padding=new Padding(25,18,25,10) }; root.Controls.Add(hero,0,1);
            hero.Controls.Add(new Label { Text="Собери сцену.\nПоставь камеру.",Font=new Font("Segoe UI",30,FontStyle.Bold),AutoSize=true,Location=new Point(25,19),BackColor=Color.Transparent });
            hero.Controls.Add(new Label { Text="Персонажи, рисунки и пролётки в одном проекте.",AutoSize=true,ForeColor=ClxnStyle.Muted,Location=new Point(29,146),BackColor=Color.Transparent });
            var open=ClxnStyle.Button("ОТКРЫТЬ МИР GTA V   →",(s,e)=>Launch("world"),280,true); open.Location=new Point(29,178); hero.Controls.Add(open);
            var socials=new FlowLayoutPanel{Size=new Size(350,48),WrapContents=false,BackColor=ClxnStyle.Background,Padding=new Padding(2,0,0,0)};hero.Controls.Add(socials);
            Action placeSocials=()=>socials.Location=new Point(hero.ClientSize.Width-375,174);hero.Resize+=(s,e)=>placeSocials();placeSocials();
            socials.Controls.Add(ClxnStyle.Button("Discord клана ↗",(s,e)=>ClxnLinks.Open(this,ClxnLinks.Discord),162));socials.Controls.Add(ClxnStyle.Button("YouTube · Roltik ↗",(s,e)=>ClxnLinks.Open(this,ClxnLinks.YouTube),170));
            var cards=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(0,18,0,6) }; cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); cards.RowStyles.Add(new RowStyle(SizeType.Percent,50)); cards.RowStyles.Add(new RowStyle(SizeType.Percent,50)); root.Controls.Add(cards,0,2);
            AddCard(cards,0,0,"01  /  LIVE 3D","Рисовать в 3D-мире","Метки и кисть прямо на поверхности карты",()=>Launch("world"));
            AddCard(cards,1,0,"02  /  WORLD EDITOR","Редактор YMAP","Объекты, сцены и проекты CodeWalker",()=>Launch("project"));
            AddCard(cards,0,1,"03  /  GAME FILES","Архивы RPF","Просмотр игровых ресурсов",()=>Launch("explorer"));
            AddCard(cards,1,1,"04  /  CINEMATICS","Пролётки камеры","Кадры · F4, воспроизведение · F5",()=>Launch("world"));
            var foot=new Panel { Dock=DockStyle.Fill }; foot.Controls.Add(ClxnStyle.Label("1  Обзор     /     2  Кисть     /     3  Метка     /     4  Персонаж     /     F2  Свернуть интерфейс",9,ClxnStyle.Muted));
            var credit=ClxnStyle.Label("На базе CodeWalker by dexyfex • файлы GTA V загружаются локально",9,ClxnStyle.Muted); credit.Top=27; foot.Controls.Add(credit); root.Controls.Add(foot,0,3);ClxnTheme.Apply(this);
        }
        void AddCard(TableLayoutPanel host,int col,int row,string tag,string title,string description,Action action) {
            var panel=new ClxnCard { Dock=DockStyle.Fill,BackColor=ClxnStyle.Surface,Margin=new Padding(col==0?0:9,6,col==0?9:0,6),Padding=new Padding(20,12,20,12),Cursor=Cursors.Hand };

            var t=ClxnStyle.Label(tag,9,ClxnStyle.Accent); t.Location=new Point(20,12);
            var name=new Label { Text=title,BackColor=Color.Transparent,AutoSize=true,Font=new Font("Segoe UI",17,FontStyle.Bold),Location=new Point(17,39) };
            var sub=ClxnStyle.Label(description,10,ClxnStyle.Muted); sub.Location=new Point(20,78);
            panel.Controls.AddRange(new Control[]{t,name,sub}); panel.Click+=(s,e)=>action(); foreach(Control c in panel.Controls)c.Click+=(s,e)=>action();host.Controls.Add(panel,col,row);
        }
        void Launch(string mode) { try { Process.Start(new ProcessStartInfo(Application.ExecutablePath,mode){ WorkingDirectory=AppDomain.CurrentDomain.BaseDirectory,UseShellExecute=false }); }catch(Exception ex){MessageBox.Show(this,ex.Message,"666clxn");} }
    }
}


