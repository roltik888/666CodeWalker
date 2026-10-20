namespace CodeWalker {
 internal static class ClxnShortcuts {
  internal static string ToolForKey(int keyData){if((keyData&unchecked((int)0xffff0000))!=0)return null;switch(keyData){case 49:case 97:return "move";case 50:case 98:return "draw";case 51:case 99:return "pin";case 52:case 100:return "actor";default:return null;}}
 }
}
