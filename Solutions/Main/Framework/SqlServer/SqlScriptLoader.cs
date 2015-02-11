//-----------------------------------------------------------------------
// <copyright file="SqlScriptLoader.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.SqlServer.Extended
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// SqlScriptLoader
    /// </summary>
    public sealed class SqlScriptLoader
    {
        private readonly StreamReader reader;
        private readonly StringBuilder contents;
        private readonly bool strip;
        private char currentChar;
        private char nextChar;
        private bool inComment;
        private int commentDepth;
        
        /// <summary>
        /// Initializes a new instance of the SqlScriptLoader class
        /// </summary>
        /// <param name="reader">StreamReader</param>
        /// <param name="stripMultiLineComments">bool</param>
        public SqlScriptLoader(StreamReader reader, bool stripMultiLineComments)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            this.strip = stripMultiLineComments;
            this.reader = reader;
            this.contents = new StringBuilder();
        }

        /// <summary>
        /// ReadToEnd
        /// </summary>
        /// <returns>string</returns>
        public string ReadToEnd()
        {
            if (this.reader.EndOfStream)
            {
                return this.contents.ToString();
            }

            if (this.strip)
            {
                this.commentDepth = 0;
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
            }
            else
            {
                this.contents.Append(this.reader.ReadToEnd());
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
