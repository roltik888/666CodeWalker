using CodeWalker.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shell;

namespace CodeWalker
{
    static class Program
    {
        internal static string AnnotationPath,ExportVideoPath,OpenFilePath;
        internal static int ExportWidth=1280,ExportHeight=720,ExportFps=30;
        internal static bool ExitAfterExport;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            if(args.Contains("--register-file-icons")){try{ClxnFileBrand.Install();}catch{Environment.ExitCode=1;}return;}
            bool menumode = false;
            bool explorermode = false;
            bool projectmode = false;
            bool vehiclesmode = false;
            bool pedsmode = false;
            if ((args != null) && (args.Length > 0))
            {
                foreach (string arg in args)
                {
                    if (arg.StartsWith("--gta-folder=", StringComparison.OrdinalIgnoreCase)) {
                        string folder=arg.Substring(13);
                        Settings.Default.GTAFolder=folder;
                        Settings.Default.GTAGen9=File.Exists(Path.Combine(folder,"GTA5_Enhanced.exe"));
                    }
                    if (arg.StartsWith("--annotations=", StringComparison.OrdinalIgnoreCase)) AnnotationPath=Path.GetFullPath(arg.Substring(14));
                    if(arg.StartsWith("--export-mp4=",StringComparison.OrdinalIgnoreCase))ExportVideoPath=Path.GetFullPath(arg.Substring("--export-mp4=".Length));
                    if(arg.StartsWith("--export-width=",StringComparison.OrdinalIgnoreCase))int.TryParse(arg.Substring("--export-width=".Length),out ExportWidth);
                    if(arg.StartsWith("--export-height=",StringComparison.OrdinalIgnoreCase))int.TryParse(arg.Substring("--export-height=".Length),out ExportHeight);
                    if(arg.StartsWith("--export-fps=",StringComparison.OrdinalIgnoreCase))int.TryParse(arg.Substring("--export-fps=".Length),out ExportFps);
                    if(arg=="--exit-after-export")ExitAfterExport=true;
                    if(arg.StartsWith("--open-file=",StringComparison.OrdinalIgnoreCase))OpenFilePath=Path.GetFullPath(arg.Substring("--open-file=".Length));
                    string argl = arg.ToLowerInvariant();
                    if (argl == "menu")
                    {
                        menumode = true;
                    }
                    if (argl == "explorer")
                    {
                        explorermode = true;
                    }
                    if (argl == "project")
                    {
                        projectmode = true;
                    }
                    if (argl == "vehicles")
                    {
                        vehiclesmode = true;
                    }
                    if (argl == "peds")
                    {
                        pedsmode = true;
                    }
                }
            }

            try {
                var savedFolder=Path.Combine(ClxnHomeForm.DataFolder,"game-folder.txt");
                if(string.IsNullOrWhiteSpace(Settings.Default.GTAFolder) && File.Exists(savedFolder)) {
                    var folder=File.ReadAllText(savedFolder).Trim();
                    if(File.Exists(Path.Combine(folder,"GTA5.exe")) || File.Exists(Path.Combine(folder,"GTA5_Enhanced.exe"))) {
                        Settings.Default.GTAFolder=folder;
                        Settings.Default.GTAGen9=File.Exists(Path.Combine(folder,"GTA5_Enhanced.exe"));
                    }
                }
            } catch { }

            if(OpenFilePath!=null){if(!File.Exists(OpenFilePath)){MessageBox.Show("Файл не найден: "+OpenFilePath,"666 CLXN");return;}var extension=Path.GetExtension(OpenFilePath).ToLowerInvariant();if(extension==".clxn")AnnotationPath=OpenFilePath;else if(extension==".rpf")explorermode=true;else projectmode=true;}
            EnsureJumpList();

            //Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);ClxnBrand.WatchWindows();

            if (args.Length == 0)
            {
                Application.Run(new ClxnHomeForm());
                return;
            }


