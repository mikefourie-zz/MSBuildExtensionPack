//-----------------------------------------------------------------------
// <copyright file="Maths.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
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
    /// <para><i>And</i> (<b>Required: </b> Numbers <b>Output: </b>Result)</para>
    /// <para><i>Or</i> (<b>Required: </b> Numbers <b>Output: </b>Result)</para>
    /// <para><i>Compare</i> (<b>Required: </b> P1, P2, Comparison <b>Output: </b>LogicalResult)</para>
    /// <para><i>Divide</i> (<b>Required: </b> Numbers <b>Output: </b>Result)</para>
    /// <para><i>Evaluate</i> (<b>Required: </b> Expression <b>Output: </b>Result)</para>
    /// <para><i>Multiply</i> (<b>Required: </b> Numbers <b>Output: </b>Result)</para>
    /// <para><i>Modulus</i> (<b>Required: </b> Numbers <b>Output: </b>Result)</para>
    /// <para><i>Subtract</i> (<b>Required: </b> Numbers <b>Output: </b>Result)</para>
    /// <para><i>LeftShift</i> (<b>Required: </b> Numbers <b>Output: </b>Result)</para>
    /// <para><i>RightShift</i> (<b>Required: </b> Numbers <b>Output: </b>Result)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
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
    ///         <CallTarget Targets="Default2"/>
    ///     </Target>
    ///     <!-- Factorial calculation provided by Stephen Cleary -->
    ///     <Target Name="Default2">
    ///         <PropertyGroup>
    ///             <Input>8</Input>
    ///         </PropertyGroup>
    ///         <!-- Argument error checking -->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="Compare" P1="$(Input)" P2="1" Comparison="LessThan">
    ///             <Output TaskParameter="LogicalResult" PropertyName="InputCheck"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///         <Error Condition="'$(InputCheck)' != 'False'" Text="Input cannot be less than 1."/>
    ///         <!-- Invoke the Factorial target with the current Input property -->
    ///         <MSBuild Projects="$(MSBuildProjectFile)" Targets="Factorial" Properties="Input=$(Input)">
    ///             <Output TaskParameter="TargetOutputs" ItemName="FactorialResult"/>
    ///         </MSBuild>
    ///         <!-- Display the result -->
    ///         <Message Importance="high" Text="Result: @(FactorialResult)"/>
    ///     </Target>
    ///     <!-- The Factorial target uses FactorialCore to do the calculation, storing the result in FactorialResult -->
    ///     <Target Name="Factorial" DependsOnTargets="FactorialCore" Outputs="$(FactorialResult)"/>
    ///     <Target Name="FactorialCore">
    ///         <!-- If the input is 1, then the factorial is 1 -->
    ///         <PropertyGroup Condition="'$(Input)' == '1'">
    ///             <FactorialResult>1</FactorialResult>
    ///         </PropertyGroup>
    ///         <!-- If we don't know the result yet (i.e. the input is not 1) then calculate the factorial -->
    ///         <CallTarget Condition="'$(FactorialResult)' == ''" Targets="CalculateFactorial"/>
    ///     </Target>
    ///     <Target Name="CalculateFactorial">
    ///         <!-- Subtract 1 from $(Input) -->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="Subtract" Numbers="$(Input);1">
    ///             <Output TaskParameter="Result" PropertyName="InputMinus1"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///         <!-- Determine the factorial of $(Input) - 1 -->
    ///         <MSBuild Projects="$(MSBuildProjectFile)" Targets="Factorial" Properties="Input=$(InputMinus1)">
    ///             <Output TaskParameter="TargetOutputs" ItemName="SubResult"/>
    ///         </MSBuild>
    ///         <!-- Multiply !($(Input) - 1) by $(Input) to get the result-->
    ///         <MSBuild.ExtensionPack.Science.Maths TaskAction="Multiply" Numbers="@(SubResult);$(Input)">
    ///             <Output TaskParameter="Result" PropertyName="FactorialResult"/>
    ///         </MSBuild.ExtensionPack.Science.Maths>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Maths : BaseTask
    {
        private const string AddTaskAction = "Add";
        private const string CompareTaskAction = "Compare";
        private const string DivideTaskAction = "Divide";
        private const string EvaluateTaskAction = "Evaluate";
        private const string MultiplyTaskAction = "Multiply";
        private const string SubtractTaskAction = "Subtract";
        private const string OrTaskAction = "Or";
        private const string AndTaskAction = "And";
        private const string ModulusTaskAction = "Modulus";
        private const string LeftShiftTaskAction = "LeftShift";
        private const string RightShiftTaskAction = "RightShift";

        private float[] numbers;
        private float total;

        /// <summary>
        /// Sets P1.
        /// </summary>
        public long P1 { get; set; }

        /// <summary>
        /// Gets the LogicalResult
        /// </summary>
        [Output]
        public bool LogicalResult { get; set; }

        /// <summary>
        /// Sets P2.
        /// </summary>
        public long P2 { get; set; }

        /// <summary>
        /// Sets the Comparison. Supports 'GreaterThan', 'LessThan', 'GreaterThanOrEquals', 'LessThanOrEquals'
        /// </summary>
        public string Comparison { get; set; }

        /// <summary>
        /// A semicolon separated collection of numbers
        /// </summary>
        public string[] Numbers
        {
            set { this.numbers = ToFloatArray(value); }
        }

        /// <summary>
        /// Gets the result.
        /// </summary>
        [Output]
        public float Result { get; set; }

        /// <summary>
        /// Sets the expression.
        /// </summary>
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
                case AddTaskAction:
                    this.Add();
                    break;
                case SubtractTaskAction:
                    this.Subtract();
                    break;
                case MultiplyTaskAction:
                    this.Multiply();
                    break;
                case DivideTaskAction:
                    this.Divide();
                    break;
                case EvaluateTaskAction:
                    this.Evaluate();
                    break;
                case CompareTaskAction:
                    this.Compare();
                    break;
                case ModulusTaskAction:
                    this.Modulus();
                    break;
                case AndTaskAction:
                    this.And();
                    break;
                case OrTaskAction:
                    this.Or();
                    break;
                case LeftShiftTaskAction:
                    this.LeftShift();
                    break;
                case RightShiftTaskAction:
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
            using (DataTable dt = new DataTable())
            {
                dt.Locale = CultureInfo.CurrentCulture;
                this.total = DecimalToSglDbl(Convert.ToDecimal(dt.Compute(this.Expression, string.Empty).ToString(), CultureInfo.CurrentCulture));
            }
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