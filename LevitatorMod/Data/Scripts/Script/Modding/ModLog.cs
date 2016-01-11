/*
* Levitator's Space Engineers Modding Library
* Log File Class
*
* It buffers all writes so that you can log events prior to ModAPIGateway initialization
* It has the special property that it can be Init()ed again after it has been Disposed()
* to accomodate SessionComponent initialization problems
*
* Reuse is free as long as you attribute the author.
*
* V1.0
*
*/

using System;
using System.IO;
using System.Text;

using Sandbox.Common;
using Sandbox.ModAPI;
using Levitator.SE.Utility;

namespace Levitator.SE.Modding
{
    public interface IModLog : IDisposable
    {        
        void Init();
        void Log(string prefix, Exception x);
        void Log(string message, bool display = false, MyFontEnum font = MyFontEnum.White);
    }

    public class ModLog : IModLog
    {
        private TextWriter Writer;
        private string FileName;
        private Type Scope;
        private string Prefix;
        private StringBuilder Backlog = new StringBuilder();
        private StringBuilder NewMessage = new StringBuilder();
        
        public void Init()
        {            
            try {
                Writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(FileName, Scope);
                Flush();             
            }
            catch(Exception x)
            {
                Log("Failed opening log " + FileName, x);
                throw x;
            }            
        }

        public ModLog(string name, Type type, string prefix)
        {
            FileName = name;
            Scope = type;
            Prefix = prefix;
        }
      
		private void BufferMessage(string message)
		{
			NewMessage.Clear();
			NewMessage.Append(DateTime.Now.ToString());
			NewMessage.Append(": ");
			NewMessage.Append(message);
			NewMessage.Append("\r\n");
			Backlog.Append(NewMessage);
		}

        //Write a buffered log message and optionally display it
        public void Log(string message, bool display = false, MyFontEnum font = MyFontEnum.White)
        {
			BufferMessage(message);
            Flush();
            			
            if (null != Writer && display)
            {                
                try {
                    NewMessage.Clear();
                    NewMessage.Append(Prefix);
                    NewMessage.Append(": ");
                    NewMessage.Append(message);
                    
					ModBase.ShowNotification(Util.WrapText(NewMessage.ToString(), 100), font);
                }
                catch(Exception x)
                {
                    Log("Failed to display previous: " + x, false);
                }
            }                        
        }

        public void Log(string prefix, Exception x)
        {
            bool Display = null != Writer;            
            Log(prefix + ": " + x, Display, MyFontEnum.ErrorMessageBoxText);
        }
		
		public static string DestinationString(Network.Destination dest){

			var player = dest.GetPlayer();

			if (null == player)
				return dest.ToString();
			else
				return string.Format("{0}/{1}", player.DisplayName, dest.ToString());			
		}
				
        private void Flush()
        {
			try {
				if (null != Writer)
				{
					Writer.Write(Backlog);
					Writer.Flush();
					Backlog.Clear();
				}
			}
			catch(Exception x)
			{
				BufferMessage("ModLog.Flush(): " + x);
			}     
        }

        public void Dispose()
        {
            ((IDisposable)Writer).Dispose();
            Writer = null;
        }
    }
}
