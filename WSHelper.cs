/**
*
* Dynamic call webservice
*
* @author : Issen.Yu
* @version: 19.5.17.0
* @email  : yusiyuan1208@qq.com
*
*/
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.IO;
using System.Net;
using System.Reflection;
using System.Web.Services.Description;
using System.Xml.Serialization;

namespace wshelper
{
    public class WSHelper
    {
        private static Hashtable _assemblys = new Hashtable();

        public static object Invoke(string url, string className, string methodName, object[] parameters)
        {
            lock (_assemblys.SyncRoot)
            {
                if (!_assemblys.ContainsKey(url))
                {
                    WebClient web = new WebClient();
                    using (Stream stream = web.OpenRead($"{ url }?WSDL"))
                    {
                        CodeNamespace nmspace = new CodeNamespace();
                        CodeCompileUnit unit = new CodeCompileUnit();
                        ServiceDescription description = ServiceDescription.Read(stream);
                        ServiceDescriptionImporter importer = new ServiceDescriptionImporter()
                        {
                            ProtocolName = "Soap",
                            Style = ServiceDescriptionImportStyle.Client,
                            CodeGenerationOptions = CodeGenerationOptions.GenerateProperties | CodeGenerationOptions.GenerateNewAsync
                        };
                        importer.AddServiceDescription(description, null, null);
                        unit.Namespaces.Add(nmspace);
                        ServiceDescriptionImportWarnings warning = importer.Import(nmspace, unit);
                        CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
                        CompilerParameters parameter = new CompilerParameters()
                        {
                            GenerateInMemory = true
                        };
                        parameter.ReferencedAssemblies.Add("System.dll");
                        parameter.ReferencedAssemblies.Add("System.XML.dll");
                        parameter.ReferencedAssemblies.Add("System.Web.Services.dll");
                        parameter.ReferencedAssemblies.Add("System.Data.dll");
                        CompilerResults result = provider.CompileAssemblyFromDom(parameter, unit);
                        if (!result.Errors.HasErrors)
                        {
                            _assemblys[url] = result.CompiledAssembly;
                        }
                    }
                }
            }
            if (!_assemblys.ContainsKey(url))
            {
                throw new DllNotFoundException();
            }
            Type t = ((Assembly)_assemblys[url]).GetType(className);

            return t.GetMethod(methodName).Invoke(Activator.CreateInstance(t), parameters);
        }
    }
}
