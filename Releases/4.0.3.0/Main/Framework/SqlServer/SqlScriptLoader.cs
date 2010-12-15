//-----------------------------------------------------------------------
// <copyright file="SqlScriptLoader.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.SqlServer.Extended
{
    using System;
    using System.IO;
    using System.Text;

    public sealed class SqlScriptLoader
    {
        private readonly StreamReader reader;
        private readonly StringBuilder contents;
        private char currentChar;
        private char nextChar;
        private bool inComment;
        private int commentDepth;

        public SqlScriptLoader(StreamReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            this.reader = reader;
            this.contents = new StringBuilder();
        }

        public string ReadToEnd()
        {
            this.commentDepth = 0;
            if (this.reader.EndOfStream)
            {
                return this.contents.ToString();
            }

            while (!this.reader.EndOfStream)
            {
                if (!this.Read())
                {
                    break;
                }

                if (this.inComment && this.currentChar == '*' && this.Peek() && this.nextChar == '/')
                {
                    this.commentDepth--;
                    this.Read();

                    if (this.commentDepth == 0)
                    {
                        this.inComment = false;
                        continue;
                    }
                }

                if (this.currentChar == '/' && this.Peek() && this.nextChar == '*')
                {
                    this.inComment = true;
                    this.commentDepth++;
                    this.Read();
                    continue;
                }

                if (!this.inComment)
                {
                    this.contents.Append(this.currentChar);
                }
            }

            return this.contents.ToString();
        }

        private bool Read()
        {
            int nextByte = this.reader.Read();
            if (nextByte == -1)
            {
                return false;
            }

            this.currentChar = Convert.ToChar(nextByte);
            return true;
        }

        private bool Peek()
        {
            int nextByte = this.reader.Peek();
            if (nextByte == -1)
            {
                return false;
            }

            this.nextChar = Convert.ToChar(nextByte);
            return true;
        }
    }
}
