using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace FluidSpirv {
    public class SpirvBuilder : Task {
        [Required]
        public string InFile { get; set; }

        [Required]
        public string OutFile { get; set; }

        public enum ShaderStage {
            Auto,
            Fragment,
            Vertex,
            Geometry,
            Compute,
            TesC,
            TesE
        }

        public ShaderStage ShaderType {
            get {
                ShaderStage val;
                if (Enum.TryParse(ShaderTypeStr, out val)) {
                    return val;
                } else {
                    return ShaderStage.Auto;
                }
            }
        }

        [Required]
        public string ShaderTypeStr { get; set; }

        public bool Verbose { get; set; }

        public bool Vulkan { get; set; }

        public override bool Execute() {
            Process p = new Process();
            p.StartInfo.FileName = @"glslangValidator.exe";
            string args = "";

            switch (ShaderType) {
                case ShaderStage.Fragment:
                    args += "-S frag ";
                    break;

                case ShaderStage.Vertex:
                    args += "-S vert ";
                    break;

                case ShaderStage.Geometry:
                    args += "-S geom ";
                    break;

                case ShaderStage.Compute:
                    args += "-S comp ";
                    break;

                case ShaderStage.TesC:
                    args += "-S tesc ";
                    break;

                case ShaderStage.TesE:
                    args += "-S tese ";
                    break;

                default:
                    break;
            }

            if (Vulkan) {
                args += "-V ";
            } else {
                args += "-G ";
            }
            // TODO: Switch between Vulkan and OpenGL processing

            // TODO: Filename should be properly escaped
            args += " \"" + InFile + "\" ";
            args += " -o \"" + OutFile + "\" ";

            p.StartInfo.Arguments = args;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;

            Log.LogMessage(MessageImportance.High, "Running {0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);

            if (!p.Start()) {
                Log.LogError("Failed to start tool {0}", p.StartInfo.FileName);
                return false;
            }

            /*while (!p.HasExited) {
                string stdOut = "";
                string stdErr = "";

                while (p.StandardOutput.Peek() >= 0) {
                    stdOut += (char)p.StandardOutput.Read();
                }

                if (stdOut.Length > 0) {
                    Log.LogMessage(MessageImportance.High, stdOut);
                }

                while (p.StandardError.Peek() >= 0) {
                    stdErr += (char)p.StandardError.Read();
                }

                if (stdErr.Length > 0) {
                    Log.LogError(null, null, null, Filename,
                        -1, -1, -1, -1, stdErr);
                }

                Thread.Yield();
            }*/
            p.WaitForExit();

            Regex errorRgx = new Regex(@"^ERROR:\s+(.+):(\d+): (.*)$");

            //while (p.StandardError.Peek() >= 0) {
            while (p.StandardOutput.Peek() >= 0) {
                string line = p.StandardOutput.ReadLine();

                Match m = errorRgx.Match(line);
                if (m.Success) {
                    for (int j = 0; j < m.Groups.Count; j++) {
                        Capture c = m.Groups[j].Captures[0];
                        Log.LogMessage(MessageImportance.High, "Capture" + j + "='" + c + "', Position=" + c.Index);
                    }


                    string srcFile = m.Groups[1].Captures[0].Value;
                    int lineNum = int.Parse(m.Groups[2].Captures[0].Value);
                    //int colNum = int.Parse(m.Groups[1].Captures[0].Value);
                    Log.LogMessage(MessageImportance.High, "<{0}, {1}>", lineNum, 0);
                    string message = m.Groups[3].Captures[0].Value;
                    Log.LogError(null, null, null, srcFile,
                        lineNum, 0, 0, 0, message);
                } else if (line.StartsWith("ERROR")) {
                    Log.LogError(null, null, null, InFile,
                        0, 0, 0, 0, line);
                }
            }

            if (p.ExitCode != 0 && !Log.HasLoggedErrors) {
                Log.LogError(null, null, null, InFile,
                    -1, -1, -1, -1, "Non-zero exit code: {0}", p.ExitCode.ToString());
            }

            return !Log.HasLoggedErrors;
        }
    }
}
