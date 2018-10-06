#region Test for .NET vs Standard/Core
#if (NET2 || NET35 || NET40 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472)
#define SWAGGER_NETCLASSIC
#endif
// TODO: Add to the list of .NET versions above if Microsoft releases > 4.7.2
#endregion

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Globalization;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Web.Http;
#if SWAGGER_NETCLASSIC
using System.Web.Http.Description;
#else
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
#endif
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureFunctionSwaggerDefinition
{
    public static class Swagger
    {
        const string SwaggerFunctionName = "Swagger";

        [FunctionName("Swagger")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            var assembly = Assembly.GetExecutingAssembly();

            dynamic doc = new ExpandoObject();
            doc.swagger = "2.0";
            doc.info = new ExpandoObject();
            doc.info.title = assembly.GetName().Name;
            doc.info.version = "1.0.0";
            doc.host = req.RequestUri.Authority;
            doc.basePath = "/";
            doc.schemes = new[] { "https" };
            if (doc.host.Contains("127.0.0.1") || doc.host.Contains("localhost"))
            {
                doc.schemes = new[] { "http" };
            }
            doc.definitions = new ExpandoObject();
            doc.paths = GeneratePaths(assembly, doc);
            doc.securityDefinitions = GenerateSecurityDefinitions();

            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<object>(doc, new JsonMediaTypeFormatter()),

            });
        }

        private static dynamic GenerateSecurityDefinitions()
        {
            dynamic securityDefinitions = new ExpandoObject();
            securityDefinitions.apikeyQuery = new ExpandoObject();
            securityDefinitions.apikeyQuery.type = "apiKey";
            securityDefinitions.apikeyQuery.name = "code";
            securityDefinitions.apikeyQuery.@in = "query";

            // Microsoft Flow import doesn't like two apiKey options, so we leave one out.

            //securityDefinitions.apikeyHeader = new ExpandoObject();
            //securityDefinitions.apikeyHeader.type = "apiKey";
            //securityDefinitions.apikeyHeader.name = "x-functions-key";
            //securityDefinitions.apikeyHeader.@in = "header";
            return securityDefinitions;
        }

        private static dynamic GeneratePaths(Assembly assembly, dynamic doc)
        {
            dynamic paths = new ExpandoObject();
            var methods = assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(FunctionNameAttribute), false).Length > 0)
                .ToArray();
            foreach (MethodInfo methodInfo in methods)
            {
                string route = "/api/";

                var functionAttr = (FunctionNameAttribute)methodInfo.GetCustomAttributes(typeof(FunctionNameAttribute), false)
                    .Single();

                if (functionAttr.Name == SwaggerFunctionName) continue;

                HttpTriggerAttribute triggerAttribute = null;
                foreach (ParameterInfo parameter in methodInfo.GetParameters())
                {
                    triggerAttribute = parameter.GetCustomAttributes(typeof(HttpTriggerAttribute), false)
                        .FirstOrDefault() as HttpTriggerAttribute;
                    if (triggerAttribute != null) break;
                }
                if (triggerAttribute == null) continue; // Trigger attribute is required in an Azure function

                if (!string.IsNullOrWhiteSpace(triggerAttribute.Route))
                {
                    route += triggerAttribute.Route;
                }
                else
                {
                    route += functionAttr.Name;
                }

                dynamic path = new ExpandoObject();

                var verbs = triggerAttribute.Methods ?? new[] { "get", "post", "delete", "head", "patch", "put", "options" };
                foreach (string verb in verbs)
                {
                    dynamic operation = new ExpandoObject();
                    operation.operationId = ToTitleCase(functionAttr.Name) + ToTitleCase(verb);
                    operation.produces = new[] { "application/json" };
                    operation.consumes = new[] { "application/json" };
                    operation.parameters = GenerateFunctionParametersSignature(methodInfo, route, doc);

                    // Summary is title
                    operation.summary = GetFunctionName(methodInfo, functionAttr.Name);
                    // Verbose description
                    operation.description = GetFunctionDescription(methodInfo, functionAttr.Name);

                    operation.responses = GenerateResponseParameterSignature(methodInfo, doc);
                    dynamic keyQuery = new ExpandoObject();
                    keyQuery.apikeyQuery = new string[0];
                    operation.security = new ExpandoObject[] { keyQuery };

                    // Microsoft Flow import doesn't like two apiKey options, so we leave one out.
                    //dynamic apikeyHeader = new ExpandoObject();
                    //apikeyHeader.apikeyHeader = new string[0];
                    //operation.security = new ExpandoObject[] { keyQuery, apikeyHeader };

                    AddToExpando(path, verb, operation);
                }
                AddToExpando(paths, route, path);
            }
            return paths;
        }

        private static string GetFunctionDescription(MethodInfo methodInfo, string funcName)
        {
            var displayAttr = (DisplayAttribute)methodInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
                .SingleOrDefault();
            return !string.IsNullOrWhiteSpace(displayAttr?.Description) ? displayAttr.Description : $"This function will run {funcName}";
        }

        /// <summary>
        /// Max 80 characters in summary/title
        /// </summary>
        private static string GetFunctionName(MethodInfo methodInfo, string funcName)
        {
            var displayAttr = (DisplayAttribute)methodInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
                .SingleOrDefault();
            if (!string.IsNullOrWhiteSpace(displayAttr?.Name))
            {
                return displayAttr.Name.Length > 80 ? displayAttr.Name.Substring(0, 80) : displayAttr.Name;
            }
            return $"Run {funcName}";
        }

        private static string GetPropertyDescription(PropertyInfo propertyInfo)
        {
            var displayAttr = (DisplayAttribute)propertyInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
                .SingleOrDefault();
            return !string.IsNullOrWhiteSpace(displayAttr?.Description) ? displayAttr.Description : $"This returns {propertyInfo.PropertyType.Name}";
        }

        private static dynamic GenerateResponseParameterSignature(MethodInfo methodInfo, dynamic doc)
        {
            dynamic responses = new ExpandoObject();

#if SWAGGER_NETCLASSIC
            var responseTypeAttrs = (IEnumerable<ResponseTypeAttribute>)methodInfo.GetCustomAttributes(typeof(ResponseTypeAttribute), false);
            if (responseTypeAttrs.Count() == 0)
                responseTypeAttrs = responseTypeAttrs.Concat(new[] { (ResponseTypeAttribute)null });
#else
            var responseTypeAttrs = (IEnumerable<ProducesResponseTypeAttribute>)methodInfo.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), false);
            if (responseTypeAttrs.Count() == 0)
                responseTypeAttrs = responseTypeAttrs.Concat(new[] { (ProducesResponseTypeAttribute)null });
