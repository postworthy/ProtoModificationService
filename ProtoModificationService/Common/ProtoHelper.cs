using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProtoModificationService.Common
{
    public class ProtoHelper
    {
        const string ERROR_RESULT = "ERROR";
        static List<string> globalOutput = new List<string>();
        static Dictionary<string, string> protos = new Dictionary<string, string>();
        static Dictionary<string, string> fields = new Dictionary<string, string>();

        public async Task<byte[]> Run(string rawDataFilePath, IEnumerable<ModifyObj> mos = null)
        {
            globalOutput = new List<string>();
            protos = new Dictionary<string, string>();
            fields = new Dictionary<string, string>();

            string outputProtoFilePath = Path.GetTempFileName();

            if (!File.Exists(rawDataFilePath))
                throw new ArgumentException("RAW_DATA_FILE must point to a raw data file.");

            if (!Directory.Exists(Path.GetPathRoot(outputProtoFilePath)))
                throw new ArgumentException("Output directory does not exist.");

            await ProcessHelper.RunCommand($"chmod", "+x /app/protoc.sh");
            var result = await ProcessHelper.RunCommand($"bash", $"-c \"/app/protoc.sh '{rawDataFilePath}'\"");


            globalOutput.Add("syntax = \"proto3\";");
            globalOutput.Add("");
            using (var ms = new MemoryStream(UTF8Encoding.UTF8.GetBytes(result)))
            using (var sr = new StreamReader(ms))
            {
                var (_, outputStr) = getProto(sr, "0");
                foreach (var key in protos.Keys)
                {
                    globalOutput.Add(protos[key]);
                }
                globalOutput.Add(outputStr);
                var o = string.Join(Environment.NewLine, globalOutput);
                await File.WriteAllTextAsync(outputProtoFilePath, o);
                if (mos == null)
                    return UTF8Encoding.UTF8.GetBytes(o);
            }

            if (mos != null)
            {
                var transformedResult = await ProcessHelper.RunCommand($"bash", $"-c \"/app/protoc.sh '{rawDataFilePath}' '{Path.GetPathRoot(outputProtoFilePath)}' '{outputProtoFilePath}'\"");
                transformedResult = Regex.Replace(transformedResult, "\\n([0-9]+)\\s{(.)+}", "", RegexOptions.Singleline);
                var orig = transformedResult;
                foreach (var mo in mos)
                {
                    transformedResult = transformedResult.Replace(mo.Find, mo.Replace);
                }
                if (orig != transformedResult)
                {
                    var tempFile = Path.GetTempFileName();
                    using (StreamWriter sw = new StreamWriter(tempFile))
                    {
                        sw.Write(transformedResult);
                    }
                    var binaryResults = await ProcessHelper.RunCommand_BinaryResults($"bash", $"-c \"/app/protoc.sh '{tempFile}' '{Path.GetPathRoot(outputProtoFilePath)}' '{outputProtoFilePath}' 'encode'\"");
                    await File.WriteAllBytesAsync(outputProtoFilePath + ".bin", binaryResults);
                    return binaryResults;
                }
                else
                {
                    return File.ReadAllBytes(rawDataFilePath);
                }
            }

            return null;
        }

        private (string, string) getProto(StreamReader sr, string protoNum)
        {
            var output = new List<string>();
            var protoName = "MyProto_" + protoNum;
            output.Add("message " + protoName + " {");
            var line = "";
            var containsHex = false;
            while ((line = sr.ReadLine()) != null && line != "")
            {
                var newProto = line.Trim().Split(new char[] { ' ' }).Skip(1).Take(1).FirstOrDefault() == "{";
                var closeProto = line.Trim() == "}";
                if (!newProto && !closeProto)
                {
                    var fieldNum = line.Trim().Split(":")[0];
                    var fieldName = "prop_" + protoNum + "_" + fieldNum;
                    if (!fields.ContainsKey(fieldName))
                        fields.Add(fieldName, fieldName);
                    else
                    {
                        for (int i = 0; i < output.Count; i++)
                        {
                            if (output[i].EndsWith(fieldNum + ";") && !output[i].StartsWith("repeated"))
                            {
                                output[i] = "repeated " + output[i];
                                break;
                            }
                        }
                        continue;
                    }
                    var data = string.Join(":", line.Split(":").Skip(1)).Trim();
                    if (data.StartsWith("\""))
                        output.Add("string " + fieldName + " = " + fieldNum + ";");
                    else if (Int32.TryParse(data, out Int32 _))
                        output.Add("int32 " + fieldName + " = " + fieldNum + ";");
                    else if (Int64.TryParse(data, out Int64 _))
                        output.Add("int64 " + fieldName + " = " + fieldNum + ";");
                    else
                    {
                        if (data.Trim().StartsWith("0x"))
                            containsHex = true;
                        //output.Add("int64 " + fieldName + " = " + fieldNum + ";");
                        //throw new Exception("Unknown:" + line);
                        //output.AddLine("unknown prop_" + propCount++);
                    }
                }
                else if (newProto)
                {
                    var needField = true;
                    var needProto = true;
                    var fieldNum = line.Trim().Split(new char[] { ' ' })[0];
                    var fieldName = "prop_" + protoNum + "_" + fieldNum;
                    var protoPostfix = protoNum + "_" + fieldNum;
                    var newProtoName = "MyProto_" + protoPostfix;

                    if (!protos.ContainsKey(newProtoName))
                        protos.Add(newProtoName, "");
                    else
                        needProto = false;


                    if (!fields.ContainsKey(fieldName))
                        fields.Add(fieldName, fieldName);
                    else
                    {

                        for (int i = 0; i < output.Count; i++)
                        {
                            if (output[i].Contains(fieldName) && !output[i].StartsWith("repeated"))
                            {
                                output[i] = "repeated " + output[i];
                                break;
                            }
                        }

                        needField = false;
                    }
                    var (name, outputStr) = getProto(sr, protoPostfix);
                    if (outputStr == ERROR_RESULT)
                    {

                    }
                    else
                    {
                        if (needProto)
                            protos[newProtoName] = outputStr;
                        else if (protos[newProtoName].Length < outputStr.Length)
                            protos[newProtoName] = outputStr;

                        if (needField) output.Add(newProtoName + " " + fieldName + " = " + fieldNum + ";");
                    }
                }
                else if (closeProto)
                {
                    output.Add("}");
                    return (protoName, !containsHex ? string.Join(Environment.NewLine, output) : ERROR_RESULT);
                }
            }
            output.Add("}");
            return (protoName, !containsHex ? string.Join(Environment.NewLine, output) : ERROR_RESULT);
        }
    }
}
