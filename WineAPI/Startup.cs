using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json.Serialization;

namespace WineAPI
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
            // use this to apply response cache settings globally
            services.AddHttpCacheHeaders((expirationModelOptions) =>
            {
                expirationModelOptions.MaxAge = 30;
            },
            (validationModelOptions) =>
            {
                validationModelOptions.MustRevalidate = true;
            });

            services.AddControllers(setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true;
            })
                .AddNewtonsoftJson( setupAction =>
                {
                    setupAction.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                })
                .AddXmlDataContractSerializerFormatters()
                .ConfigureApiBehaviorOptions(setupAction =>
                {
                    setupAction.InvalidModelStateResponseFactory = context =>
                    {
                        // create problem details object
                        var problemDetailsFactory = context.HttpContext.RequestServices
                            .GetRequiredService<ProblemDetailsFactory>();
                        var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(
                            context.HttpContext,
                            context.ModelState);

                        // add info not added by default
                        problemDetails.Detail = "See errors field for details.";
                        problemDetails.Instance = context.HttpContext.Request.Path;

                        // find out which status code to use
                        var actionExecutingContext =
                            context as Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext;

                        // if there are model state errors and all arguments were correctly found, then there are validation errors
                        if ((context.ModelState.ErrorCount > 0) &&
                            (actionExecutingContext?.ActionArguments.Count ==
                            context.ActionDescriptor.Parameters.Count))
                        {
                            problemDetails.Type = "https://tools.ietf.org/html/rfc4918#section-11.2";
                            problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                            problemDetails.Title = "One or more validation errors occurred.";

                            return new UnprocessableEntityObjectResult(problemDetails)
                            {
                                ContentTypes = { "application/problem+json" }
                            };
                        };

                        // if one of the arguments wasn't correctly found or couldn't be parsed
                        problemDetails.Status = StatusCodes.Status400BadRequest;
                        problemDetails.Title = "One or more input errors occurred.";
                        return new BadRequestObjectResult(problemDetails)
                        {
                            ContentTypes = { "application/problem+json" }
                        };
                    };
                });

            services.Configure<MvcOptions>(config =>
            {
                var newtonsoftJsonOutputFormatter = config.OutputFormatters
                    .OfType<NewtonsoftJsonOutputFormatter>()?.FirstOrDefault();

                if(newtonsoftJsonOutputFormatter != null)
                {
                    newtonsoftJsonOutputFormatter.SupportedMediaTypes.Add("application/vnd.pe7.hateoas+json");
                }
            });

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddDbContext<Models.WineDataContext>(options =>
                options.UseMySQL(Configuration.GetConnectionString("DefaultConnection")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unexpected fault occurred. Please try again later.");
                    });
                });
            }

            app.UseResponseCaching();

            // use this to apply response cache settings globally
            app.UseHttpCacheHeaders();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
