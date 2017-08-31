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
        /// <summary>
        /// When returning a HttpResponseMessage, annotate the class with ResponseType
        /// </summary>
        [FunctionName("TemplatePost1")]
        [ResponseType(typeof(BodyClass))]
        [Display(Name = "Test Post", Description = "This is a longer description")]
        public static async Task<HttpResponseMessage> Post1(
            [HttpTrigger(AuthorizationLevel.Function, "post")] BodyClass myinput, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<BodyClass>(myinput, new JsonMediaTypeFormatter())
            });
        }

        /// <summary>
        /// Return type specified directly
        /// Use the Display attribute to annotate a function title and description to show in for example Microsoft Flow
        /// </summary>
        [FunctionName("TemplatePost2")]
        [Display(Name = "Test Post", Description = "This is a longer description")]
        public static async Task<BodyClass> Post2([HttpTrigger(AuthorizationLevel.Function, "post")] BodyClass myinput,
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            return myinput;
        }

        /// <summary>
        /// Wrap single values in a class, if not they can be hard to type for resulting API's 
        /// This one uses a generic helper wrapper.
        /// 
        /// It's best practice to return a class as a result to provide context and parameter naming 
        /// </summary>
        [FunctionName("TemplateGet1")]
        [ResponseType(typeof(Result<bool>))]
        public static async Task<HttpResponseMessage> Get1(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "TemplateGet1/name/{name}/id/{id}")] string name,
            int id, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<Result<bool>>(new Result<bool>(false), new JsonMediaTypeFormatter())
            });
        }

        /// <summary>
        /// If returning the result explicitly, there is no need to annotate with ResponseType
        /// </summary>
        [FunctionName("TemplateGet2")]
        public static async Task<Result<string>> Get2(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "TemplateGet2/name/{name}/id/{id}")]
            HttpRequestMessage req, string name, int id, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            return new Result<string>("Hello");
        }

        /// <summary>
        /// Pass input as query parametere on a GET request
        /// </summary>
        /// <example>
        /// GET /api/TemplateGet3?name=foo
        /// </example>
        [FunctionName("TemplateGet3")]
        public static void Get3(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "TemplateGet3")] [FromUri] string name,
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
        }

        /// <summary>
        /// Pass a complex type as query parameteres by their property names
        /// Nested complex types is not supported when using query parameteres. Instead use POST.
        /// </summary>
        /// <example>
        /// GET /api/TemplateGet3?AString=foo&Required=required&public=true
        /// </example>
        [FunctionName("TemplateGet4")]
        public static void Get4(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "TemplateGet4")] [FromUri] UriClass input,
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
        }

        /// <summary>
        /// Nested complex types not supported as URI params in Azure functions
        /// </summary>
        public class UriClass
        {
            public string AString { get; set; }

            [Required]
            public string Required { get; set; }

            public bool Public { get; set; }
        }

        public class BodyClass
        {
            [Display(Description = "This is a string parameter")]
            public string AString { get; set; }

            [Required]
            public string Required { get; set; }

            public bool Public { get; set; }
            public Color Color { get; set; }
        }

        public class Color
        {
            public string RGB { get; set; }
        }

        /// <summary>
        /// Generic helper wrapper class
        /// </summary>
        public class Result<T>
        {
            public Result(T value)
            {
                Value = value;
            }

            public T Value { get; set; }
        }
    }
}