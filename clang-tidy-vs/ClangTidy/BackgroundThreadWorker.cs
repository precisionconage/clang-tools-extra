using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LLVM.ClangTidy
{
    internal class OutputEventArgs : EventArgs
    {
        public readonly string s_Output;

        public OutputEventArgs(string output)
        {
            s_Output = output;
        }
    }

    public class BackgroundThreadWorker
    {
        public event EventHandler ThreadDone;
        private string m_ExeName;
        private string m_Arguments;
        public BackgroundThreadWorker(String exe_name, String arguments)
        {
            m_ExeName = exe_name;
            m_Arguments = arguments;
        }
        public void Run()
        {
            string result = RunExternalExe(m_ExeName, m_Arguments);
            ThreadDone?.Invoke(this, new OutputEventArgs(result));
        }

        // Run External executable using background thread
        private string RunExternalExe(string filename, string arguments = null)
        {
            var process = new System.Diagnostics.Process();

            process.StartInfo.FileName = filename;
            if (!string.IsNullOrEmpty(arguments))
            {
                process.StartInfo.Arguments = arguments;
            }

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;

            var std_output = new StringBuilder();
            process.OutputDataReceived += (sender, args) => std_output.AppendLine(args.Data);

            string std_error = null;
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                std_error = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                throw new Exception("error while executing " + FormatErrorMsg(filename, arguments) + ": " + e.Message, e);
            }

            if (process.ExitCode == 0)
            {
                return FormatMsg(std_output.ToString());
            }
            else
            {
                var message = new StringBuilder();

                if (!string.IsNullOrEmpty(std_error))
                {
                    message.AppendLine(std_error);
                }

                if (std_output.Length != 0)
                {
                    message.AppendLine("Std output:");
                    message.AppendLine(std_output.ToString());
                }

                throw new Exception(FormatErrorMsg(filename, arguments) + 
                    " finished with exit code = " + process.ExitCode + ": " + message);
            }
        }

        private string FormatErrorMsg(string filename, string arguments)
        {
            return "'" + filename +
                ((string.IsNullOrEmpty(arguments)) ? string.Empty : " " + arguments) + "'";
        }

        private string FormatMsg(string message)
        {
            string pattern = @":(\d+):(\d+)";
            string replacement = "($1,$2)"; // Format output allowing auto navigation to source code line and column

            Regex rgx = new Regex(pattern);
            message = rgx.Replace(message, replacement);

            pattern = @".*(TPreprocessorGenerated).*(file not found).*\n.*\n.*\n";
            replacement = "";

            rgx = new Regex(pattern);
            message = rgx.Replace(message, replacement);

            pattern = @".*file not found.*\n#include( *)<.*>.*\n.*";
            replacement = "";

            rgx = new Regex(pattern);
            message = rgx.Replace(message, replacement);

            pattern = @"\nwarning: .*\n";
            replacement = "\n";

            rgx = new Regex(pattern);
            message = rgx.Replace(message, replacement);

            return message;
        }
    }
}
