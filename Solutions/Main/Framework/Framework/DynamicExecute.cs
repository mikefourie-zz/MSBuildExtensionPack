//-----------------------------------------------------------------------
// <copyright file="DynamicExecute.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// Task Contributors: Stephen Cleary
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.CSharp;

    // Possible future extensions:
    //   Advanced conversions (supporting more user-defined types via IFormattable, constructors, ToString, TypeConverter/TypeDescriptor, or Reflection/AssignableFrom)
    //   Languages other than C# (not too hard if we convert to CodeDOM)
    //   Remote execution support

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Define</i> (<b>Required: </b> Code <b>Optional: </b> Inputs, Outputs, References, UsingNamespaces, NoDefaultParameters, NoDefaultReferences, NoDefaultUsingNamespaces <b>Output: </b> OutputMethodId). Defines and compiles a new method, which can then be used to create a closure.</para>
    /// <para><i>Create</i> (<b>Required: </b> MethodId <b>Output: </b> OutputClosureId). Creates a new closure. All input and output arguments for this closure are set to their default values.</para>
    /// <para><i>SetInput</i> (<b>Required: </b> ClosureId, Name, InputValue). Sets an argument value for a closure.</para>
    /// <para><i>Invoke</i> (<b>Required: </b> ClosureId). Invokes a closure.</para>
    /// <para><i>GetOutput</i> (<b>Required: </b> ClosureId, Name <b>Output: </b> OutputValue). Retrieves a result value (output parameter value) from a closure.</para>
    /// <para><i>Destroy</i> (<b>Required: </b> ClosureId). Disposes of a closure. The closure ID is no longer valid after this task action.</para>
    /// <para><i>Call</i> (<b>Required: </b> MethodId <b>Optional: </b> Input1, Input2, Input3 <b>Output: </b> Output1, Output2, Output3). Calls a method with up to three inputs, returning up to three outputs. Internally, creates a closure, sets the input parameters, invokes it, retrieves the output parameters, and destroys it.</para>
    /// <para><i>Run</i> (<b>Required: </b> Code <b>Optional: </b> Inputs, Outputs, References, UsingNamespaces, NoDefaultParameters, NoDefaultReferences, NoDefaultUsingNamespaces, Input1, Input2, Input3 <b>Output: </b> Output1, Output2, Output3, OutputMethodId). Defines a method and runs it. The task outputs include the outputs from the method as well as the method identifier.</para>
    /// <para><b>Remote Execution Support: </b> None.</para>
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="DynamicExecute"/> task allows defining and executing code at build time. The code is not interpreted; rather, it is compiled and then loaded into the MSBuild process.</para>
    /// <para>Currently, the only supported language is C#.</para>
    /// <para><b>Code, Methods, and Closures</b></para>
    /// <para>"Code" is the actual source code to be executed. Code may be executed directly by the <b>Run</b> task action, or code may be used to define a method by the <b>Define</b> task action.</para>
    /// <para>A "method" is a piece of defined code. Methods are compiled and loaded into the MSBuild process dynamically. A method may be executed by the <b>Call</b> task action, or used to create a closure by the <b>Create</b> task action.</para>
    /// <para>A "closure" is a reference to a method along with values for all the method's input and output parameters.</para>
    /// <para><b>Using Closures</b></para>
    /// <para>A closure contains values for all inputs and outputs of a particular method. Generally, a closure is created, its input values are set, it is invoked, its output values are retrieved, and finally the closure is destroyed.</para>
    /// <para>When a closure is created, all input and output values are set to their default values. It is possible to call a method without specifying input values; in this case, the default values are used. It is also possible to re-use a closure instead of destroying it; however, this may cause confusion since the output values are not reset before invoking the method again.</para>
    /// <para>Most of the time, the <b>Call</b> or <b>Run</b> task actions are used. These create a closure to do their work, destroying it when they are done. These task actions are much more compact than using the closure-based task actions such as <b>SetInput</b> and <b>GetOutput</b>.</para>
    /// <para>However, <b>Call</b> and <b>Run</b> do have limitations. Closures allow any number of input and output values, instead of just three. Also, input and output values are set and retrieved by name when using closures directly; <b>Run</b> and <b>Call</b> can only set and retrieve by position.</para>
    /// <para><b>Code Context</b></para>
    /// <para>The actual code is compiled into a static method of a class. Enclosing <see cref="Code"/> in curly braces is not necessary.</para>
    /// <para>There are three types of parameters passed to the method: default parameters, input parameters, and output parameters.</para>
    /// <para>Currently, there is only one default parameter, named "@this". Its type is <b>Microsoft.Build.Utilities.Task</b>, and it may be used to access task-level properties such as <b>Log</b> and <b>BuildEngine2</b>. Default parameters may be disabled by specifying <see cref="NoDefaultParameters"/>.</para>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- A very simple example, showing the default parameter (currently, there is only one) -->
    ///         <!-- Output: Hi! -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Code="@this.Log.LogMessage(%22Hi!%22);"
    ///                                                         />
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// <para>Input parameters are strongly-typed, as defined by <see cref="Inputs"/>.</para>
    /// <para>Output parameters are likewise strongly-typed, as defined by <see cref="Outputs"/>. They are compiled as <b>ref</b> parameters, so all method outputs are optional by definition.</para>
    /// <para>The compiled method returns void, so code containing a simple <b>return</b> will compile, but any code attempting to return a value will not. All method outputs must be assigned to an output parameter before returning.</para>
    /// <para>Assembly references and using namespaces may be augmented or replaced; see <b>Advanced Code Options</b> below for more details.</para>
    /// <para><b>Specifying Inputs and Outputs</b></para>
    /// <para>Each input or output has a type and a name. The name must be a legal C# parameter name, and cannot start with "_" or be equal to "@this". Input and output names must be unique; an input cannot have the same name as an output.</para>
    /// <para>If <see cref="NoDefaultParameters"/> is specified, then input and output names may start with "_" or be equal to "@this".</para>
    /// <para>Type and name pairs may be specified one of three ways. The first (and most compact) way is to pass a comma-delimited string of type and name pairs. This is the most familiar syntax to C#.</para>
    /// <para>The second way is to pass an array of task items, with the identity of each item set to its type and name separated by at least one space.</para>
    /// <para>The third way is to pass an array of task items, with the type looked up from the item's "Type" metadata. The name may be specified by the item's "Name" metadata or its identity.</para>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- These are equivalent -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Inputs="string format"
    ///                                                         Outputs="string result"
    ///                                                         Input1="yyyy-MM-dd"
    ///                                                         Code="result = DateTime.Now.ToString(format);"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="FormattedDate"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <Message Text="Formatted date: $(FormattedDate)"/>
    ///         <ItemGroup>
    ///             <FormatInputs Include="string format"/>
    ///             <FormatOutputs Include="result">
    ///                 <Type>string</Type>
    ///             </FormatOutputs>
    ///         </ItemGroup>
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Inputs="@(FormatInputs)"
    ///                                                         Outputs="@(FormatOutputs)"
    ///                                                         Input1="yyyy-MM-dd"
    ///                                                         Code="result = DateTime.Now.ToString(format);"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="FormattedDate"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <Message Text="Formatted date: $(FormattedDate)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// <para><b>Supported Input and Output Types</b></para>
    /// <para>Input and output types must be one of the following:</para>
    /// <para><b>Group A</b> - <b>ITaskItem[]</b> or <b>ITaskItem</b>. This is the MSBuild task item group / task item, which may be used to access metadata.</para>
    /// <para><b>Group B</b> - The CLR types convertible to and from <b>string</b>. This includes the types <b>string</b>, <b>char</b>, <b>bool</b>, <b>byte</b>, <b>sbyte</b>, <b>short</b>, <b>ushort</b>, <b>int</b>, <b>uint</b>, <b>long</b>, <b>ulong</b>, <b>float</b>, <b>double</b>, <b>Decimal</b>, and <b>DateTime</b>.</para>
    /// <para><b>Group C</b> - Any nullable type whose underlying type is in Group B. This includes the types <b>char?</b>, <b>bool?</b>, <b>byte?</b>, <b>sbyte?</b>, <b>short?</b>, <b>ushort?</b>, <b>int?</b>, <b>uint?</b>, <b>long?</b>, <b>ulong?</b>, <b>float?</b>, <b>double?</b>, <b>Decimal?</b>, and <b>DateTime?</b>.</para>
    /// <para><b>Group D</b> - An array of any type from Group B. Each element of the array must contain a value; it is not valid to pass or return arrays if one of the elements in the array is null.</para>
    /// <para>Invalid input and output types are not detected at compile time. They are only detected if a value fails to convert to or from the specified type.</para>
    /// <para><b>Conversion of Input Parameters</b></para>
    /// <para>An input argument value passes through two conversions. The first is the default MSBuild conversion, and the second is performed by the <b>SetInput</b>, <b>Call</b>, or <b>Run</b> task action.</para>
    /// <para>The MSBuild conversion always converts to <b>ITaskItem[]</b>, because this is the type of the <see cref="InputValue"/>, <see cref="Input1"/>, <see cref="Input2"/>, and <see cref="Input3"/> properties.</para>
    /// <para>Once the input value has been converted to an array of task items, the <b>DynamicExecute</b> task action performs a second conversion. This is designed to work similarly to the MSBuild conversions and default C# conversions to prevent unexpected behavior. The exact steps taken are dependent on the group the input type belongs to; see <i>Supported Types</i> above for more information about the type grouping.</para>
    /// <para><b>Group A</b> - No actual conversion is performed. If the method expects a single <b>ITaskItem</b>, then the task action ensures that the input value contains only a single task item.</para>
    /// <para><b>Groups B and C</b> - The task ensures that the input value contains only a single task item. Then, the task item's <b>ItemSpec</b> is used as a string value, and this string is converted to the expected type.</para>
    /// <para><b>Group D</b> - Each task item's <b>ItemSpec</b> is used as a string value, and this string is converted to the expected type. The result is an array with the same number of elements as the array of task items.</para>
    /// <para>If an input argument value is null, then no conversions are performed; the method is passed a null value.</para>
    /// <para>Special conversions exist if the input parameter is of <b>bool</b> type. Valid values include "true", "false", "yes", "no", "on", and "off", all case-insensitive. In addition, these values may be prefixed with the logical "not" operator ("!"). These conversions are supported because they are MSBuild conventions.</para>
    /// <para><b>Conversion of Strings</b></para>
    /// <para>String input parameters may cause problems if the argument value contains semicolons. In this case, the default MSBuild conversion will split the string into an array of <b>ITaskItem</b>, using the semicolons as separators.</para>
    /// <para>To prevent this behavior, one may first escape the string by using the <see cref="TextString"/> <i>Replace</i> task action, as this example illustrates:</para>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <PropertyGroup>
    ///             <String>This semicolon does not separate items; it is used in a grammatical sense.</String>
    ///         </PropertyGroup>
    ///         <!-- Semicolons normally act as item separators; to prevent this treatment, escape them first -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="Replace"
    ///                                                     OldString="$(String)"
    ///                                                     OldValue=";"
    ///                                                     NewValue="%3B"
    ///                                                     >
    ///             <Output TaskParameter="NewString" PropertyName="EscapedString"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <!-- $(String) would be treated as a vector argument (2 elements), but $(EscapedString) is a scalar argument -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Inputs="string test"
    ///                                                         Outputs="string result"
    ///                                                         Code="result = test;"
    ///                                                         Input1="$(EscapedString)"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="Result"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Converting the result to an item group shows that the semicolon is still not used as a separator -->
    ///         <ItemGroup>
    ///             <!-- Only one item will exist in this item group -->
    ///             <ResultItemGroup Include="$(Result)"/>
    ///         </ItemGroup>
    ///         <!-- Result:  This semicolon does not separate items; it is used in a grammatical sense.  -->
    ///         <Message Text="Result: @(ResultItemGroup->' %(Identity) ')"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// <para><b>Conversion of Output Parameters</b></para>
    /// <para>An output argument value passes through two conversions. The first is performed by the <b>GetOutput</b>, <b>Call</b>, or <b>Run</b> task action. The second is the default MSBuild conversion.</para>
    /// <para>The <b>DynamicExecute</b> task action performs the first conversion. This is always a conversion to <b>ITaskItem[]</b>, because that is the type of the <see cref="OutputValue"/>, <see cref="Output1"/>, <see cref="Output2"/>, and <see cref="Output3"/> properties.</para>
    /// <para>This conversion is designed to work similarly to the MSBuild conversions and default C# conversions to prevent unexpected behavior. The exact steps taken are dependent on the group the output type belongs to; see <i>Supported Types</i> above for more information about the type grouping.</para>
    /// <para><b>Group A</b> - The actual objects returned must be of type <b>TaskItem</b> or <b>TaskItem[]</b> (returning an instance of another type implementing <b>ITaskItem</b> is not supported). No actual conversion is performed. If the method produces a single <b>TaskItem</b>, then the task action creates an array of task items containing only the single element.</para>
    /// <para><b>Groups B and C</b> - An array of task items is returned containing a single element. The <b>ItemSpec</b> of that single element is the output value converted to a string. Note that null values are treated specially (see below).</para>
    /// <para><b>Group D</b> - An array of task items is returned, with the same number of items as the output array. For each corresponding array item, the <b>ItemSpec</b> of the task item array element is set to the string representation of the output array element.</para>
    /// <para>If an output argument value is null, then no conversions are performed by the task action. MSBuild will convert a null value to an empty string or empty item group if necessary.</para>
    /// <para>The default MSBuild conversion will convert from <b>ITaskItem[]</b> to an item group or string as necessary.</para>
    /// <para><b>Advanced Code Options</b></para>
    /// <para>When a method is compiled, it is given some assembly references by default. See <see cref="References"/> for a list of the default references. Specify <see cref="NoDefaultReferences"/> to prevent default references from being used.</para>
    /// <para>The method is also given some <b>using namespace</b> declarations by default. See <see cref="UsingNamespaces"/> for a list of the default "using namespaces". Specify <see cref="NoDefaultUsingNamespaces"/> to prevent default "using namespaces" from being used.</para>
    /// <para>Finally, the method is given some default parameters. Currently, the only default parameter is "@this", but all parameter names beginning with an underscore ("_") are reserved for future default parameters. Specify <see cref="NoDefaultParameters"/> to prevent default parameters from being used.</para>
    /// <para><b>Limitations</b></para>
    /// <para>One defined method may not call another defined method.</para>
    /// <para>There is no facility for a method storing data in a way that it could be retrieved by a future call of the method (or another method). A workaround is to convert any such data to a string representation and pass that as an input and / or output parameter.</para>
    /// </remarks>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- A very simple example, using a default parameter -->
    ///         <!-- Output: Hi! -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Code="@this.Log.LogMessage(%22Hi!%22);"
    ///                                                         />
    ///         <!-- An example that takes a string argument and returns a string result -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Inputs="string format"
    ///                                                         Outputs="string result"
    ///                                                         Input1="yyyy-MM-dd"
    ///                                                         Code="result = DateTime.Now.ToString(format);"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="FormattedDate"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Output (varies by time): 2009-06-10 -->
    ///         <Message Text="Formatted date: $(FormattedDate)"/>
    ///         <!-- An example that shows the more advanced conversions available for boolean arguments -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Inputs="bool test"
    ///                                                         Outputs="bool result"
    ///                                                         Input1="!no"
    ///                                                         Code="result = test;"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="ConversionTestResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Output: Converts '!no' to: True -->
    ///         <Message Text="Converts '!no' to: $(ConversionTestResult)"/>
    ///         <!-- Take two array arguments and return an array -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Inputs="string[] first, string[] second"
    ///                                                         Outputs="string[] result"
    ///                                                         Input1="1;2;3"
    ///                                                         Input2="10;10;10"
    ///                                                         Code="result = new string[first.Length];   for (int i = 0; i != first.Length; ++i)   result[i] = first[i] + second[i];"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="ArrayTestResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Output: Array test result: 110;210;310 -->
    ///         <Message Text="Array test result: $(ArrayTestResult)"/>
    ///         <!-- Take two array arguments of non-string type and return an array -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Inputs="int[] first, int[] second"
    ///                                                         Outputs="int[] result"
    ///                                                         Input1="1;2;3"
    ///                                                         Input2="10;10;10"
    ///                                                         Code="result = new int[first.Length];   for (int i = 0; i != first.Length; ++i)   result[i] = first[i] + second[i];"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="ArrayTestResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Output: Array test result: 11;12;13 -->
    ///         <Message Text="Array test result: $(ArrayTestResult)"/>
    ///         <!-- A much more complex example: defining a more reusable DynamicTask, that performs a cross product on item groups -->
    ///         <PropertyGroup>
    ///             <CrossProductCode>
    ///                 &lt;![CDATA[
    ///                     if (string.IsNullOrEmpty(separator))
    ///                         separator = ";";
    ///                     result = new TaskItem[itemGroup1.Length * itemGroup2.Length];
    ///                     int i = 0;
    ///                     foreach (ITaskItem item1 in itemGroup1)
    ///                     {
    ///                         foreach (ITaskItem item2 in itemGroup2)
    ///                         {
    ///                             // Determine metadata
    ///                             Dictionary<string, string> metadata = new Dictionary<string, string>();
    ///                             // Copy all metadata from the first item
    ///                             if (string.IsNullOrEmpty(prefix1))
    ///                             {
    ///                                 foreach (string name in item1.MetadataNames)
    ///                                     metadata.Add(name, item1.GetMetadata(name));
    ///                             }
    ///                             else
    ///                             {
    ///                                 foreach (string name in item1.MetadataNames)
    ///                                     metadata.Add(prefix1 + name, item1.GetMetadata(name));
    ///                             }
    ///                             // Copy all metadata from the second item
    ///                             if (string.IsNullOrEmpty(prefix2))
    ///                             {
    ///                                 foreach (string name in item2.MetadataNames)
    ///                                     if (!metadata.ContainsKey(name))
    ///                                         metadata.Add(name, item2.GetMetadata(name));
    ///                             }
    ///                             else
    ///                             {
    ///                                 foreach (string name in item2.MetadataNames)
    ///                                     if (!metadata.ContainsKey(prefix2 + name))
    ///                                         metadata.Add(prefix2 + name, item2.GetMetadata(name));
    ///                             }
    ///                             // Create an output item with a (hopefully unique) itemspec.
    ///                             result[i++] = new TaskItem(item1.ItemSpec + separator + item2.ItemSpec, metadata);
    ///                         }
    ///                     }
    ///                 ]]&gt;
    ///             </CrossProductCode>
    ///         </PropertyGroup>
    ///         <ItemGroup>
    ///             <!-- Parameters and results: these are used in the method definition -->
    ///             <CrossProductParameters Include="itemGroup1;itemGroup2">
    ///                 <Type>ITaskItem[]</Type>
    ///             </CrossProductParameters>
    ///             <CrossProductParameters Include="separator;prefix1;prefix2">
    ///                 <Type>string</Type>
    ///             </CrossProductParameters>
    ///             <CrossProductResults Include="result">
    ///                 <Type>ITaskItem[]</Type>
    ///             </CrossProductResults>
    ///             <!-- Arguments: these are used by the closure -->
    ///             <CrossProductArguments1 Include="x;y;z">
    ///                 <M1>Meta1</M1>
    ///             </CrossProductArguments1>
    ///             <CrossProductArguments2 Include="1;2">
    ///                 <M2>Meta2</M2>
    ///             </CrossProductArguments2>
    ///             <CrossProductArguments2 Include="3">
    ///                 <M1>Meta1 that is overwritten</M1>
    ///                 <M2>A different Meta2</M2>
    ///             </CrossProductArguments2>
    ///         </ItemGroup>
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Inputs="@(CrossProductParameters)"
    ///                                                         Outputs="@(CrossProductResults)"
    ///                                                         Input1="@(CrossProductArguments1)"
    ///                                                         Input2="@(CrossProductArguments2)"
    ///                                                         Code="$(CrossProductCode)"
    ///                                                         >
    ///             <Output TaskParameter="Output1" ItemName="CrossProductResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Output: Cross product:  x;1 { M1=Meta1, M2=Meta2 } ; x;2 { M1=Meta1, M2=Meta2 } ; x;3 { M1=Meta1, M2=A different Meta2 } ; y;1 { M1=Meta1, M2=Meta2 } ; y;2 { M1=Meta1, M2=Meta2 } ; y;3 { M1=Meta1, M2=A different Meta2 } ; z;1 { M1=Meta1, M2=Meta2 } ; z;2 { M1=Meta1, M2=Meta2 } ; z;3 { M1=Meta1, M2=A different Meta2 } -->
    ///         <Message Text="Cross product: @(CrossProductResult->' %(Identity) { M1=%(M1), M2=%(M2) } ')"/>
    ///         <!-- The "Run" and "Call" TaskActions are limited to 3 inputs and 3 outputs (currently), but by separating out each step,
    ///                 any number of inputs and outputs may be specified, and they may be specified by name -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Define"
    ///                                                         Inputs="@(CrossProductParameters)"
    ///                                                         Outputs="@(CrossProductResults)"
    ///                                                         Code="$(CrossProductCode)"
    ///                                                         >
    ///             <Output TaskParameter="OutputMethodId" PropertyName="CrossProductMethodId"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Create"
    ///                                                         MethodId="$(CrossProductMethodId)"
    ///                                                         >
    ///             <Output TaskParameter="OutputClosureId" PropertyName="CrossProductClosureId"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="SetInput"
    ///                                                         ClosureId="$(CrossProductClosureId)"
    ///                                                         Name="itemGroup1"
    ///                                                         InputValue="@(CrossProductArguments1)"
    ///                                                         />
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="SetInput"
    ///                                                         ClosureId="$(CrossProductClosureId)"
    ///                                                         Name="itemGroup2"
    ///                                                         InputValue="@(CrossProductArguments2)"
    ///                                                         />
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="SetInput"
    ///                                                         ClosureId="$(CrossProductClosureId)"
    ///                                                         Name="prefix1"
    ///                                                         InputValue="P1_"
    ///                                                         />
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="SetInput"
    ///                                                         ClosureId="$(CrossProductClosureId)"
    ///                                                         Name="prefix2"
    ///                                                         InputValue="P2_"
    ///                                                         />
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="SetInput"
    ///                                                         ClosureId="$(CrossProductClosureId)"
    ///                                                         Name="separator"
    ///                                                         InputValue="."
    ///                                                         />
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Invoke"
    ///                                                         ClosureId="$(CrossProductClosureId)"
    ///                                                         />
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="GetOutput"
    ///                                                         ClosureId="$(CrossProductClosureId)"
    ///                                                         Name="result"
    ///                                                         >
    ///             <Output TaskParameter="OutputValue" ItemName="ComplexCrossProductResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Output: Cross product with more parameters:  x.1 { P1_M1=Meta1, P2_M1=, P2_M2=Meta2 } ; x.2 { P1_M1=Meta1, P2_M1=, P2_M2=Meta2 } ; x.3 { P1_M1=Meta1, P2_M1=Meta1 that is overwritten, P2_M2=A different Meta2 } ; y.1 { P1_M1=Meta1, P2_M1=, P2_M2=Meta2 } ; y.2 { P1_M1=Meta1, P2_M1=, P2_M2=Meta2 } ; y.3 { P1_M1=Meta1, P2_M1=Meta1 that is overwritten, P2_M2=A different Meta2 } ; z.1 { P1_M1=Meta1, P2_M1=, P2_M2=Meta2 } ; z.2 { P1_M1=Meta1, P2_M1=, P2_M2=Meta2 } ; z.3 { P1_M1=Meta1, P2_M1=Meta1 that is overwritten, P2_M2=A different Meta2 } -->
    ///         <Message Text="Cross product with more parameters: @(ComplexCrossProductResult->' %(Identity) { P1_M1=%(P1_M1), P2_M1=%(P2_M1), P2_M2=%(P2_M2) } ')"/>
    ///         <!-- Testing nullable parameter values -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Inputs="int? arg"
    ///                                                         Outputs="int? result"
    ///                                                         Input1="33"
    ///                                                         Code="result = arg + 3;"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="DefaultResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Output: 33 + 3: 36 -->
    ///         <Message Text="33 + 3: $(DefaultResult)"/>
    ///         <!-- Testing nullable parameter values with null argument (the output value is actually null in this case, which MSBuild converts to an empty string) -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Inputs="int? arg"
    ///                                                         Outputs="int? result"
    ///                                                         Code="result = arg + 3;"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="DefaultResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Output: default(int?) + 3: -->
    ///         <Message Text="default(int?) + 3: $(DefaultResult)"/>
    ///         <!-- Testing parameter values with default argument -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Inputs="int arg"
    ///                                                         Outputs="int? result"
    ///                                                         Code="result = arg + 3;"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="DefaultResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Output: default(int) + 3: 3 -->
    ///         <Message Text="default(int) + 3: $(DefaultResult)"/>
    ///         <!-- Defining a method once and calling it multiple times (this is more resource-efficient than always using Run) -->
    ///         <!--   (the GUID-testing regex was taken from the Regular Expression Library, http://regexlib.com/ ) -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Define"
    ///                                                         Inputs="string arg"
    ///                                                         Outputs="bool result"
    ///                                                         UsingNamespaces="System.Text.RegularExpressions"
    ///                                                         Code="result = Regex.IsMatch(arg, @%22[{|\(]?[0-9a-fA-F]{8}[-]?([0-9a-fA-F]{4}[-]?){3}[0-9a-fA-F]{12}[\)|}]?%22);"
    ///                                                         >
    ///             <Output TaskParameter="OutputMethodId" PropertyName="IsGuidMethod"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- The first call -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Call"
    ///                                                         MethodId="$(IsGuidMethod)"
    ///                                                         Input1="{914D226A-2F5B-4944-934D-96BBE6571977}"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="IsGuidResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Output: IsGuid({914D226A-2F5B-4944-934D-96BBE6571977}): True -->
    ///         <Message Text="IsGuid({914D226A-2F5B-4944-934D-96BBE6571977}): $(IsGuidResult)"/>
    ///         <!-- The second call; recompiling the method is unnecessary for this call -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Call"
    ///                                                         MethodId="$(IsGuidMethod)"
    ///                                                         Input1="{X14D226A-2F5B-4944-934D-96BBE6571977}"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="IsGuidResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Output: IsGuid({X14D226A-2F5B-4944-934D-96BBE6571977}): False -->
    ///         <Message Text="IsGuid({X14D226A-2F5B-4944-934D-96BBE6571977}): $(IsGuidResult)"/>
    ///         <!-- Using a parameter to define part of the code for a Run task action. -->
    ///         <PropertyGroup>
    ///             <MathArgument>42 - 37</MathArgument>
    ///         </PropertyGroup>
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         Outputs="int result"
    ///                                                         Code="result = 2 * ($(MathArgument));"
    ///                                                         >
    ///             <Output TaskParameter="Output1" PropertyName="MathResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DynamicExecute>
    ///         <!-- Output: 2 * (42 - 37) = 10 -->
    ///         <Message Text="2 * (42 - 37) = $(MathResult)"/>
    ///         <!-- Other assemblies may also be referenced, along with optional "using namespace" declarations -->
    ///         <!-- Output: {"Hi from Windows Forms!" in a MessageBox} -->
    ///         <MSBuild.ExtensionPack.Framework.DynamicExecute TaskAction="Run"
    ///                                                         References="System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
    ///                                                         UsingNamespaces="System.Windows;System.Windows.Forms"
    ///                                                         Code="MessageBox.Show(%22Hi from Windows Forms!%22);"
    ///                                                         />
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public sealed class DynamicExecute : BaseTask
    {
        private const string DefineTaskAction = "Define";
        private const string CreateTaskAction = "Create";
        private const string SetInputTaskAction = "SetInput";
        private const string InvokeTaskAction = "Invoke";
        private const string GetOutputTaskAction = "GetOutput";
        private const string DestroyTaskAction = "Destroy";
        private const string CallTaskAction = "Call";
        private const string RunTaskAction = "Run";

        /// <summary>
        /// The shared collection of method definitions. Once defined, a method is never undefined.
        /// </summary>
        private static readonly Dictionary<string, MethodDefinition> Methods = new Dictionary<string, MethodDefinition>();

        /// <summary>
        /// The shared collection of closure instances. Created closures may be destroyed at a later time.
        /// </summary>
        private static readonly Dictionary<string, Closure> Closures = new Dictionary<string, Closure>();

        /// <summary>
        /// Specifies the inputs for <see cref="Code"/>. Each input has a type and a name.
        /// </summary>
        /// <remarks>
        /// <para>This is an optional parameter for the <b>Define</b> and <b>Run</b> task actions.</para>
        /// <para>This may be set to a string containing a comma-delimited or semicolon-delimited sequence of (type, name) pairs.</para>
        /// <para>Otherwise, each input is represented by a task item. The name of an input is taken from the metadata "Name", if it exists; otherwise, it is taken from the item's identity. The type of an input is taken from the metadata "Type".</para>
        /// </remarks>
        /// <seealso cref="NoDefaultParameters"/>
        public ITaskItem[] Inputs { get; set; }

        /// <summary>
        /// Specifies the outputs for <see cref="Code"/>. Each output has a type and a name.
        /// </summary>
        /// <remarks>
        /// <para>This is an optional parameter for the <b>Define</b> and <b>Run</b> task actions.</para>
        /// <para>This may be set to a string containing a comma-delimited or semicolon-delimited sequence of (type, name) pairs.</para>
        /// <para>Otherwise, each output is represented by a task item. The name of an output is taken from the metadata "Name", if it exists; otherwise, it is the item's identity. The type of an output is taken from the metadata "Type".</para>
        /// </remarks>
        public ITaskItem[] Outputs { get; set; }

        /// <summary>
        /// Specifies additional references for <see cref="Code"/>.
        /// </summary>
        /// <remarks>
        /// <para>This is an optional parameter for the <b>Define</b> and <b>Run</b> task actions.</para>
        /// <para>The name of a reference is taken from the metadata "Name", if it exists; otherwise, it is the item's identity.</para>
        /// <para>To reference assemblies in the GAC, a strong name must be used, e.g., "System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089".</para>
        /// <para>The default references are System (System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089), System.Core (System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089), Microsoft.Build.Framework (Microsoft.Build.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a), and Microsoft.Build.Utilities.v4.0 (Microsoft.Build.Utilities.v4.0, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a).</para>
        /// </remarks>
        /// <seealso cref="NoDefaultReferences"/>
        public ITaskItem[] References { get; set; }

        /// <summary>
        /// Specifies additional "using namespaces" for <see cref="Code"/>. These are namespaces that are brought into the code's scope.
        /// </summary>
        /// <remarks>
        /// <para>This is an optional parameter for the <b>Define</b> and <b>Run</b> task actions.</para>
        /// <para>The name of a namespace is taken from the metadata "Name", if it exists; otherwise, it is the item's identity.</para>
        /// <para>The default namespaces are System, System.Collections.Generic, System.Linq, System.Text, Microsoft.Build.Framework, and Microsoft.Build.Utilities.</para>
        /// </remarks>
        /// <seealso cref="NoDefaultUsingNamespaces"/>
        public ITaskItem[] UsingNamespaces { get; set; }

        /// <summary>
        /// The actual method code for dynamic execution.
        /// </summary>
        /// <remarks>
        /// <para>This is a required parameter for the <b>Define</b> and <b>Run</b> task actions.</para>
        /// <para>This code is treated as a method body when compiled. The method does not return a value; rather, outputs are passed as reference parameters to the method.</para>
        /// </remarks>
        public string Code { get; set; }

        /// <summary>
        /// Specifies to not include the default "using namespaces" for <see cref="Code"/>.
        /// </summary>
        /// <remarks>
        /// <para>This is an optional parameter for the <b>Define</b> and <b>Run</b> task actions.</para>
        /// </remarks>
        /// <seealso cref="UsingNamespaces"/>
        public bool NoDefaultUsingNamespaces { get; set; }

        /// <summary>
        /// Specifies to not include the default references for <see cref="Code"/>.
        /// </summary>
        /// <remarks>
        /// <para>This is an optional parameter for the <b>Define</b> and <b>Run</b> task actions.</para>
        /// </remarks>
        /// <seealso cref="References"/>
        public bool NoDefaultReferences { get; set; }

        /// <summary>
        /// Specifies to not define the default parameters for <see cref="Code"/>.
        /// </summary>
        /// <remarks>
        /// <para>This is an optional parameter for the <b>Define</b> and <b>Run</b> task actions.</para>
        /// </remarks>
        /// <seealso cref="Inputs"/>
        public bool NoDefaultParameters { get; set; }

        /// <summary>
        /// The identifier of the method definition.
        /// </summary>
        /// <remarks>
        /// <para>This is a required parameter for the <b>Create</b> and <b>Call</b> task actions.</para>
        /// </remarks>
        public string MethodId { get; set; }

        /// <summary>
        /// The identifier of the closure instance.
        /// </summary>
        /// <remarks>
        /// <para>This is a required parameter for the <b>SetInput</b>, <b>Invoke</b>, <b>GetOutput</b>, and <b>Destroy</b> task actions.</para>
        /// </remarks>
        public string ClosureId { get; set; }

        /// <summary>
        /// The name of the input argument to set, or the output argument to retrieve.
        /// </summary>
        /// <remarks>
        /// <para>This is a required parameter for the <b>SetInput</b> and <b>GetOutput</b> task actions.</para>
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// The value to set.
        /// </summary>
        /// <remarks>
        /// <para>This is a required parameter for the <b>SetInput</b> task action.</para>
        /// </remarks>
        public ITaskItem[] InputValue { get; set; }

        /// <summary>
        /// The value for the first input parameter.
        /// </summary>
        /// <remarks>
        /// <para>This is an optional parameter for the <b>Run</b> and <b>Call</b> task actions.</para>
        /// </remarks>
        public ITaskItem[] Input1 { get; set; }

        /// <summary>
        /// The value for the second input parameter.
        /// </summary>
        /// <remarks>
        /// <para>This is an optional parameter for the <b>Run</b> and <b>Call</b> task actions.</para>
        /// </remarks>
        public ITaskItem[] Input2 { get; set; }

        /// <summary>
        /// The value for the third input parameter.
        /// </summary>
        /// <remarks>
        /// <para>This is an optional parameter for the <b>Run</b> and <b>Call</b> task actions.</para>
        /// </remarks>
        public ITaskItem[] Input3 { get; set; }

        /// <summary>
        /// The ID of a defined method.
        /// </summary>
        /// <remarks>
        /// <para>This is an output for the <b>Define</b> and <b>Run</b> task actions.</para>
        /// </remarks>
        [Output]
        public string OutputMethodId { get; private set; }

        /// <summary>
        /// The ID of a closure instance.
        /// </summary>
        /// <remarks>
        /// <para>This is an output for the <b>Create</b> task action.</para>
        /// </remarks>
        [Output]
        public string OutputClosureId { get; private set; }

        /// <summary>
        /// The value of a closure output.
        /// </summary>
        /// <remarks>
        /// <para>This is an output for the <b>GetOutput</b> task action.</para>
        /// </remarks>
        [Output]
        public ITaskItem[] OutputValue { get; private set; }

        /// <summary>
        /// The value of the first closure output.
        /// </summary>
        /// <remarks>
        /// <para>This is an output for the <b>Run</b> and <b>Call</b> task actions.</para>
        /// </remarks>
        [Output]
        public ITaskItem[] Output1 { get; private set; }

        /// <summary>
        /// The value of the second closure output.
        /// </summary>
        /// <remarks>
        /// <para>This is an output for the <b>Run</b> and <b>Call</b> task actions.</para>
        /// </remarks>
        [Output]
        public ITaskItem[] Output2 { get; private set; }

        /// <summary>
        /// The value of the third closure output.
        /// </summary>
        /// <remarks>
        /// <para>This is an output for the <b>Run</b> and <b>Call</b> task actions.</para>
        /// </remarks>
        [Output]
        public ITaskItem[] Output3 { get; private set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine(false))
            {
                return;
            }

            switch (this.TaskAction)
            {
                case DefineTaskAction:
                    if (string.IsNullOrEmpty(this.Code))
                    {
                        this.Log.LogError("DynamicExecute.Code is not optional for TaskAction=Define");
                        return;
                    }

                    this.ExecuteDefine();
                    break;
                case CreateTaskAction:
                    if (string.IsNullOrEmpty(this.MethodId))
                    {
                        this.Log.LogError("DynamicExecute.MethodId is not optional for TaskAction=Create");
                        return;
                    }

                    this.ExecuteCreate();
                    break;
                case SetInputTaskAction:
                    if (string.IsNullOrEmpty(this.ClosureId))
                    {
                        this.Log.LogError("DynamicExecute.ClosureId is not optional for TaskAction=SetInput");
                        return;
                    }

                    if (string.IsNullOrEmpty(this.Name))
                    {
                        this.Log.LogError("DynamicExecute.Name is not optional for TaskAction=SetInput");
                        return;
                    }

                    if (this.InputValue == null)
                    {
                        this.Log.LogError("DynamicExecute.InputValue is not optional for TaskAction=SetInput");
                        return;
                    }

                    this.ExecuteSetInput();
                    break;
                case InvokeTaskAction:
                    if (string.IsNullOrEmpty(this.ClosureId))
                    {
                        this.Log.LogError("DynamicExecute.ClosureId is not optional for TaskAction=Invoke");
                        return;
                    }

                    this.ExecuteInvoke();
                    break;
                case GetOutputTaskAction:
                    if (string.IsNullOrEmpty(this.ClosureId))
                    {
                        this.Log.LogError("DynamicExecute.ClosureId is not optional for TaskAction=GetOutput");
                        return;
                    }

                    if (string.IsNullOrEmpty(this.Name))
                    {
                        this.Log.LogError("DynamicExecute.Name is not optional for TaskAction=GetOutput");
                        return;
                    }

                    this.ExecuteGetOutput();
                    break;
                case DestroyTaskAction:
                    if (string.IsNullOrEmpty(this.ClosureId))
                    {
                        this.Log.LogError("DynamicExecute.ClosureId is not optional for TaskAction=Destroy");
                        return;
                    }

                    this.ExecuteDestroy();
                    break;
                case CallTaskAction:
                    if (string.IsNullOrEmpty(this.MethodId))
                    {
                        this.Log.LogError("DynamicExecute.MethodId is not optional for TaskAction=Call");
                        return;
                    }

                    this.ExecuteCall();
                    break;
                case RunTaskAction:
                    if (string.IsNullOrEmpty(this.Code))
                    {
                        this.Log.LogError("DynamicExecute.Code is not optional for TaskAction=Run");
                        return;
                    }

                    this.ExecuteRun();
                    break;
                default:
                    this.Log.LogError("Unknown TaskAction \"" + (this.TaskAction ?? "<null>") + "\" for DynamicExecute");
                    return;
            }
        }

        /// <summary>
        /// Converts a scalar MSBuild input value into a method input value.
        /// </summary>
        /// <param name="type">The input type that the method is expecting.</param>
        /// <param name="value">The MSBuild input value.</param>
        /// <returns>A method input value.</returns>
        private static object ConvertScalarArgument(Type type, ITaskItem value)
        {
            Type underlyingNullableType = Nullable.GetUnderlyingType(type);
            if (underlyingNullableType != null)
            {
                type = underlyingNullableType;
            }

            if (type == typeof(bool))
            {
                string val = value.ItemSpec;
                bool? boolVal = null;
                bool invert = false;
                if (val.StartsWith("!", StringComparison.OrdinalIgnoreCase))
                {
                    invert = true;
                    val = val.Substring(1);
                }

                if (string.Equals(val, "true", StringComparison.OrdinalIgnoreCase))
                {
                    boolVal = true;
                }

                if (string.Equals(val, "false", StringComparison.OrdinalIgnoreCase))
                {
                    boolVal = false;
                }

                if (string.Equals(val, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    boolVal = true;
                }

                if (string.Equals(val, "no", StringComparison.OrdinalIgnoreCase))
                {
                    boolVal = false;
                }

                if (string.Equals(val, "on", StringComparison.OrdinalIgnoreCase))
                {
                    boolVal = true;
                }

                if (string.Equals(val, "off", StringComparison.OrdinalIgnoreCase))
                {
                    boolVal = false;
                }

                if (boolVal != null)
                {
                    if (invert)
                    {
                        return !boolVal.Value;
                    }

                    return boolVal.Value;
                }
            }

            return System.Convert.ChangeType(value.ItemSpec, type, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts an MSBuild input value into a method input value.
        /// </summary>
        /// <param name="type">The input type that the method is expecting.</param>
        /// <param name="value">The MSBuild input value.</param>
        /// <returns>A method input value.</returns>
        private static object ConvertArgument(Type type, ITaskItem[] value)
        {
            if (type == typeof(ITaskItem[]))
            {
                return value;
            }

            if (value == null || value.Length == 0)
            {
                return null;
            }

            if (type.IsArray)
            {
                Array ret = Array.CreateInstance(type.GetElementType(), value.Length);
                for (int i = 0; i != value.Length; ++i)
                {
                    ret.SetValue(ConvertScalarArgument(type.GetElementType(), value[i]), i);
                }

                return ret;
            }

            if (value.Length != 1)
            {
                throw new ArgumentException("Attempted to pass a vector value as a scalar argument");
            }

            if (type == typeof(ITaskItem))
            {
                return value[0];
            }

            return ConvertScalarArgument(type, value[0]);
        }

        /// <summary>
        /// Converts a single, scalar method output value into a scalar MSBuild output value.
        /// </summary>
        /// <param name="value">The value returned by the method.</param>
        /// <returns>An MSBuild output value.</returns>
        private static ITaskItem ConvertScalarResult(object value)
        {
            string stringValue = (string)System.Convert.ChangeType(value, typeof(string), CultureInfo.InvariantCulture);
            return new TaskItem(stringValue);
        }

        /// <summary>
        /// Converts a single method output value into an MSBuild output value.
        /// </summary>
        /// <param name="value">The value returned by the method.</param>
        /// <returns>An MSBuild output value.</returns>
        private static ITaskItem[] ConvertResult(object value)
        {
            // All result values are boxed, so nullable types without values are just null object references
            if (value == null)
            {
                return new ITaskItem[0];
            }

            Type type = value.GetType();
            if (type == typeof(ITaskItem[]) || type == typeof(TaskItem[]))
            {
                return (ITaskItem[])value;
            }

            if (type == typeof(ITaskItem) || type == typeof(TaskItem))
            {
                return new[] { (ITaskItem)value };
            }

            if (type.IsArray)
            {
                Array val = (Array)value;
                ITaskItem[] ret = new ITaskItem[val.Length];
                for (int i = 0; i != val.Length; ++i)
                {
                    ret[i] = ConvertScalarResult(val.GetValue(i));
                }

                return ret;
            }

            return new[] { ConvertScalarResult(value) };
        }

        /// <summary>
        /// Returns the value of the "Name" metadata, if it exists. Otherwise, the item specification is returned.
        /// </summary>
        /// <param name="item">The task item to inspect.</param>
        /// <returns>The item's name or identity.</returns>
        private static string NameOrIdentity(ITaskItem item)
        {
            return item.MetadataNames.Cast<string>().Contains("Name") ? item.GetMetadata("Name") : item.ItemSpec;
        }

        /// <summary>
        /// Defines a method.
        /// </summary>
        /// <param name="method">The compiled method code.</param>
        /// <param name="inputs">The definitions of the inputs to the method.</param>
        /// <param name="outputs">The definitions of the outputs from the method.</param>
        /// <param name="numberOfDefaultParameters">The number of default parameters for the method.</param>
        /// <returns>The ID of the method definition.</returns>
        private static string DefineMethod(MethodInfo method, IEnumerable<string> inputs, IEnumerable<string> outputs, int numberOfDefaultParameters)
        {
            lock (Methods)
            {
                string id = System.Guid.NewGuid().ToString();
                Methods.Add(id, new MethodDefinition(method, numberOfDefaultParameters, inputs, outputs));
                return id;
            }
        }

        /// <summary>
        /// Retrieves a previously-defined method by ID.
        /// </summary>
        /// <param name="methodId">The ID of the method definition.</param>
        /// <returns>The method definition.</returns>
        private static MethodDefinition LookupMethod(string methodId)
        {
            lock (Methods)
            {
                if (!Methods.ContainsKey(methodId))
                {
                    throw new KeyNotFoundException("Unknown DynamicExecute method id: " + methodId);
                }

                return Methods[methodId];
            }
        }

        /// <summary>
        /// Creates a new closure and adds it to the container of existing closures.
        /// </summary>
        /// <param name="methodId">The ID of the method definition.</param>
        /// <returns>The ID of the new closure instance.</returns>
        private static string CreateClosure(string methodId)
        {
            lock (Methods)
            {
                if (!Methods.ContainsKey(methodId))
                {
                    throw new KeyNotFoundException("Unknown DynamicExecute method id: " + methodId);
                }

                string ret = System.Guid.NewGuid().ToString();
                Closures.Add(ret, new Closure(Methods[methodId]));
                return ret;
            }
        }

        /// <summary>
        /// Retrieves a previously-created closure by id.
        /// </summary>
        /// <param name="closureId">The ID of the previously-created closure.</param>
        /// <returns>The previously-created closure.</returns>
        private static Closure LookupClosure(string closureId)
        {
            lock (Methods)
            {
                if (!Closures.ContainsKey(closureId))
                {
                    throw new KeyNotFoundException("Unknown DynamicExecute closure id: " + closureId);
                }

                return Closures[closureId];
            }
        }

        /// <summary>
        /// Deletes a closure. If the closure id does not identify a previously-created closure, no error is thrown.
        /// </summary>
        /// <param name="closureId">The ID of the closure to delete.</param>
        private static void DestroyClosure(string closureId)
        {
            lock (Methods)
            {
                Closures.Remove(closureId);
            }
        }

        /// <summary>
        /// Splits a string containing a type followed by a name, separated by any number of space characters.
        /// </summary>
        /// <param name="typeAndName">The string containing the type and name.</param>
        /// <returns>An object containing the split string.</returns>
        private static NameAndType ParseTypeAndName(string typeAndName)
        {
            string[] typeandname = typeAndName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (typeandname.Length != 2)
            {
                throw new ArgumentException("Inputs/Outputs definition not valid: \"" + typeAndName + "\" is not a valid Type/Name pair");
            }

            return new NameAndType { Type = typeandname[0], Name = typeandname[1] };
        }

        /// <summary>
        /// Parses the inputs or outputs, yielding a sequence of <see cref="NameAndType"/> objects.
        /// </summary>
        /// <param name="inputsOutputs">The inputs or outputs value.</param>
        /// <returns>A sequence of <see cref="NameAndType"/> objects.</returns>
        private static IEnumerable<NameAndType> ParseInputsOutputs(ITaskItem[] inputsOutputs)
        {
            // If no inputs or outputs are defined, then yield an empty sequence.
            if (inputsOutputs == null || inputsOutputs.Length == 0)
            {
                yield break;
            }

            // We test for spaces in the ItemSpecs; these are invalid in identifiers but are required in both minilanguages

            // If there is only one definition and it has a space, treat it as a comma-delimited minilanguage
            if (inputsOutputs.Length == 1 && inputsOutputs[0].ItemSpec.Contains(" "))
            {
                // First, split by commas
                IEnumerable<string> typesAndNames = from x in inputsOutputs[0].ItemSpec.Split(',') select x.Trim();

                // Second, split by spaces (allowing multiple spaces)
                foreach (string x in typesAndNames)
                {
                    yield return ParseTypeAndName(x);
                }

                yield break;
            }

            // Each entry may have the type and name in its ItemSpec, or it may have the type in its metadata
            foreach (ITaskItem x in inputsOutputs)
            {
                if (x.ItemSpec.Contains(" "))
                {
                    yield return ParseTypeAndName(x.ItemSpec);
                }
                else
                {
                    yield return new NameAndType { Name = NameOrIdentity(x), Type = x.GetMetadata("Type") };
                }
            }
        }

        /// <summary>
        /// Returns the number of default parameters for a method currently being defined.
        /// </summary>
        /// <returns>The number of default parameters.</returns>
        private int NumberOfDefaultParameters()
        {
            // Currently, there is only one default parameter
            return this.NoDefaultParameters ? 0 : 1;
        }

        /// <summary>
        /// Sets all the default parameters and then executes the closure.
        /// </summary>
        /// <param name="closure">The closure to execute.</param>
        private void Execute(Closure closure)
        {
            closure.SetDefaultArgument(0, this);

            try
            {
                closure.Run();
            }
            catch (Exception ex)
            {
                this.Log.LogError("Uncaught exception from DynamicExecute method: [" + ex.GetType().Name + "] " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Executes the Run task action.
        /// </summary>
        private void ExecuteRun()
        {
            // Define the method
            this.ExecuteDefine();
            this.MethodId = this.OutputMethodId;

            this.ExecuteCall();
        }

        /// <summary>
        /// Executes the Call task action.
        /// </summary>
        private void ExecuteCall()
        {
            // Create the closure
            Closure closure = new Closure(LookupMethod(this.MethodId));

            // Set parameters
            if (this.Input1 != null)
            {
                closure.SetArgument(0, ConvertArgument(closure.GetInputParameterType(0), this.Input1));
            }

            if (this.Input2 != null)
            {
                closure.SetArgument(1, ConvertArgument(closure.GetInputParameterType(1), this.Input2));
            }

            if (this.Input3 != null)
            {
                closure.SetArgument(2, ConvertArgument(closure.GetInputParameterType(2), this.Input3));
            }

            // Invoke the closure
            this.Execute(closure);

            // Retrieve results
            this.Output1 = ConvertResult(closure.TryGetOutput(0));
            this.Output2 = ConvertResult(closure.TryGetOutput(1));
            this.Output3 = ConvertResult(closure.TryGetOutput(2));
        }

        /// <summary>
        /// Executes the Define task action.
        /// </summary>
        private void ExecuteDefine()
        {
            StringBuilder code = new StringBuilder();
            if (!this.NoDefaultUsingNamespaces)
            {
                code.AppendLine("using System;");
                code.AppendLine("using System.Collections.Generic;");
                code.AppendLine("using System.Linq;");
                code.AppendLine("using System.Text;");
                code.AppendLine("using Microsoft.Build.Framework;");
                code.AppendLine("using Microsoft.Build.Utilities;");
            }

            if (this.UsingNamespaces != null)
            {
                foreach (ITaskItem item in this.UsingNamespaces)
                {
                    string name = NameOrIdentity(item);
                    code.AppendLine("using " + name + ";");
                }
            }

            code.AppendLine("namespace MSBuild.ExtensionPack.Framework {");
            code.AppendLine("public static class T {");
            code.Append("public static void Go(");

            if (!this.NoDefaultParameters)
            {
                code.Append("Microsoft.Build.Utilities.Task @this");
            }

            // Parameter definitions (inputs and outputs)
            IEnumerable<NameAndType> inputs = ParseInputsOutputs(this.Inputs);
            foreach (NameAndType nameAndType in inputs)
            {
                code.Append("," + nameAndType.Type + " " + nameAndType.Name);
            }

            IEnumerable<NameAndType> outputs = ParseInputsOutputs(this.Outputs);
            foreach (NameAndType nameAndType in outputs)
            {
                code.Append(",ref " + nameAndType.Type + " " + nameAndType.Name);
            }

            code.AppendLine(") {");
            code.AppendLine(this.Code);
            code.AppendLine("} } }");

            // Prepare references
            CompilerParameters parameters = new CompilerParameters { GenerateInMemory = true };
            if (!this.NoDefaultReferences)
            {
                parameters.ReferencedAssemblies.Add(System.Reflection.Assembly.Load("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").Location);
                parameters.ReferencedAssemblies.Add(System.Reflection.Assembly.Load("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").Location);
                parameters.ReferencedAssemblies.Add(System.Reflection.Assembly.Load("Microsoft.Build.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location);
                parameters.ReferencedAssemblies.Add(System.Reflection.Assembly.Load("Microsoft.Build.Utilities.v4.0, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location);
            }

            if (this.References != null)
            {
                foreach (ITaskItem item in this.References)
                {
                    string name = NameOrIdentity(item);
                    parameters.ReferencedAssemblies.Add(System.Reflection.Assembly.Load(name).Location);
                }
            }

            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            {
                // Compile it
                CompilerResults results = provider.CompileAssemblyFromSource(parameters, code.ToString());
                if (results.Errors.Count != 0)
                {
                    bool onlyWarnings = true;
                    foreach (CompilerError error in results.Errors)
                    {
                        if (error.IsWarning)
                        {
                            this.LogTaskWarning(error.ErrorNumber + ": " + error.ErrorText);
                        }
                        else
                        {
                            this.Log.LogError(error.ErrorNumber + ": " + error.ErrorText);
                            onlyWarnings = false;
                        }
                    }

                    if (!onlyWarnings)
                    {
                        throw new InvalidProgramException("Compilation of DynamicExecute method failed");
                    }
                }

                // Load the compiled method
                System.Reflection.Assembly result = results.CompiledAssembly;
                Type type = result.GetType("MSBuild.ExtensionPack.Framework.T");
                MethodInfo method = type.GetMethod("Go");
                this.OutputMethodId = DefineMethod(method, inputs.Select(x => x.Name), outputs.Select(x => x.Name), this.NumberOfDefaultParameters());
            }
        }

        /// <summary>
        /// Executes the Create task action.
        /// </summary>
        private void ExecuteCreate()
        {
            this.OutputClosureId = CreateClosure(this.MethodId);
        }

        /// <summary>
        /// Executes the SetInput task action.
        /// </summary>
        private void ExecuteSetInput()
        {
            Closure closure = LookupClosure(this.ClosureId);
            closure.SetArgument(this.Name, ConvertArgument(closure.GetInputParameterType(this.Name), this.InputValue));
        }

        /// <summary>
        /// Executes the Invoke task action.
        /// </summary>
        private void ExecuteInvoke()
        {
            this.Execute(LookupClosure(this.ClosureId));
        }

        /// <summary>
        /// Executes the GetOutput task action.
        /// </summary>
        private void ExecuteGetOutput()
        {
            Closure closure = LookupClosure(this.ClosureId);
            this.OutputValue = ConvertResult(closure.GetOutput(this.Name));
        }

        /// <summary>
        /// Executes the Destroy task action.
        /// </summary>
        private void ExecuteDestroy()
        {
            DestroyClosure(this.ClosureId);
        }

        /// <summary>
        /// Represents a compiled DynamicExecute method definition.
        /// </summary>
        private class MethodDefinition
        {
            /// <summary>
            /// The number of default parameters for this method.
            /// </summary>
            private readonly int numberOfDefaultParameters;

            /// <summary>
            /// The names of input parameters for this method.
            /// </summary>
            private readonly string[] inputs;

            /// <summary>
            /// The names of output parameters for this method.
            /// </summary>
            private readonly string[] outputs;

            /// <summary>
            /// The actual compiled method.
            /// </summary>
            private readonly MethodInfo compiledMethod;

            /// <summary>
            /// Initializes a new instance of the <see cref="MethodDefinition"/> class, creating a new method definition.
            /// </summary>
            /// <param name="compiledMethod">The underlying compiled method.</param>
            /// <param name="numberOfDefaultParameters">The number of default parameters for this method.</param>
            /// <param name="inputs">The inputs for the method.</param>
            /// <param name="outputs">The outputs for the method.</param>
            public MethodDefinition(MethodInfo compiledMethod, int numberOfDefaultParameters, IEnumerable<string> inputs, IEnumerable<string> outputs)
            {
                this.compiledMethod = compiledMethod;
                this.numberOfDefaultParameters = numberOfDefaultParameters;
                this.inputs = inputs.ToArray();
                this.outputs = outputs.ToArray();
            }

            /// <summary>
            /// Returns the actual compiled method.
            /// </summary>
            public MethodInfo CompiledMethod
            {
                get { return this.compiledMethod; }
            }

            /// <summary>
            /// Returns the total number of parameters required to invoke the method (including default, explicit, and return values).
            /// </summary>
            public int NumberOfParameters
            {
                get { return this.numberOfDefaultParameters + this.inputs.Length + this.outputs.Length; }
            }

            /// <summary>
            /// Returns the argument index for the given default parameter, or -1 if it is not defined.
            /// </summary>
            /// <param name="defaultIndex">The zero-based index of the default parameter.</param>
            /// <returns>The argument index for the default parameter, or -1 if that parameter is not defined.</returns>
            public int GetDefaultArgumentIndex(int defaultIndex)
            {
                if (defaultIndex >= 0 && defaultIndex < this.numberOfDefaultParameters)
                {
                    return defaultIndex;
                }

                return -1;
            }

            /// <summary>
            /// Returns the argument index for the given input parameter, or -1 if it is not defined.
            /// </summary>
            /// <param name="inputIndex">The zero-based index of the input parameter.</param>
            /// <returns>The argument index for the input parameter, or -1 if that parameter is not defined.</returns>
            public int GetInputArgumentIndex(int inputIndex)
            {
                if (inputIndex < 0 || inputIndex >= this.inputs.Length)
                {
                    return -1;
                }

                return this.numberOfDefaultParameters + inputIndex;
            }

            /// <summary>
            /// Returns the argument index for the given input parameter, or -1 if it is not defined.
            /// </summary>
            /// <param name="inputName">The name of the input parameter.</param>
            /// <returns>The argument index for the input parameter, or -1 if that parameter is not defined.</returns>
            public int GetInputArgumentIndex(string inputName)
            {
                for (int i = 0; i != this.inputs.Length; ++i)
                {
                    if (this.inputs[i] == inputName)
                    {
                        return this.numberOfDefaultParameters + i;
                    }
                }

                return -1;
            }

            /// <summary>
            /// Returns the argument index for the given output parameter, or -1 if it is not defined.
            /// </summary>
            /// <param name="outputIndex">The zero-based index of the output parameter.</param>
            /// <returns>The argument index for the output parameter, or -1 if that parameter is not defined.</returns>
            public int GetOutputArgumentIndex(int outputIndex)
            {
                if (outputIndex < 0 || outputIndex >= this.outputs.Length)
                {
                    return -1;
                }

                return this.numberOfDefaultParameters + this.inputs.Length + outputIndex;
            }

            /// <summary>
            /// Returns the argument index for the given output parameter, or -1 if it is not defined.
            /// </summary>
            /// <param name="outputName">The name of the output parameter.</param>
            /// <returns>The argument index for the output parameter, or -1 if that parameter is not defined.</returns>
            public int GetOutputArgumentIndex(string outputName)
            {
                for (int i = 0; i != this.outputs.Length; ++i)
                {
                    if (this.outputs[i] == outputName)
                    {
                        return this.numberOfDefaultParameters + this.inputs.Length + i;
                    }
                }

                return -1;
            }
        }

        /// <summary>
        /// Represents a closure, including values for the default, input, and output parameters.
        /// </summary>
        /// <remarks>
        /// <para>A "parameter index" is a 0-based index into the array of parameters. It may refer to a default, input, or output parameter.</para>
        /// </remarks>
        private class Closure
        {
            /// <summary>
            /// The underlying method definition.
            /// </summary>
            private readonly MethodDefinition methodDefinition;

            /// <summary>
            /// The arguments (and return values) for this closure.
            /// </summary>
            private readonly object[] arguments;

            /// <summary>
            /// Initializes a new instance of the <see cref="Closure"/> class. Creates a new closure, allocating space for the parameters.
            /// </summary>
            /// <param name="methodDefinition">The method definition used to create the new closure.</param>
            public Closure(MethodDefinition methodDefinition)
            {
                this.methodDefinition = methodDefinition;
                this.arguments = new object[methodDefinition.NumberOfParameters];
            }

            /// <summary>
            /// Sets a default argument to a value. Invalid default parameter indices are ignored.
            /// </summary>
            /// <param name="defaultParameterIndex">The zero-based index of the default parameter to set.</param>
            /// <param name="value">The value to set.</param>
            public void SetDefaultArgument(int defaultParameterIndex, object value)
            {
                int i = this.methodDefinition.GetDefaultArgumentIndex(defaultParameterIndex);
                if (i != -1)
                {
                    this.arguments[i] = value;
                }
            }

            /// <summary>
            /// Sets an input argument to a value.
            /// </summary>
            /// <param name="inputIndex">The zero-based index of the input parameter to set.</param>
            /// <param name="value">The value to set.</param>
            public void SetArgument(int inputIndex, object value)
            {
                int i = this.methodDefinition.GetInputArgumentIndex(inputIndex);
                if (i == -1)
                {
                    throw new ArgumentOutOfRangeException("DynamicExecute closure input index out of bounds: " + inputIndex.ToString(CultureInfo.CurrentCulture));
                }

                this.arguments[i] = value;
            }

            /// <summary>
            /// Sets an input argument to a value.
            /// </summary>
            /// <param name="inputName">The name of the input parameter to set.</param>
            /// <param name="value">The value to set.</param>
            public void SetArgument(string inputName, object value)
            {
                int i = this.methodDefinition.GetInputArgumentIndex(inputName);
                if (i == -1)
                {
                    throw new KeyNotFoundException("DynamicExecute closure input name not recognized: " + inputName);
                }

                this.arguments[i] = value;
            }

            /// <summary>
            /// Gets an output argument value. Returns null if the output argument does not exist.
            /// </summary>
            /// <param name="outputIndex">The zero-based index of the output argument to retrieve.</param>
            /// <returns>The value of the output argument, or null if the index is out of bounds.</returns>
            public object TryGetOutput(int outputIndex)
            {
                int i = this.methodDefinition.GetOutputArgumentIndex(outputIndex);
                if (i == -1)
                {
                    return null;
                }

                return this.arguments[i];
            }

            /// <summary>
            /// Gets an output argument value.
            /// </summary>
            /// <param name="outputName">The name of the output argument to retrieve.</param>
            /// <returns>The value of the output argument.</returns>
            public object GetOutput(string outputName)
            {
                int i = this.methodDefinition.GetOutputArgumentIndex(outputName);
                if (i == -1)
                {
                    throw new KeyNotFoundException("DynamicExecute closure output name not recognized: " + outputName);
                }

                return this.arguments[i];
            }

            /// <summary>
            /// Gets an input parameter's CLI type.
            /// </summary>
            /// <param name="inputIndex">The zero-based index of the input parameter to retrieve.</param>
            /// <returns>The type of the input parameter.</returns>
            public Type GetInputParameterType(int inputIndex)
            {
                int i = this.methodDefinition.GetInputArgumentIndex(inputIndex);
                if (i == -1)
                {
                    throw new ArgumentOutOfRangeException("DynamicExecute closure input index out of bounds: " + inputIndex.ToString(CultureInfo.CurrentCulture));
                }

                return this.methodDefinition.CompiledMethod.GetParameters()[i].ParameterType;
            }

            /// <summary>
            /// Gets an input parameter's CLI type.
            /// </summary>
            /// <param name="inputName">The name of the input parameter to retrieve.</param>
            /// <returns>The type of the input parameter.</returns>
            public Type GetInputParameterType(string inputName)
            {
                int i = this.methodDefinition.GetInputArgumentIndex(inputName);
                if (i == -1)
                {
                    throw new KeyNotFoundException("DynamicExecute closure input name not recognized: " + inputName);
                }

                return this.methodDefinition.CompiledMethod.GetParameters()[i].ParameterType;
            }

            /// <summary>
            /// Executes the underlying compiled method, with the currently-defined arguments.
            /// </summary>
            public void Run()
            {
                this.methodDefinition.CompiledMethod.Invoke(null, this.arguments);
            }
        }

        private class NameAndType
        {
            public string Name { get; set; }

            public string Type { get; set; }
        }
    }
}