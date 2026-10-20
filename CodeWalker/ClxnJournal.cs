using System;
using System.Collections.Generic;
namespace CodeWalker {
 internal sealed class ClxnJournal {
  readonly List<string> lines=new List<string>();readonly object sync=new object();string last;
  internal int Revision {get;private set;}
  internal void Add(string message){if(string.IsNullOrWhiteSpace(message))return;lock(sync){if(message==last)return;last=message;lines.Add(DateTime.Now.ToString("HH:mm:ss")+"  "+message);if(lines.Count>200)lines.RemoveAt(0);Revision++;}}
  internal string[] Snapshot(){lock(sync)return lines.ToArray();}
  internal void Clear(){lock(sync){lines.Clear();last=null;Revision++;}}
 }
}
