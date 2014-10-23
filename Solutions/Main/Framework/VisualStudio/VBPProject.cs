//-----------------------------------------------------------------------
// <copyright file="VBPProject.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.VisualStudio.Extended
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;

    public class VBPProject
    {
        private readonly List<string> lines = new List<string>();
        private string projectFile;

        public VBPProject()
        {
        }

        public VBPProject(string projectFileExt)
        {
            this.ProjectFile = projectFileExt;
        }

        public string ProjectFile
        {
            get
            {
                return this.projectFile;
            }

            set
            {
                if (!File.Exists(value))
                {
                    throw new Exception("Project file name does not exist");
                }

                this.projectFile = value;
            }
        }

        public bool Load()
        {
            if (string.IsNullOrEmpty(this.ProjectFile))
            {
                return false;
            }

            StreamReader lineStream = null;
            try
            {
                lineStream = new StreamReader(this.projectFile, Encoding.Default);
                while (!lineStream.EndOfStream)
                {
                    this.lines.Add(lineStream.ReadLine());
                }
            }
            catch
            {
                // intended
            }
            finally
            {
                if (lineStream != null)
                {
                    lineStream.Close();
                }
            }

            return true;
        }

        public bool Save()
        {
            if (string.IsNullOrEmpty(this.projectFile) | this.lines.Count == 0)
            {
                return false;
            }

            StreamWriter lineStream = null;
            bool readOnly = false;
            try
            {
                if ((File.GetAttributes(this.projectFile) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    readOnly = true;
                    File.SetAttributes(this.projectFile, FileAttributes.Normal);
                }

                lineStream = new StreamWriter(this.projectFile, false, Encoding.Default);
                foreach (string line in this.lines)
                {
                    lineStream.WriteLine(line);
                }
            }
            catch
            {
                // intended
            }
            finally
            {
                if (lineStream != null)
                {
                    lineStream.Close();
                }

                if (readOnly)
                {
                    File.SetAttributes(this.projectFile, FileAttributes.ReadOnly);
                }
            }

            return true;
        }

        public bool SetProjectProperty(string name, string value, bool addProp)
        {
            if (string.IsNullOrEmpty(name) | string.IsNullOrEmpty(value))
            {
                return false;
            }

            int index;

            for (index = 0; index <= this.lines.Count - 1; index++)
            {
                string buffer = this.lines[index].ToUpper(CultureInfo.InvariantCulture);

                if (buffer.StartsWith(name.ToUpper(CultureInfo.InvariantCulture) + "=", StringComparison.OrdinalIgnoreCase))
                {
                    this.lines[index] = this.lines[index].Substring(0, (name + "=").Length) + value;
                    return true;
                }
            }

            if (addProp)
            {
                this.lines.Add(name + "=" + value);
                return true;
            }

            return false;
        }

        public bool GetProjectProperty(string name, ref string value)
        {
            foreach (string line in this.lines)
            {
                string buffer = line.ToUpper(CultureInfo.InvariantCulture);

                if (buffer.StartsWith(name.ToUpper(CultureInfo.InvariantCulture) + "=", StringComparison.OrdinalIgnoreCase))
                {
                    value = line.Substring(1 + (name + "=").Length);
                    return true;
                }
            }

            return false;
        }

        public List<FileInfo> GetFiles()
        {
            List<FileInfo> retVal = new List<FileInfo>();
            FileInfo projectFileInfo = new FileInfo(this.projectFile);
            foreach (var line in this.lines)
            {
                //Module=Module1; Module1.bas

                var splittedLine = line.Split('=');
                switch (splittedLine[0])
                {
                    case "Form":
                    case "Module":
                    case "Class":
                    case "UserControl":
                        //Module1; Module1.bas
                        //Form1.frm


                        string fileName = splittedLine[1];
                        if (fileName.Contains(";"))
                        {
                            fileName = fileName.Substring(fileName.IndexOf(";") + 1);
                            fileName = fileName.Trim();
                        }
                        fileName = Path.Combine(projectFileInfo.Directory.FullName, fileName);
                        retVal.Add(new FileInfo(fileName));
                        break;

                    default:
                        break;
                }
            }
            return retVal;
        }

        public FileInfo ArtifactFile
        {
            get
            {
                string artifactFileName = null;
                if (!this.GetProjectProperty("ExeName32", ref artifactFileName)) throw new ApplicationException("'ExeName32' Property not found");
                artifactFileName = artifactFileName.Replace("\"", "");

                FileInfo projectFileInfo = new FileInfo(this.ProjectFile);

                string artifactPath = projectFileInfo.Directory.FullName;
                string path32 = null;
                if (this.GetProjectProperty("Path32", ref path32))
                {
                    path32 = path32.Replace("\"", "");
                    artifactPath = Path.Combine(artifactPath, path32);
                }

                artifactFileName = Path.Combine(artifactPath, artifactFileName);
                return new FileInfo(artifactFileName);
            }
        }
    }
}