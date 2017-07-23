//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlScriptLoaderTests.cs">(c) 2017 Mike Fourie and Contributors (https://github.com/mikefourie/MSBuildExtensionPack) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework.Tests
{
    using System.IO;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MSBuild.ExtensionPack.SqlServer.Extended;

    [TestClass]
    public sealed class SqlScriptLoaderTests
    {
        private SqlScriptLoader loader;
        private string sql;

        [TestInitialize]
        public void Setup()
        {
        }

        [TestMethod]
        public void RemovesSingleMultiLineComment()
        {
            this.GivenSqlStream("select /* I don't know! */ * from mytable");
            this.WhenLoadingSql();
            this.ThenSqlReadIs("select  * from mytable");
        }

        [TestMethod]
        public void RemovesMultiLineComment()
        {
            const string Tsql = @"select * /*
I am a 
mutli
line comment */
                     from 
/* another */
                       my_table";
            this.GivenSqlStream(Tsql);
            this.WhenLoadingSql();

            const string ExpectedSql = @"select * 
                     from 

                       my_table";

            this.ThenSqlReadIs(ExpectedSql);
        }

        [TestMethod]
        public void ReadsEmptyFile()
        {
            this.GivenSqlStream(string.Empty);
            this.WhenLoadingSql();
            this.ThenSqlReadIs(string.Empty);
        }

        [TestMethod]
        public void ReadsCommentThatNeverCloses()
        {
            this.GivenSqlStream("seleect /*");
            this.WhenLoadingSql();
            this.ThenSqlReadIs("seleect ");
        }

        [TestMethod]
        public void ReadsNestedComments()
        {
            this.GivenSqlStream("select /* begin  A /* begin b /* c */ b end */ A comment end */ * from my_table");
            this.WhenLoadingSql();
            this.ThenSqlReadIs("select  * from my_table");
        }

        [TestMethod]
        public void ReadsClosingCommentCharacters()
        {
            const string Tsql = "select '*/' from my_table";
            this.GivenSqlStream(Tsql);
            this.WhenLoadingSql();
            this.ThenSqlReadIs(Tsql);
        }

        private void GivenSqlStream(string tsql)
        {
            byte[] sqlBytes = new UTF8Encoding().GetBytes(tsql);
            this.loader = new SqlScriptLoader(new StreamReader(new MemoryStream(sqlBytes)), true);
        }

        private void ThenSqlReadIs(string expectedSql)
        {
            Assert.AreEqual(expectedSql, this.sql);
        }

        private void WhenLoadingSql()
        {
            this.sql = this.loader.ReadToEnd();
        }
    }
}
