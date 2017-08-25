using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace mAdcOW.AzureFunction.SwaggerDefinition
{
    public static class Templates
    {
        [FunctionName("TemplatePost1")]
        [ResponseType(typeof(string))]
        [Display(Name = "Test Post", Description = "This is a longer description")]
        public static async Task<HttpResponseMessage> Post1([HttpTrigger(AuthorizationLevel.Function, "post")]SampleClass myinput, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<string>(myinput.AString, new JsonMediaTypeFormatter())
            });
        }

        [FunctionName("TemplateGet1")]
        [ResponseType(typeof(bool))]
        public static async Task<HttpResponseMessage> Get1([HttpTrigger(AuthorizationLevel.Function, "get", Route = "TemplateGet1/name/{name}/id/{id}")]string name, int id, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<bool>(true, new JsonMediaTypeFormatter())
            });
        }

        [FunctionName("TemplateGet2")]
        public static async Task<string> Get2([HttpTrigger(AuthorizationLevel.Function, "get", Route = "TemplateGet2/name/{name}/id/{id}")]HttpRequestMessage req, string name, int id, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            return "Hello";
        }

        [FunctionName("TemplateGet3")]
        public static void Get3([HttpTrigger(AuthorizationLevel.Function, "get", Route = "TemplateGet3")][FromUri]string name, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
        }

        [FunctionName("TemplateGet4")]
        public static void Get4([HttpTrigger(AuthorizationLevel.Function, "get", Route = "TemplateGet4")][FromUri]SampleClass input, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
        }

        public class SampleClass
        {
            public string AString { get; set; }
            [Required]
            public string Required { get; set; }
            public bool Public { get; set; }
        }
    }
}
