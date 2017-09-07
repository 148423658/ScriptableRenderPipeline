using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;
using System.Text.RegularExpressions;

namespace UnityEditor.VFX
{
    static class VFXCodeGenerator
    {
        public enum CompilationMode
        {
            Debug,
            Runtime
        }

        //This function insure to keep padding while replacing a specific string
        private static void ReplaceMultiline(StringBuilder target, string targetQuery, StringBuilder value)
        {
            string[] delim = { System.Environment.NewLine, "\n" };
            var valueLines = value.ToString().Split(delim, System.StringSplitOptions.None);
            if (valueLines.Length <= 1)
            {
                target.Replace(targetQuery, value.ToString());
            }
            else
            {
                while (true)
                {
                    var targetCopy = target.ToString();
                    var index = targetCopy.IndexOf(targetQuery);
                    if (index == -1)
                    {
                        break;
                    }

                    var padding = "";
                    index--;
                    while (index > 0 && (targetCopy[index] == ' ' || targetCopy[index] == '\t'))
                    {
                        padding = targetCopy[index] + padding;
                        index--;
                    }

                    var currentValue = new StringBuilder();
                    foreach (var line in valueLines)
                    {
                        currentValue.AppendLine(padding + line);
                    }
                    target.Replace(padding + targetQuery, currentValue.ToString());
                }
            }
        }

        static private VFXShaderWriter GenerateLoadAttribute(string matching, VFXContext context)
        {
            var r = new VFXShaderWriter();

            var regex = new Regex(matching);
            var attributesFromContext = context.GetData().GetAttributes().Where(o => regex.IsMatch(o.attrib.name)).ToArray();
            var attributesSource = attributesFromContext.Where(o => o.attrib.location == VFXAttributeLocation.Source).ToArray();
            var attributesCurrent = attributesFromContext.Where(o => o.attrib.location == VFXAttributeLocation.Current).ToArray();

            //< Current Attribute
            foreach (var attribute in attributesCurrent.Select(o => o.attrib))
            {
                var name = attribute.name;
                if (context.GetData().IsAttributeStored(attribute) && context.GetData().IsAttributeRead(attribute, context) && context.contextType != VFXContextType.kInit)
                {
                    r.WriteVariable(attribute.type, name, context.GetData().GetLoadAttributeCode(attribute));
                }
                else
                {
                    r.WriteVariable(attribute.type, name, attribute.value.GetCodeString(null));
                }
                r.WriteLine();
            }

            //< Source Attribute
            foreach (var attribute in attributesSource.Select(o => o.attrib))
            {
                var name = string.Format("{0}_source", attribute.name);
                if (attributesCurrent.Any(o => o.attrib.name == attribute.name))
                {
                    var reference = new VFXAttributeExpression(new VFXAttribute(attribute.name, attribute.value, VFXAttributeLocation.Current));
                    r.WriteVariable(reference.ValueType, name, reference.GetCodeString(null));
                }
                else
                {
                    r.WriteVariable(attribute.type, name, attribute.value.GetCodeString(null));
                }
                r.WriteLine();
            }
            return r;
        }

        static private StringBuilder GenerateStoreAttribute(string matching, VFXContext context)
        {
            var r = new StringBuilder();
            var regex = new Regex(matching);

            var attributesFromContext = context.GetData().GetAttributes().Where(o => regex.IsMatch(o.attrib.name) &&
                    context.GetData().IsAttributeStored(o.attrib) &&
                    context.GetData().IsAttributeWritten(o.attrib, context)).ToArray();

            foreach (var attribute in attributesFromContext.Select(o => o.attrib))
            {
                r.Append(context.GetData().GetStoreAttributeCode(attribute, new VFXAttributeExpression(attribute).GetCodeString(null)));
                r.AppendLine(";");
            }
            return r;
        }

        static private VFXShaderWriter GenerateLoadParameter(string matching, VFXNamedExpression[] namedExpressions, Dictionary<VFXExpression, string> expressionToName)
        {
            var r = new VFXShaderWriter();
            var regex = new Regex(matching);
            var expressionToNameLocal = new Dictionary<VFXExpression, string>(expressionToName);

            var filteredNamedExpressions = namedExpressions.Where(o => regex.IsMatch(o.name)).ToArray();
            foreach (var namedExpression in filteredNamedExpressions)
            {
                r.WriteVariable(namedExpression.exp.ValueType, namedExpression.name, "0");
                r.WriteLine();
            }

            r.EnterScope();
            foreach (var namedExpression in filteredNamedExpressions)
            {
                if (!expressionToNameLocal.ContainsKey(namedExpression.exp))
                {
                    r.WriteVariable(namedExpression.exp, expressionToNameLocal);
                    r.WriteLine();
                }
                r.WriteAssignement(namedExpression.exp.ValueType, namedExpression.name, expressionToNameLocal[namedExpression.exp]);
                r.WriteLine();
            }
            r.ExitScope();
            return r;
        }

