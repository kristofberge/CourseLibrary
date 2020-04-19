using AutoMapper;
using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Repos;
using CourseLibrary.API.Services;
using Marvin.Cache.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;

namespace CourseLibrary.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            _ = services
                .AddHttpCacheHeaders(expirationModelOptions =>
                {
                    expirationModelOptions.MaxAge = 60;
                    expirationModelOptions.CacheLocation = CacheLocation.Private;
                },
                validationModelOptions =>
                {
                    validationModelOptions.MustRevalidate = true;
                })
                .AddResponseCaching()
                .AddControllers(setupAction =>
                {
                    setupAction.ReturnHttpNotAcceptable = true;
                    setupAction.CacheProfiles.Add("240SecondsCacheProfile", new CacheProfile { Duration = 240 });
                })
                .AddNewtonsoftJson(setupAction => setupAction.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver())
                .AddXmlDataContractSerializerFormatters()
                .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = HandleInvalidModelState);

            _ = services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            _ = services.Configure<MvcOptions>(config =>
            {
                var newtonsoftJsonOutputFormatter = config.OutputFormatters.OfType<NewtonsoftJsonOutputFormatter>()?.FirstOrDefault();
                if (newtonsoftJsonOutputFormatter != null)
                {
                    newtonsoftJsonOutputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.hateoas+json");
                }
            });

            _ = services
                .AddScoped<IPropertyMappingService, PropertyMappingService>()
                .AddTransient<ICourseLibraryRepository, CourseLibraryRepository>()
                .AddTransient<IPropertyCheckerService, PropertyCheckerService>()
                .AddDbContext<CourseLibraryContext>(options => options.UseSqlServer(@"Server=localhost;Database=CourseLibraryDb;User=sa;Password=Xamarin1;"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                _ = app.UseDeveloperExceptionPage();
            }
            else
            {
                _ = app.UseExceptionHandler(builder =>
                {
                    builder.Run(async (context) =>
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unepected fault happened. Try again later.");
                    });
                });
            }

            _ = app
                .UseResponseCaching()
                .UseHttpCacheHeaders()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints => endpoints.MapControllers());
        }

        private IActionResult HandleInvalidModelState(ActionContext context)
        {
            var problemsDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problemDetails = problemsDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);

            // First we set some general properties
            problemDetails.Detail = "See the errors field for details";
            problemDetails.Instance = context.HttpContext.Request.Path;

            // Then we check if there are any validation errors
            if (context.ModelState.ErrorCount > 0 &&
                (context as ActionExecutingContext)?.ActionArguments.Count == context.ActionDescriptor.Parameters.Count)
            {
                // If there are, we return 422 unprocessable entity.
                problemDetails.Type = "https://courselibrary.com/modelvalidationproblem";
                problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                problemDetails.Title = "One or more validation errors occurred.";

                return new UnprocessableEntityObjectResult(problemDetails) { ContentTypes = { "application/problem+json" } };
            }

            // If not, we return bad request
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Title = "One or more errors on input occurred.";
            return new BadRequestObjectResult(problemDetails) { ContentTypes = { "application/problem+json" } };
        }
    }
}