            // Always check the GTA folder first thing
            if (!GTAFolder.UpdateGTAFolder(Properties.Settings.Default.RememberGTAFolder))
            {
                MessageBox.Show("Could not load CodeWalker because no valid GTA 5 folder was selected. CodeWalker will now exit.", "GTA 5 Folder Not Found", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            try {
                Directory.CreateDirectory(ClxnHomeForm.DataFolder);
                File.WriteAllText(Path.Combine(ClxnHomeForm.DataFolder,"game-folder.txt"),GTAFolder.CurrentGTAFolder);
            } catch { }
#if !DEBUG
            try
            {
#endif
                if(OpenFilePath!=null&&Path.GetExtension(OpenFilePath).Equals(".ycd",StringComparison.OrdinalIgnoreCase)){var resource=new GameFiles.YcdFile();GameFiles.RpfFile.LoadResourceFile(resource,File.ReadAllBytes(OpenFilePath),46);resource.Name=Path.GetFileName(OpenFilePath);var viewer=new Forms.YcdForm{FilePath=OpenFilePath};viewer.LoadYcd(resource);Application.Run(viewer);}
                else if (menumode)
                {
                    Application.Run(new MenuForm());
                }
                else if (explorermode)
                {
                    if(OpenFilePath!=null){var parent=Path.GetDirectoryName(OpenFilePath)+Path.DirectorySeparatorChar;var gameRoot=GTAFolder.GetCurrentGTAFolderWithTrailingSlash();if(OpenFilePath.StartsWith(gameRoot,StringComparison.OrdinalIgnoreCase))Settings.Default.RPFExplorerStartFolder=OpenFilePath.Substring(gameRoot.Length);else{var existing=Settings.Default.RPFExplorerExtraFolders??"";if(!existing.Split('\n').Any(p=>p.Trim().Equals(parent,StringComparison.OrdinalIgnoreCase)))Settings.Default.RPFExplorerExtraFolders=existing+"\n"+parent;Settings.Default.RPFExplorerStartFolder=OpenFilePath;}}Application.Run(new ExploreForm());
                }
                else if (projectmode)
                {
                    var project=new Project.ProjectForm();if(OpenFilePath!=null)project.Shown+=(s,e)=>{if(Path.GetExtension(OpenFilePath).Equals(".cwproj",StringComparison.OrdinalIgnoreCase))project.OpenProject(OpenFilePath);else project.OpenFiles(new[]{OpenFilePath});};Application.Run(project);
                }
                else if (vehiclesmode)
                {
                    Application.Run(new VehicleForm());
                }
                else if (pedsmode)
                {
                    Application.Run(new PedsForm());
                }
                else
                {
                    Application.Run(new WorldForm());
                }
#if !DEBUG
            }
            catch (Exception ex)
            {
                try {
                    Directory.CreateDirectory(ClxnHomeForm.DataFolder);
                    File.AppendAllText(Path.Combine(ClxnHomeForm.DataFolder, "errors.log"), DateTime.Now.ToString("O") + " " + ex + Environment.NewLine);
                } catch { }
                MessageBox.Show("An unexpected error was encountered!\n" + ex.ToString());
                //this can happen if folder wasn't chosen, or in some other catastrophic error. meh.
            }
#endif
        }


        static void EnsureJumpList()
        {
            if (Settings.Default.JumpListInitialised) return;

            try
            {
                var cwpath = Assembly.GetEntryAssembly().Location;
                var cwdir = Path.GetDirectoryName(cwpath);

                var jtWorld = new JumpTask();
                jtWorld.ApplicationPath = cwpath;
                jtWorld.IconResourcePath = cwpath;
                jtWorld.WorkingDirectory = cwdir;
                jtWorld.Arguments = "";
                jtWorld.Title = "World View";
                jtWorld.Description = "Display the GTAV World";
                jtWorld.CustomCategory = "Launch Options";

                var jtExplorer = new JumpTask();
                jtExplorer.ApplicationPath = cwpath;
                jtExplorer.IconResourcePath = Path.Combine(cwdir, "CodeWalker RPF Explorer.exe");
                jtExplorer.WorkingDirectory = cwdir;
                jtExplorer.Arguments = "explorer";
                jtExplorer.Title = "RPF Explorer";
                jtExplorer.Description = "Open RPF Explorer";
                jtExplorer.CustomCategory = "Launch Options";

                var jtVehicles = new JumpTask();
                jtVehicles.ApplicationPath = cwpath;
                jtVehicles.IconResourcePath = Path.Combine(cwdir, "CodeWalker Vehicle Viewer.exe");
                jtVehicles.WorkingDirectory = cwdir;
                jtVehicles.Arguments = "vehicles";
                jtVehicles.Title = "Vehicle Viewer";
                jtVehicles.Description = "Open Vehicle Viewer";
                jtVehicles.CustomCategory = "Launch Options";

                var jtPeds = new JumpTask();
                jtPeds.ApplicationPath = cwpath;
                jtPeds.IconResourcePath = Path.Combine(cwdir, "CodeWalker Ped Viewer.exe");
                jtPeds.WorkingDirectory = cwdir;
                jtPeds.Arguments = "peds";
                jtPeds.Title = "Ped Viewer";
                jtPeds.Description = "Open Ped Viewer";
                jtPeds.CustomCategory = "Launch Options";

                var jumpList = new JumpList();

                jumpList.JumpItems.Add(jtWorld);
                jumpList.JumpItems.Add(jtExplorer);
                jumpList.JumpItems.Add(jtVehicles);
                jumpList.JumpItems.Add(jtPeds);

                jumpList.Apply();

                Settings.Default.JumpListInitialised = true;
                Settings.Default.Save();
            }
            catch
            { }
        }
    }
}

