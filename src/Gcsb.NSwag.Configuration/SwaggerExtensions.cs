using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NJsonSchema.Generation;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace Gcsb.NSwag.Configuration
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection Swagger(this IServiceCollection services, Config config,
            Func<(Predicate<Type> eligibleControllers, SecurityScheme securityScheme)> func = null)
        {
            (var eligibleControllers, var securityScheme) = func != null ? func() : ((x) => x != typeof(object), SecurityScheme.Basic);

            var operators = new ProcessorSwaggerControllers(eligibleControllers);

            services.AddSwaggerDocument((document, serviceProvider) =>
            {
                document.Title = config.Title;
                document.Version = config.Version;
                document.OperationProcessors.Add(new OperationSecurityScopeProcessor("JWT Token"));
                document.DocumentProcessors.Add(new SecurityDefinitionAppender("JWT Token", new List<string>(),
                    config.GetSecuritySchemaByType(securityScheme)));

                document.PostProcess = s =>
                {
                    s.Paths.ToList().ForEach(p =>
                    {
                        p.Value.Parameters.Add(
                        new OpenApiParameter()
                        {
                            Kind = OpenApiParameterKind.Header,
                            Type = NJsonSchema.JsonObjectType.String,
                            IsRequired = false,
                            Name = "Accept-Language",
                            Description = "pt-BR or en-US",
                            Default = "pt-BR"
                        });
                    });
                };

                document.OperationProcessors.Insert(0, operators);

                if (config.SchemaProcessor != null && serviceProvider.GetService(config.SchemaProcessor) is ISchemaProcessor schemaProcessor)
                    document.SchemaProcessors.Add(schemaProcessor);
            });

            return services;
        }

        public static IApplicationBuilder Swagger(this IApplicationBuilder app)
        {
            app.UseOpenApi(config =>
            {
                config.PostProcess = (document, request) =>
                {
                    document.Host = ExtractHost(request);
                    document.BasePath = ExtractPath(request);
                    document.Schemes.Clear();
                };
            });

            app.UseSwaggerUi3(config =>
            {
                config.TransformToExternalPath =
                    (route, request) => ExtractPath(request) + route;
            });

            return app;
        }

        private static string ExtractHost(HttpRequest request) =>
            request.Headers.ContainsKey("X-Forwarded-Host") ?
                new Uri($"{ExtractProto(request)}://{request.Headers["X-Forwarded-Host"].First()}").Host :
                    request.Host.Value;

        private static string ExtractProto(HttpRequest request) =>
            request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? request.Protocol;

        private static string ExtractPath(HttpRequest request) =>
            request.Headers.ContainsKey("X-Forwarded-Prefix") ?
                request.Headers["X-Forwarded-Prefix"].FirstOrDefault() :
                string.Empty;
    }
}