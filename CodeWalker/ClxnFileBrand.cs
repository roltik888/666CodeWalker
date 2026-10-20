using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Win32;
namespace CodeWalker {
 internal static class ClxnFileBrand {
  internal static readonly string[] Extensions={".clxn",".cwproj",".ymap",".ytyp",".ydr",".ydd",".ytd",".yft",".ybn",".ycd",".ynv",".ynd",".ymt",".rpf"};
  [DllImport("shell32.dll")]static extern void SHChangeNotify(uint id,uint flags,IntPtr item1,IntPtr item2);
  internal static void Install(){
   string exe=Application.ExecutablePath;string icon="\""+exe+"\",0";var backup=new XElement("iconBackup",new XAttribute("application",exe));var writes=new Dictionary<string,string>();
   foreach(var ext in Extensions){string prog="666clxn"+ext;writes[prog]="666 CLXN · "+ext;writes[prog+"\\DefaultIcon"]=icon;writes[prog+"\\shell\\open\\command"]="\""+exe+"\" --open-file=\"%1\"";
    using(var key=Registry.ClassesRoot.OpenSubKey(ext)){var current=key?.GetValue("") as string;if(string.IsNullOrWhiteSpace(current)){writes[ext]=prog;}else if(ext==".clxn"||ext==".cwproj"||current.IndexOf("CodeWalker",StringComparison.OrdinalIgnoreCase)>=0||current.StartsWith("666clxn",StringComparison.OrdinalIgnoreCase)){writes[current+"\\DefaultIcon"]=icon;}}
    writes[ext+"\\DefaultIcon"]=icon;
   }
   foreach(var item in writes){using(var old=Registry.CurrentUser.OpenSubKey("Software\\Classes\\"+item.Key)){var value=old?.GetValue("") as string;backup.Add(new XElement("value",new XAttribute("key",item.Key),new XAttribute("existed",value!=null),value??""));}}
   Directory.CreateDirectory(ClxnHomeForm.DataFolder);var path=Path.Combine(ClxnHomeForm.DataFolder,"file-icons-backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmssfff")+".xml");backup.Save(path);
   foreach(var item in writes)using(var key=Registry.CurrentUser.CreateSubKey("Software\\Classes\\"+item.Key))key.SetValue("",item.Value,RegistryValueKind.String);
   foreach(var ext in Extensions)using(var key=Registry.CurrentUser.CreateSubKey("Software\\Classes\\"+ext+"\\OpenWithProgids"))key.SetValue("666clxn"+ext,new byte[0],RegistryValueKind.None);
   SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);
  }
 }
}
