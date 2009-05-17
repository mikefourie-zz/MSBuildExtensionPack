//-----------------------------------------------------------------------
// <copyright file="VBPProject.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
using System.Globalization;

namespace MSBuild.ExtensionPack.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class VBPProject
    {
        private readonly List<string> Lines = new List<string>();
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
                    this.Lines.Add(lineStream.ReadLine());
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
            if (string.IsNullOrEmpty(this.projectFile) | this.Lines.Count == 0)
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
                foreach (string line in this.Lines)
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

            for (index = 0; index <= this.Lines.Count - 1; index++)
            {
                string buffer = this.Lines[index].ToUpper(CultureInfo.InvariantCulture);

                if (buffer.StartsWith(name.ToUpper(CultureInfo.InvariantCulture) + "=", StringComparison.OrdinalIgnoreCase))
                {
                    this.Lines[index] = this.Lines[index].Substring(0, (name + "=").Length) + value;
                    return true;
                }
            }

            if (addProp)
            {
                this.Lines.Add(name + "=" + value);
                return true;
            }

            return false;
        }

        public bool GetProjectProperty(string name, ref string value)
        {
            foreach (string line in this.Lines)
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
    }
}