using GraphQL.Conventions;
using GraphQL.Server;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Query = Forte.NET.Schema.Query;

namespace Forte.NET {
    public class Startup {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            var engine = new GraphQLEngine()
                .WithQuery<Query>()
                .BuildSchema();
            var schema = engine.GetSchema();

            services.AddControllers();
            services.AddSingleton(schema);
            services.AddWebSockets(options => { });
            services
                .AddGraphQL((options, provider) => {
                    var logger = provider.GetRequiredService<ILogger<Startup>>();
                    options.UnhandledExceptionDelegate = ctx =>
                        logger.LogError(
                            "GraphQL Error: {Error}\n{Stacktrace}",
                            ctx.OriginalException.Message,
                            ctx.OriginalException.StackTrace
                        );
                })
                .AddWebSockets()
                .AddSystemTextJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            app.UseWebSockets();
            app.UseGraphQLWebSockets<ISchema>();
            app.UseGraphiQLServer(new GraphiQLOptions { Path = "/graphiql" });
            app.UseGraphQL<ISchema>();
        }
    }
}