        static public void Build(VFXContext context, CompilationMode[] modes, StringBuilder[] stringBuilders, VFXExpressionMapper gpuMapper, string templateName)
        {
            var fallbackTemplate = string.Format("Assets/VFXShaders/{0}.template", templateName);
            var processedFile = new Dictionary<string, StringBuilder>();
            for (int i = 0; i < modes.Length; ++i)
            {
                var mode = modes[i];
                var currentTemplate = string.Format("Assets/VFXShaders/{0}_{1}.template", templateName, mode.ToString().ToLower());
                if (!System.IO.File.Exists(currentTemplate))
                {
                    currentTemplate = fallbackTemplate;
                }

                if (processedFile.ContainsKey(currentTemplate))
                {
                    stringBuilders[i] = new StringBuilder(processedFile[currentTemplate].ToString());
                }
                else
                {
                    Build(context, currentTemplate, stringBuilders[i], gpuMapper);
                    processedFile.Add(currentTemplate, stringBuilders[i]);
                }
            }
        }

        static private void Build(VFXContext context, string templatePath, StringBuilder stringBuilder, VFXExpressionMapper gpuMapper)
        {
            var dependencies = new HashSet<Object>();
            context.CollectDependencies(dependencies);

            var templateContent = new StringBuilder(System.IO.File.ReadAllText(templatePath));

            var uniformMapper = new VFXUniformMapper(gpuMapper);
            var globalDeclaration = new VFXShaderWriter();
            globalDeclaration.WriteCBuffer(uniformMapper, "parameters");
            globalDeclaration.WriteTexture(uniformMapper);

            //< Block processor
            var blockFunction = new VFXShaderWriter();
            var blockCallFunction = new VFXShaderWriter();
            var blockDeclared = new HashSet<string>();
            var expressionToName = context.GetData().GetAttributes().ToDictionary(o => new VFXAttributeExpression(o.attrib) as VFXExpression, o => (new VFXAttributeExpression(o.attrib)).GetCodeString(null));
            expressionToName = expressionToName.Union(uniformMapper.expressionToName).ToDictionary(s => s.Key, s => s.Value);

            foreach (var current in context.childrenWithImplicit.Select((v, i) => new { block = v, blockIndex = i }))
            {
                var block = current.block;
                var blockIndex = current.blockIndex;

                var expressionParameter = new List<VFXExpression>();
                var nameParameter = new List<string>();
                var modeParameter = new List<VFXAttributeMode>();
                foreach (var attribute in block.attributes)
                {
                    expressionParameter.Add(new VFXAttributeExpression(attribute.attrib));
                    nameParameter.Add(attribute.attrib.name);
                    modeParameter.Add(attribute.mode);
                }

                foreach (var parameter in block.parameters)
                {
                    var expReduced = gpuMapper.FromNameAndId(parameter.name, blockIndex);
                    if (!expReduced.Is(VFXExpression.Flags.InvalidOnGPU))
                    {
                        expressionParameter.Add(expReduced);
                        nameParameter.Add(parameter.name);
                        modeParameter.Add(VFXAttributeMode.None);
                    }
                }

                var methodName = block.functionName;
                if (!blockDeclared.Contains(methodName))
                {
                    blockDeclared.Add(methodName);
                    blockFunction.WriteBlockFunction(gpuMapper, methodName, block.source, expressionParameter, nameParameter, modeParameter);
                }

                //< Parameters (computed and/or extracted from uniform)
                var expressionToNameLocal = expressionToName;
                bool needScope = expressionParameter.Any(e => !expressionToNameLocal.ContainsKey(e));
                if (needScope)
                {
                    expressionToNameLocal = new Dictionary<VFXExpression, string>(expressionToNameLocal);
                    blockCallFunction.EnterScope();
                    foreach (var exp in expressionParameter)
                    {
                        if (expressionToNameLocal.ContainsKey(exp))
                        {
                            continue;
                        }
                        blockCallFunction.WriteVariable(exp, expressionToNameLocal);
                    }
                }

                blockCallFunction.WriteCallFunction(methodName, expressionParameter, nameParameter, modeParameter, gpuMapper, expressionToNameLocal);

                if (needScope)
                    blockCallFunction.ExitScope();
            }

            //< Final composition
            var globalIncludeContent = new VFXShaderWriter();
            globalIncludeContent.WriteLine("#include \"HLSLSupport.cginc\"");
            globalIncludeContent.WriteLine("#define NB_THREADS_PER_GROUP 256");
            foreach (var attribute in context.GetData().GetAttributes().Select(o => o.attrib))
                globalIncludeContent.WriteLineFormat("#define VFX_USE_{0}_{1} 1", attribute.name.ToUpper(), attribute.location == VFXAttributeLocation.Current ? "CURRENT" : "SOURCE");
            foreach (var additionnalDefine in context.additionalDefines)
                globalIncludeContent.WriteLineFormat("#define {0} 1", additionnalDefine);

            globalIncludeContent.WriteLine();
            globalIncludeContent.WriteLine("#include \"Assets/VFXShaders/VFXCommon.cginc\"");

            stringBuilder.Append(templateContent);

            ReplaceMultiline(stringBuilder, "${VFXGlobalInclude}", globalIncludeContent.builder);
            ReplaceMultiline(stringBuilder, "${VFXGlobalDeclaration}", globalDeclaration.builder);
            ReplaceMultiline(stringBuilder, "${VFXGeneratedBlockFunction}", blockFunction.builder);
            ReplaceMultiline(stringBuilder, "${VFXProcessBlocks}", blockCallFunction.builder);

            //< Load Parameter
            var loadParameterRegex = new Regex("\\${VFXLoadParameter:{(.*?)}}");
            var mainParameters = gpuMapper.CollectExpression(-1).ToArray();
            while (loadParameterRegex.IsMatch(stringBuilder.ToString()))
            {
                var current = loadParameterRegex.Match(stringBuilder.ToString());
                var match = current.Groups[0].Value;
                var pattern = current.Groups[1].Value;
                var loadParameters = GenerateLoadParameter(pattern, mainParameters, expressionToName);
                ReplaceMultiline(stringBuilder, match, loadParameters.builder);
            }

            //< Load Attribute
            if (stringBuilder.ToString().Contains("${VFXLoadAttributes}"))
            {
                var loadAttribute = GenerateLoadAttribute(".*", context);
                ReplaceMultiline(stringBuilder, "${VFXLoadAttributes}", loadAttribute.builder);
            }

            var loadAttributeRegex = new Regex("\\${VFXLoadAttributes:{(.*?)}}");
            while (loadAttributeRegex.IsMatch(stringBuilder.ToString()))
            {
                var current = loadAttributeRegex.Match(stringBuilder.ToString());
                var match = current.Groups[0].Value;
                var pattern = current.Groups[1].Value;
                var loadAttribute = GenerateLoadAttribute(pattern, context);
                ReplaceMultiline(stringBuilder, match, loadAttribute.builder);
            }

            //< Store Attribute
            if (stringBuilder.ToString().Contains("${VFXStoreAttributes}"))
            {
                var storeAttribute = GenerateStoreAttribute(".*", context);
                ReplaceMultiline(stringBuilder, "${VFXStoreAttributes}", storeAttribute);
            }

            var storeAttributeRegex = new Regex("\\${VFXStoreAttributes:{(.*?)}}");
            while (storeAttributeRegex.IsMatch(stringBuilder.ToString()))
            {
                var current = storeAttributeRegex.Match(stringBuilder.ToString());
                var match = current.Groups[0].Value;
                var pattern = current.Groups[1].Value;
                var storeAttribute = GenerateStoreAttribute(pattern, context);
                ReplaceMultiline(stringBuilder, match, storeAttribute);
            }

            foreach (var addionnalReplacement in context.additionnalReplacements)
            {
                ReplaceMultiline(stringBuilder, addionnalReplacement.Key, addionnalReplacement.Value.builder);
            }

            Debug.LogFormat("GENERATED_OUTPUT_FILE_FOR : {0}\n{1}", context.ToString(), stringBuilder.ToString());
        }
    }
}
