using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    static class VFXCodeGeneratorHelper
    {
        public static string GeneratePrefix(uint index)
        {
            var alpha = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
            string prefix = "";
            index = index + 1;
            while (index != 0u)
            {
                prefix = alpha[index % alpha.Length] + prefix;
                index /= (uint)alpha.Length;
            }
            return prefix;
        }
    }

    class VFXShaderWriter
    {
        public static string GetValueString(VFXValueType type, object value)
        {
            var format = "";
            switch (type)
            {
                case VFXValueType.kBool:
                case VFXValueType.kInt:
                case VFXValueType.kUint:
                case VFXValueType.kFloat:
                    format = "({0}){1}";
                    break;
                case VFXValueType.kFloat2:
                case VFXValueType.kFloat3:
                case VFXValueType.kFloat4:
                    format = "{0}{1}";
                    break;
                default: throw new Exception("GetValueString missing type: " + type);
            }
            // special cases of ToString
            switch (type)
            {
                case VFXValueType.kBool:
                    value = value.ToString().ToLower();
                    break;
                case VFXValueType.kFloat2:
                    value = string.Format(CultureInfo.InvariantCulture, "({0},{1})", ((Vector2)value).x, ((Vector2)value).y);
                    break;
                case VFXValueType.kFloat3:
                    value = string.Format(CultureInfo.InvariantCulture, "({0},{1},{2})", ((Vector3)value).x, ((Vector3)value).y, ((Vector3)value).z);
                    break;
                case VFXValueType.kFloat4:
                    value = string.Format(CultureInfo.InvariantCulture, "({0},{1},{2},{3})", ((Vector4)value).x, ((Vector4)value).y, ((Vector4)value).z, ((Vector4)value).w);
                    break;
            }
            return string.Format(CultureInfo.InvariantCulture, format, VFXExpression.TypeToCode(type), value);
        }

        public static string GetMultilineWithPrefix(string str, string linePrefix)
        {
            if (linePrefix.Length == 0)
                return str;

            if (str.Length == 0)
                return linePrefix;

            string[] delim = { System.Environment.NewLine, "\n" };
            var lines = str.Split(delim, System.StringSplitOptions.None);
            var dst = new StringBuilder(linePrefix.Length * lines.Length + str.Length);

            foreach (var line in lines)
            {
                dst.Append(linePrefix);
                dst.AppendLine(line);
            }

            return dst.ToString(0, dst.Length - Environment.NewLine.Length); // Remove the last line terminator
        }

        public void WriteFormat(string str, object arg0)                                { m_Builder.AppendFormat(str, arg0); }
        public void WriteFormat(string str, object arg0, object arg1)                   { m_Builder.AppendFormat(str, arg0, arg1); }
        public void WriteFormat(string str, object arg0, object arg1, object arg2)      { m_Builder.AppendFormat(str, arg0, arg1, arg2); }

        public void WriteLineFormat(string str, object arg0)                            { WriteFormat(str, arg0); WriteLine(); }
        public void WriteLineFormat(string str, object arg0, object arg1)               { WriteFormat(str, arg0, arg1); WriteLine(); }
        public void WriteLineFormat(string str, object arg0, object arg1, object arg2)  { WriteFormat(str, arg0, arg1, arg2); WriteLine(); }

        // Generic builder method
        public void Write<T>(T t)
        {
            m_Builder.Append(t);
        }

        // Optimize version to append substring and avoid useless allocation
        public void Write(String s, int start, int length)
        {
            m_Builder.Append(s, start, length);
        }

        public void WriteLine<T>(T t)
        {
            Write(t);
            WriteLine();
        }

        public void WriteLine()
        {
            m_Builder.AppendLine();
            WriteIndent();
        }

        public void EnterScope()
        {
            WriteLine('{');
            Indent();
        }

        public void ExitScope()
        {
            Deindent();
            WriteLine('}');
        }

        public void ExitScopeStruct()
        {
            Deindent();
            WriteLine("};");
        }

        public void ReplaceMultilineWithIndent(string tag, string src)
        {
            var str = m_Builder.ToString();
            int startIndex = 0;
            while (true)
            {
                int index = str.IndexOf(tag, startIndex);
                if (index == -1)
                    break;

                var lastPrefixIndex = index;
                while (index > 0 && (str[index] == ' ' || str[index] == '\t'))
                    --index;

                var prefix = str.Substring(index, lastPrefixIndex - index);
                var formattedStr = GetMultilineWithPrefix(src, prefix).Substring(prefix.Length);
                m_Builder.Replace(tag, formattedStr, lastPrefixIndex, tag.Length);

                startIndex = index;
            }
        }

        public void WriteMultilineWithIndent<T>(T str)
        {
            if (m_Indent == 0)
                Write(str);
            else
            {
                var indentStr = new StringBuilder(m_Indent * kIndentStr.Length);
                for (int i = 0; i < m_Indent; ++i)
                    indentStr.Append(kIndentStr);
                WriteMultilineWithPrefix(str, indentStr.ToString());
            }
        }

        public void WriteMultilineWithPrefix<T>(T str, string linePrefix)
        {
            if (linePrefix.Length == 0)
                Write(str);
            else
            {
                var res = GetMultilineWithPrefix(str.ToString(), linePrefix);
                WriteLine(res.Substring(linePrefix.Length)); // Remove first line length;
            }
        }

        public override string ToString()
        {
            return m_Builder.ToString();
        }

        private int WritePadding(int alignment, int offset, ref int index)
        {
            int padding = (alignment - (offset % alignment)) % alignment;
            if (padding != 0)
                WriteLineFormat("uint{0} PADDING_{1};", padding == 1 ? "" : padding.ToString(), index++);
            return padding;
        }

        public void WriteTexture(VFXUniformMapper mapper)
        {
            foreach (var texture in mapper.textures)
            {
                WriteLineFormat("{0} {1};", VFXExpression.TypeToCode(texture.valueType), mapper.GetName(texture));
                WriteLineFormat("SamplerState sampler{0};", mapper.GetName(texture));
            }
        }

        public void WriteEventBuffer(string baseName, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                var prefix = VFXCodeGeneratorHelper.GeneratePrefix((uint)i);
                WriteLineFormat("AppendStructuredBuffer<uint> {0}_{1};", baseName, prefix);
            }
        }

        public void WriteCBuffer(VFXUniformMapper mapper, string bufferName)
        {
            var uniformValues = mapper.uniforms
                .Where(e => !e.IsAny(VFXExpression.Flags.Constant | VFXExpression.Flags.InvalidOnCPU)) // Filter out constant expressions
                .OrderByDescending(e => VFXValue.TypeToSize(e.valueType));

            var uniformBlocks = new List<List<VFXExpression>>();
            foreach (var value in uniformValues)
            {
                var block = uniformBlocks.FirstOrDefault(b => b.Sum(e => VFXValue.TypeToSize(e.valueType)) + VFXValue.TypeToSize(value.valueType) <= 4);
                if (block != null)
                    block.Add(value);
                else
                    uniformBlocks.Add(new List<VFXExpression>() { value });
            }

            if (uniformBlocks.Count > 0)
            {
                WriteLineFormat("CBUFFER_START({0})", bufferName);
                Indent();

                int paddingIndex = 0;
                foreach (var block in uniformBlocks)
                {
                    int currentSize = 0;
                    foreach (var value in block)
                    {
                        string type = VFXExpression.TypeToUniformCode(value.valueType);
                        string name = mapper.GetName(value);
                        currentSize += VFXExpression.TypeToSize(value.valueType);

                        WriteLineFormat("{0} {1};", type, name);
                    }

                    WritePadding(4, currentSize, ref paddingIndex);
                }

                Deindent();
                WriteLine("CBUFFER_END");
            }
        }

        private string AggregateParameters(List<string> parameters)
        {
            return parameters.Count == 0 ? "" : parameters.Aggregate((a, b) => a + ", " + b);
        }

        private static string GetFunctionParameterType(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.kTexture2D: return "VFXSampler2D";
                case VFXValueType.kTexture2DArray: return "VFXSampler2DArray";
                case VFXValueType.kTexture3D: return "VFXSampler3D";
                case VFXValueType.kTextureCube: return "VFXSamplerCube";
                case VFXValueType.kTextureCubeArray: return "VFXSamplerCubeArray";

                default:
                    return VFXExpression.TypeToCode(type);
            }
        }

        private static string GetFunctionParameterName(VFXExpression expression, Dictionary<VFXExpression, string> names)
        {
            var expressionName = names[expression];
            switch (expression.valueType)
            {
                case VFXValueType.kTexture2D:
                case VFXValueType.kTexture2DArray:
                case VFXValueType.kTexture3D:
                case VFXValueType.kTextureCube:
                case VFXValueType.kTextureCubeArray: return string.Format("GetVFXSampler({0}, {1})", expressionName, ("sampler" + expressionName));

                default:
                    return expressionName;
            }
        }

        public void WriteBlockFunction(VFXExpressionMapper mapper, string functionName, string source, List<VFXExpression> expressions, List<string> parameterNames, List<VFXAttributeMode> modes, string commentMethod)
        {
            var parameters = new List<string>();
            for (int i = 0; i < parameterNames.Count; ++i)
            {
                var parameter = parameterNames[i];
                var mode = modes[i];
                var expression = expressions[i];
                parameters.Add(string.Format("{0}{1} {2}", (mode & VFXAttributeMode.Write) != 0 ? "inout " : "", GetFunctionParameterType(expression.valueType), parameter));
            }

            WriteFormat("void {0}({1})", functionName, AggregateParameters(parameters));
            if (!string.IsNullOrEmpty(commentMethod))
            {
                WriteFormat(" /*{0}*/", commentMethod);
            }
            WriteLine();
            EnterScope();
            if (source != null)
                WriteMultilineWithIndent(source);
            ExitScope();
        }

        public void WriteCallFunction(string functionName, List<VFXExpression> expressions, List<string> parameterNames, List<VFXAttributeMode> modes, VFXExpressionMapper mapper, Dictionary<VFXExpression, string> variableNames)
        {
            var parameters = new List<string>();
            for (int i = 0; i < parameterNames.Count; ++i)
            {
                var parameter = parameterNames[i];
                var mode = modes[i];
                var expression = expressions[i];
                parameters.Add(string.Format("{1}{0}", GetFunctionParameterName(expression, variableNames), (mode & VFXAttributeMode.Write) != 0 ? "/*inout*/" : ""));
            }

            WriteLineFormat("{0}({1});", functionName, AggregateParameters(parameters));
        }

        public void WriteAssignement(VFXValueType type, string variableName, string value)
        {
            var format = value == "0" ? "{1} = ({0}){2};" : "{1} = {2};";
            WriteFormat(format, VFXExpression.TypeToCode(type), variableName, value);
        }

        public void WriteVariable(VFXValueType type, string variableName, string value)
        {
            if (!VFXExpression.IsTypeValidOnGPU(type))
                throw new ArgumentException(string.Format("Invalid GPU Type: {0}", type));

            WriteFormat("{0} ", VFXExpression.TypeToCode(type));
            WriteAssignement(type, variableName, value);
        }

        public void WriteVariable(VFXExpression exp, Dictionary<VFXExpression, string> variableNames)
        {
            if (!variableNames.ContainsKey(exp))
            {
                string entry;
                if (exp.Is(VFXExpression.Flags.Constant))
                    entry = exp.GetCodeString(null); // Patch constant directly
                else
                {
                    foreach (var parent in exp.parents)
                        WriteVariable(parent, variableNames);

                    // Generate a new variable name
                    entry = "tmp_" + VFXCodeGeneratorHelper.GeneratePrefix((uint)variableNames.Count());
                    string value = exp.GetCodeString(exp.parents.Select(p => variableNames[p]).ToArray());

                    WriteVariable(exp.valueType, entry, value);
                    WriteLine();
                }

                variableNames[exp] = entry;
            }
        }

        public StringBuilder builder { get { return m_Builder; } }

        // Private stuff
        private void Indent()
        {
            ++m_Indent;
            Write(kIndentStr);
        }

        private void Deindent()
        {
            if (m_Indent == 0)
                throw new InvalidOperationException("Cannot de-indent as current indentation is 0");

            --m_Indent;
            m_Builder.Remove(m_Builder.Length - kIndentStr.Length, kIndentStr.Length); // remove last indent
        }

        private void WriteIndent()
        {
            for (int i = 0; i < m_Indent; ++i)
                m_Builder.Append(kIndentStr);
        }

        private StringBuilder m_Builder = new StringBuilder();
        private int m_Indent = 0;
        private const string kIndentStr = "    ";
    }
}
