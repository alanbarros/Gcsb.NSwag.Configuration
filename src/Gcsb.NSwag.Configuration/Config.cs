using NSwag;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gcsb.NSwag.Configuration
{
    public class Config
    {
        public string Title { get; set; }
        public string Version { get; set; }
        public Type SchemaProcessor { get; }
        public List<Security> Securities { get; set; }

        public Config(string title, string version, Type schemaProcessor = null, List<Security> securities = null)
        {
            Title = title;
            Version = version;
            SchemaProcessor = schemaProcessor;
            Securities = securities ?? DefaultSecurities();
        }

        public OpenApiSecurityScheme GetSecuritySchemaByType(SecurityScheme securityScheme)
        {
            var schema = Securities.Where(a => a.SecurityType == securityScheme);

            if (schema.Any())
                return schema.First().OpenApiSecurityScheme();

            return Securities.First(a => a.SecurityType == SecurityScheme.Basic).OpenApiSecurityScheme();
        }

        private List<Security> DefaultSecurities()
        {
            var authorityUrl = Environment.GetEnvironmentVariable("OAUTH_AUTHORITY");
            var apiName = Environment.GetEnvironmentVariable("OAUTH_APINAME");

            var basicAuth = new SecurityBasic("Authorization", OpenApiSecuritySchemeType.ApiKey,
                    OpenApiSecurityApiKeyLocation.Header,
                    "Copy 'Bearer ' + valid JWT token into field");

            if (authorityUrl is null || apiName is null)
                return new List<Security> { basicAuth };

            var OAuth = new SecurityOAuth("Authorization", OpenApiSecuritySchemeType.OAuth2,
                    OpenApiSecurityApiKeyLocation.Header,
                    new Dictionary<string, string>() { { apiName, "Access API Scope" } },
                    $"{authorityUrl}/connect/token", OpenApiOAuth2Flow.Application);

            return new List<Security> { basicAuth, OAuth };
        }
    }

    public enum SecurityScheme
    {
        Basic,
        OAuth
    }

    public abstract class Security
    {
        public string Name { get; private set; }
        public SecurityScheme SecurityType { get; private set; }
        public OpenApiSecuritySchemeType Type { get; private set; }
        public OpenApiSecurityApiKeyLocation In { get; private set; }

        public abstract OpenApiSecurityScheme OpenApiSecurityScheme();

        protected Security(string name, SecurityScheme securityType,
            OpenApiSecuritySchemeType type, OpenApiSecurityApiKeyLocation @in)
        {
            Name = name;
            SecurityType = securityType;
            Type = type;
            In = @in;
        }
    }

    public class SecurityBasic : Security
    {
        public string Description { get; private set; }

        public SecurityBasic(string name,
            OpenApiSecuritySchemeType type, OpenApiSecurityApiKeyLocation @in,
            string description) : base(name, SecurityScheme.Basic, type, @in)
        {
            Description = description;
        }

        public override OpenApiSecurityScheme OpenApiSecurityScheme()
        {
            return new OpenApiSecurityScheme
            {
                Name = Name,
                In = In,
                Type = Type,
                Description = Description
            };
        }
    }

    public class SecurityOAuth : Security
    {
        public Dictionary<string, string> Scopes { get; set; }
        public string TokenUrl { get; set; }
        public OpenApiOAuth2Flow Flow { get; set; }

        public SecurityOAuth(string name, OpenApiSecuritySchemeType type,
            OpenApiSecurityApiKeyLocation @in, Dictionary<string, string> scopes,
            string tokenUrl, OpenApiOAuth2Flow flow)
            : base(name, SecurityScheme.OAuth, type, @in)
        {
            Scopes = scopes;
            TokenUrl = tokenUrl;
            Flow = flow;
        }

        public override OpenApiSecurityScheme OpenApiSecurityScheme()
        {
            return new OpenApiSecurityScheme
            {
                Name = Name,
                In = In,
                Type = Type,
                TokenUrl = TokenUrl,
                Flow = Flow,
                Scopes = Scopes
            };
        }
    }
}