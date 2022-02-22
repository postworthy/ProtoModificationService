using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ProtoModificationService.Common
{
    public static class ProcessHelper
    {
        public static async Task<string> RunCommand(string cmd, string args)
        {
            var tcs = new TaskCompletionSource<int>();
            var p = new Process() { EnableRaisingEvents = true };
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = cmd,
                Arguments = args,
                RedirectStandardError = true,
                //RedirectStandardInput = true,
                RedirectStandardOutput = true
            };
            p.Exited += (s, a) =>
            {
                tcs.SetResult(p.ExitCode);
            };
            p.Start();
            await tcs.Task;
            var results = p.StandardOutput.ReadToEnd();
            p.Dispose();

            return results;
        }

        public static async Task<byte[]> RunCommand_BinaryResults(string cmd, string args)
        {
            var tcs = new TaskCompletionSource<int>();
            var p = new Process() { EnableRaisingEvents = true };
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = cmd,
                Arguments = args,
                RedirectStandardError = true,
                //RedirectStandardInput = true,
                RedirectStandardOutput = true
            };
            p.Exited += (s, a) =>
            {
                tcs.SetResult(p.ExitCode);
            };
            p.Start();
            await tcs.Task;
            byte[] data = null;
            int lastRead = 0;
            var baseStream = p.StandardOutput.BaseStream as FileStream;
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buffer = new byte[4096];
                do
                {
                    lastRead = baseStream.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, lastRead);
                } while (lastRead > 0);

                data = ms.ToArray();
            }
            p.Dispose();

            return data;
        }
    }
}