#endif


            foreach (var responseTypeAttr in responseTypeAttrs)
            {
                dynamic responseDef = new ExpandoObject();
                responseDef.description = "OK";
                int responseCode = 200;
                var returnType = methodInfo.ReturnType;

                if (responseTypeAttr != null)
                {
#if SWAGGER_NETCLASSIC
                    responseCode = 200;
#else
                    responseCode = responseTypeAttr.StatusCode;
                    returnType = responseTypeAttr.Type;
#endif
                }
                if (returnType.IsGenericType)
                {
                    var genericReturnType = returnType.GetGenericArguments().FirstOrDefault();
                    if (genericReturnType != null)
                    {
                        returnType = genericReturnType;
                    }
                }
                if (returnType == typeof(HttpResponseMessage))
                {
                    if (responseTypeAttr == null)
                    {
                        returnType = typeof(void);
                    }
                    else
                    {
#if SWAGGER_NETCLASSIC
                        returnType = responseTypeAttr.ResponseType;
#else
                        returnType = responseTypeAttr.Type;
#endif
                    }
                }
                if (returnType != typeof(void))
                {
                    responseDef.schema = new ExpandoObject();

                    if (returnType.Namespace == "System")
                    {
                        // Warning:
                        // Allthough valid, it's always better to wrap single values in an object
                        // Returning { Value = "foo" } is better than just "foo"
                        SetParameterType(returnType, responseDef.schema, null);
                    }
                    else
                    {
                        string name = returnType.Name;
                        if (returnType.IsGenericType)
                        {
                            var realType = returnType.GetGenericArguments()[0];
                            if (realType.Namespace == "System")
                            {
                                dynamic inlineSchema = GetObjectSchemaDefinition(null, returnType);
                                responseDef.schema = inlineSchema;
                            }
                            else
                            {
                                AddDefinition(doc, responseDef.schema, returnType, name);
                            }
                        }
                        else if (returnType.IsArray)
                        {
                            AddToExpando(responseDef.schema, "type", "array");
                            responseDef.schema.items = new ExpandoObject();
                            AddDefinition(doc, responseDef.schema.items, returnType, returnType.GetElementType().Name);
                        }
                        else
                        {
                            AddDefinition(doc, responseDef.schema, returnType, name);
                        }
                    }
                }
                AddToExpando(responses, $"{responseCode}", responseDef);
            }

            return responses;
        }

        private static void AddDefinition(dynamic doc, dynamic responseDefNode, Type returnType, string name)
        {
            AddToExpando(responseDefNode, "$ref", "#/definitions/" + name);
            AddParameterDefinition((IDictionary<string, object>)doc.definitions, returnType);
        }

        private static List<object> GenerateFunctionParametersSignature(MethodInfo methodInfo, string route, dynamic doc)
        {
            var parameterSignatures = new List<object>();
            foreach (ParameterInfo parameter in methodInfo.GetParameters())
            {
                if (parameter.ParameterType == typeof(HttpRequestMessage)) continue;
#if !SWAGGER_NETCLASSIC
                if (parameter.ParameterType == typeof(HttpRequest)) continue;
#endif
                if (parameter.ParameterType == typeof(ExecutionContext)) continue;
                if (parameter.ParameterType == typeof(TraceWriter)) continue;
                if (parameter.ParameterType == typeof(Microsoft.Extensions.Logging.ILogger)) continue;
                if (parameter.ParameterType == typeof(CloudTable)) continue;

                bool hasUriAttribute = parameter.GetCustomAttributes().Any(attr => attr is FromUriAttribute);


                if (route.Contains('{' + parameter.Name))
                {
                    dynamic opParam = new ExpandoObject();
                    opParam.name = parameter.Name;
                    opParam.@in = "path";
                    opParam.required = true;
                    SetParameterType(parameter.ParameterType, opParam, null);
                    parameterSignatures.Add(opParam);
                }
                else if (hasUriAttribute && parameter.ParameterType.Namespace == "System")
                {
                    dynamic opParam = new ExpandoObject();
                    opParam.name = parameter.Name;
                    opParam.@in = "query";
                    opParam.required = parameter.GetCustomAttributes().Any(attr => attr is RequiredAttribute);
                    SetParameterType(parameter.ParameterType, opParam, doc.definitions);
                    parameterSignatures.Add(opParam);
                }
                else if (hasUriAttribute && parameter.ParameterType.Namespace != "System")
                {
                    AddObjectProperties(parameter.ParameterType, "", parameterSignatures, doc);
                }
                else
                {
                    dynamic opParam = new ExpandoObject();
                    opParam.name = parameter.Name;
                    opParam.@in = "body";
                    opParam.required = true;
                    opParam.schema = new ExpandoObject();
                    if (parameter.ParameterType.Namespace == "System")
                    {
                        SetParameterType(parameter.ParameterType, opParam.schema, null);
                    }
                    else
                    {
                        AddToExpando(opParam.schema, "$ref", "#/definitions/" + parameter.ParameterType.Name);
                        AddParameterDefinition((IDictionary<string, object>)doc.definitions, parameter.ParameterType);
                    }
                    parameterSignatures.Add(opParam);
                }
            }
            return parameterSignatures;
        }

        private static void AddObjectProperties(Type t, string parentName, List<object> parameterSignatures, dynamic doc)
        {
            var publicProperties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in publicProperties)
            {
                if (!string.IsNullOrWhiteSpace(parentName))
                {
                    parentName += ".";
                }
                if (property.PropertyType.Namespace != "System")
                {
                    AddObjectProperties(property.PropertyType, parentName + property.Name, parameterSignatures, doc);
                }
                else
                {
                    dynamic opParam = new ExpandoObject();

                    opParam.name = parentName + property.Name;
                    opParam.@in = "query";
                    opParam.required = property.GetCustomAttributes().Any(attr => attr is RequiredAttribute);
                    opParam.description = GetPropertyDescription(property);
                    SetParameterType(property.PropertyType, opParam, doc.definitions);
                    parameterSignatures.Add(opParam);
                }
            }
        }

        private static void AddParameterDefinition(IDictionary<string, object> definitions, Type parameterType)
        {
            if (!definitions.TryGetValue(parameterType.Name, out dynamic objDef))
            {
                objDef = GetObjectSchemaDefinition(definitions, parameterType);
                definitions.Add(parameterType.Name, objDef);
            }
        }

        private static dynamic GetObjectSchemaDefinition(IDictionary<string, object> definitions, Type parameterType)
        {
            dynamic objDef = new ExpandoObject();
            objDef.type = "object";
            objDef.properties = new ExpandoObject();
            var publicProperties = parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            List<string> requiredProperties = new List<string>();
            foreach (PropertyInfo property in publicProperties)
            {
                if (property.GetCustomAttributes().Any(attr => attr is RequiredAttribute))
                {
                    requiredProperties.Add(property.Name);
                }
                dynamic propDef = new ExpandoObject();
                propDef.description = GetPropertyDescription(property);
                SetParameterType(property.PropertyType, propDef, definitions);
                AddToExpando(objDef.properties, property.Name, propDef);
            }
            if (requiredProperties.Count > 0)
            {
                objDef.required = requiredProperties;
            }
            return objDef;
        }

        private static void SetParameterType(Type parameterType, dynamic opParam, dynamic definitions)
        {
            var inputType = parameterType;

            var setObject = opParam;
            if (inputType.IsArray)
            {
                opParam.type = "array";
                opParam.items = new ExpandoObject();
                setObject = opParam.items;
                parameterType = parameterType.GetElementType();
            }
            else if (inputType.IsGenericType)
            {
                opParam.type = "array";
                opParam.items = new ExpandoObject();
                setObject = opParam.items;
                parameterType = parameterType.GetGenericArguments()[0];
            }

            if (inputType.Namespace == "System" || (inputType.IsGenericType && inputType.GetGenericArguments()[0].Namespace == "System"))
            {
                switch (Type.GetTypeCode(inputType))
                {
                    case TypeCode.Int32:
                        setObject.format = "int32";
                        setObject.type = "integer";
                        break;
                    case TypeCode.Int64:
                        setObject.format = "int64";
                        setObject.type = "integer";
                        break;
                    case TypeCode.Single:
                        setObject.format = "float";
                        setObject.type = "number";
                        break;
                    case TypeCode.Double:
                        setObject.format = "double";
                        setObject.type = "number";
                        break;
                    case TypeCode.String:
                        setObject.type = "string";
                        break;
                    case TypeCode.Byte:
                        setObject.format = "byte";
                        setObject.type = "string";
                        break;
                    case TypeCode.Boolean:
                        setObject.type = "boolean";
                        break;
                    case TypeCode.DateTime:
                        setObject.format = "date";
                        setObject.type = "string";
                        break;
                    default:
                        setObject.type = "string";
                        break;
                }
            }
            else if (inputType.IsEnum)
            {
                opParam.type = "string";
                opParam.@enum = Enum.GetNames(inputType);
            }
            else if (definitions != null)
            {
                AddToExpando(setObject, "$ref", "#/definitions/" + parameterType.Name);
                AddParameterDefinition((IDictionary<string, object>)definitions, parameterType);
            }
        }

        public static string ToTitleCase(string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
        }

        public static void AddToExpando(ExpandoObject obj, string name, object value)
        {
            if (((IDictionary<string, object>)obj).ContainsKey(name))
            {
                // Fix for functions with same routes but different verbs
                var existing = (IDictionary<string, object>)((IDictionary<string, object>)obj)[name];
                var append = (IDictionary<string, object>)value;
                foreach (KeyValuePair<string, object> keyValuePair in append)
                {
                    existing.Add(keyValuePair);
                }
            }
            else
            {
                ((IDictionary<string, object>)obj).Add(name, value);
            }
        }

    }
}
