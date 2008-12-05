//-----------------------------------------------------------------------
// <copyright file="Maths.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Science
{
    using System;
    using System.Data;
    using System.Globalization;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Add</i> (<b>Required: </b> Numbers <b>Output: </b>Result)</para>
    /// <para><i>Compare</i> (<b>Required: </b> P1, P2, Comparison <b>Output: </b>LogicalResult)</para>
    /// <para><i>Divide</i> (<b>Required: </b> Numbers <b>Output: </b>Result)</para>
    /// <para><i>Evaluate</i> (<b>Required: </b> Expression <b>Output: </b>Result)</para>
    /// <para><i>Multiply</i> (<b>Required: </b> Numbers <b>Output: </b>Result)</para>
    /// <para><i>Subtract</i> (<b>Required: </b> Numbers <b>Output: </b>Result)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- Left Shift two numbers -->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="LeftShift" Numbers="15;2">
    ///             <Output PropertyName="RResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///         <Message Text="LeftShift: $(RResult)"/>
    ///         <!-- Right Shift two numbers -->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="RightShift" Numbers="33;3">
    ///             <Output PropertyName="RResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///         <Message Text="RightShift: $(RResult)"/>
    ///         <!-- Or two numbers -->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="Or" Numbers="5;4">
    ///             <Output PropertyName="RResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///         <Message Text="Or: $(RResult)"/>
    ///         <!-- Mod two numbers -->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="Modulus" Numbers="10;3">
    ///             <Output PropertyName="RResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///         <Message Text="Modulus: $(RResult)"/>
    ///         <!-- Evaluate a basic expression -->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="Evaluate" Expression="180 / (5 * (18/3)) + 2">
    ///             <Output PropertyName="RResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///         <Message Text="Evaluate: $(RResult)"/>
    ///         <!-- Add numbers -->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="Add" Numbers="13;2;13;2;13;2;13;2">
    ///             <Output PropertyName="RResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///         <Message Text="Add: $(RResult)"/>
    ///         <!-- Subtract numbers -->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="Subtract" Numbers="13;2">
    ///             <Output PropertyName="RResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///         <Message Text="Subtract: $(RResult)"/>
    ///         <!-- Divide numbers -->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="Divide" Numbers="13;2.6235">
    ///             <Output PropertyName="RResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///         <Message Text="Divide: $(RResult)"/>
    ///         <!-- Multiply numbers -->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="Multiply" Numbers="13;2">
    ///             <Output PropertyName="RResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///         <Message Text="Multiply: $(RResult)"/>
    ///         <!-- Compare whether one number is less than the other -->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="Compare" P1="2" P2="60" Comparison="LessThan">
    ///             <Output PropertyName="RResult" TaskParameter="LogicalResult"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///         <Message Text="Compare: $(RResult)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.1.0/html/34c698fb-7a58-e7fd-4263-83d150c7ef41.htm")]
    public class Maths : BaseTask
    {
        private const string AddTaskAction = "Add";
        private const string CompareTaskAction = "Compare";
        private const string DivideTaskAction = "Divide";
        private const string EvaluateTaskAction = "Evaluate";
        private const string MultiplyTaskAction = "Multiply";
        private const string SubtractTaskAction = "Subtract";

        private float[] numbers;
        private float total;

        [DropdownValue(AddTaskAction)]
        [DropdownValue(CompareTaskAction)]
        [DropdownValue(DivideTaskAction)]
        [DropdownValue(EvaluateTaskAction)]
        [DropdownValue(MultiplyTaskAction)]
        [DropdownValue(SubtractTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets P1.
        /// </summary>
        [TaskAction(CompareTaskAction, true)]
        public long P1 { get; set; }

        /// <summary>
        /// Gets the LogicalResult
        /// </summary>
        [Output]
        [TaskAction(CompareTaskAction, false)]
        public bool LogicalResult { get; set; }

        /// <summary>
        /// Sets P2.
        /// </summary>
        [TaskAction(CompareTaskAction, true)]
        public long P2 { get; set; }

        /// <summary>
        /// Sets the Comparison. Supports 'GreaterThan', 'LessThan', 'GreaterThanOrEquals', 'LessThanOrEquals'
        /// </summary>
        [TaskAction(CompareTaskAction, true)]
        public string Comparison { get; set; }

        /// <summary>
        /// A semicolon separated collection of numbers
        /// </summary>
        [TaskAction(AddTaskAction, true)]
        [TaskAction(DivideTaskAction, true)]
        [TaskAction(MultiplyTaskAction, true)]
        [TaskAction(SubtractTaskAction, true)]
        public string[] Numbers
        {
            set { this.numbers = ToFloatArray(value); }
        }

        /// <summary>
        /// Gets the result.
        /// </summary>
        [Output]
        [TaskAction(AddTaskAction, false)]
        [TaskAction(DivideTaskAction, false)]
        [TaskAction(EvaluateTaskAction, false)]
        [TaskAction(MultiplyTaskAction, false)]
        [TaskAction(SubtractTaskAction, false)]
        public float Result { get; set; }

        /// <summary>
        /// Sets the expression.
        /// </summary>
        [TaskAction(EvaluateTaskAction, true)]
        public string Expression { get; set; }

        protected static float[] ToFloatArray(string[] numberArray)
        {
            float[] floatArray = new float[numberArray.Length];

            for (int x = 0; x < numberArray.Length; x++)
            {
                float converted;
                floatArray[x] = float.TryParse(numberArray[x], out converted) ? converted : 0;
            }

            return floatArray;
        }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }
            
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "{0} numbers", this.TaskAction));
            switch (this.TaskAction)
            {
                case "Add":
                    this.Add();
                    break;
                case "Subtract":
                    this.Subtract();
                    break;
                case "Multiply":
                    this.Multiply();
                    break;
                case "Divide":
                    this.Divide();
                    break;
                case "Evaluate":
                    this.Evaluate();
                    break;
                case "Compare":
                    this.Compare();
                    break;
                case "Modulus":
                    this.Modulus();
                    break;
                case "And":
                    this.And();
                    break;
                case "Or":
                    this.Or();
                    break;
                case "LeftShift":
                    this.LeftShift();
                    break;
                case "RightShift":
                    this.RightShift();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }

            this.Result = this.total;
        }

        private static float DecimalToSglDbl(decimal argument)
        {
            return decimal.ToSingle(argument);
        }

        private void RightShift()
        {
            int totalI = Convert.ToInt32(this.numbers[0]);
            for (int i = 1; i < this.numbers.Length; i++)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Operation: {0} >> {1}", totalI, this.numbers[i]));
                totalI >>= Convert.ToInt32(this.numbers[i]);
            }

            this.total = totalI;
        }

        private void LeftShift()
        {
            int totalI = Convert.ToInt32(this.numbers[0]);
            for (int i = 1; i < this.numbers.Length; i++)
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Operation: {0} << {1}", totalI, this.numbers[i]));
                totalI <<= Convert.ToInt32(this.numbers[i]);
            }

            this.total = totalI;
        }

        private void Or()
        {
            int totalI = Convert.ToInt32(this.numbers[0]);
            for (int i = 1; i < this.numbers.Length; i++)
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Operation: {0} | {1}", totalI, this.numbers[i]));
                totalI |= Convert.ToInt32(this.numbers[i]);
            }

            this.total = totalI;
        }

        private void And()
        {
            int totalI = Convert.ToInt32(this.numbers[0]);
            for (int i = 1; i < this.numbers.Length; i++)
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Operation: {0} & {1}", totalI, this.numbers[i]));
                totalI &= Convert.ToInt32(this.numbers[i]);
            }

            this.total = totalI;
        }

        private void Compare()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Comparing: {0} [{1}] {2}", this.P1, this.Comparison.ToUpperInvariant(), this.P2));

            switch (this.Comparison.ToUpperInvariant())
            {
                case "GREATERTHAN":
                    this.LogicalResult = this.P1 > this.P2;
                    break;
                case "LESSTHAN":
                    this.LogicalResult = this.P1 < this.P2;
                    break;
                case "GREATERTHANOREQUALS":
                    this.LogicalResult = this.P1 >= this.P2;
                    break;
                case "LESSTHANOREQUALS":
                    this.LogicalResult = this.P1 <= this.P2;
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid Comparison passed: {0}. Valid Comparisons are 'GreaterThan', 'LessThan', 'GreaterThanOrEquals', 'LessThanOrEquals'", this.Comparison.ToUpperInvariant()));
                    return;
            }
        }

        private void Evaluate()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Evaluating Expression: {0}", this.Expression));
            DataTable dt = new DataTable { Locale = CultureInfo.CurrentCulture };
            this.total = DecimalToSglDbl(Convert.ToDecimal(dt.Compute(this.Expression, string.Empty).ToString(), CultureInfo.CurrentCulture));
        }

        private void Divide()
        {
            this.total = this.numbers[0];
            for (int x = 1; x < this.numbers.Length; x++)
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Operation: {0} / {1}", this.total, this.numbers[x]));
                this.total /= this.numbers[x];
            }
        }

        private void Multiply()
        {
            this.total = this.numbers[0];
            for (int i = 1; i < this.numbers.Length; i++)
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Operation: {0} * {1}", this.total, this.numbers[i]));
                this.total *= this.numbers[i];
            }
        }

        private void Subtract()
        {
            this.total = this.numbers[0];
            for (int x = 1; x < this.numbers.Length; x++)
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Operation: {0} - {1}", this.total, this.numbers[x]));
                this.total -= this.numbers[x];
            }
        }

        private void Add()
        {
            this.total = this.numbers[0];
            for (int i = 1; i < this.numbers.Length; i++)
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Operation: {0} + {1}", this.total, this.numbers[i]));
                this.total += this.numbers[i];
            }
        }

        private void Modulus()
        {
            this.total = this.numbers[0];
            for (int i = 1; i < this.numbers.Length; i++)
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Operation: {0} % {1}", this.total, this.numbers[i]));
                this.total %= this.numbers[i];
            }
        }
    }
}