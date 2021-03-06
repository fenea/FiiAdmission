﻿using System;
using System.Text;
using Api.Auth;
using AutoMapper;
using Business.AccountsRepo;
using Business.AnnouncementsRepo;
using Business.CandidatesRepo;
using Business.EmailServices;
using Business.RepartitionRepo;
using Business.SignalR;
using Business.StorageAzureServices.Implementation;
using Business.StorageAzureServices.Interfaces;
using Data.Domain;
using Data.Persistence.ApplicationUserDb;
using Data.Persistence.ContentDb;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;

namespace Api
{
    public class Startup
  {
    private const string SecretKey = "iNivDmHLpUA223sqsfhqGbMRdRj1PVkH";
    private readonly SymmetricSecurityKey _signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey));

    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddTransient<IApplicationUserDbContext, ApplicationUserDbContext>();
      services.AddTransient<IEmailSender, EmailSender>();
      services.AddTransient<IAnnouncementsRepository, AnnouncementsRepository>();
      services.AddTransient<IJobSeekerRepository, JobSeekerRepository>();
      services.AddTransient<IContentDbContext, ContentDbContext>();
      services.AddTransient<ICandidateRepository, CandidateRepository>();
      services.AddTransient<IRepartitionRepository,RepartitionRepository>();
      services.AddSingleton<IJwtFactory, JwtFactory>();

      services.AddDbContext<ContentDbContext>(options =>
        options.UseSqlServer(Configuration.GetConnectionString("ContentDbConnection")));

      services.AddDbContext<ApplicationUserDbContext>(options =>
        options.UseSqlServer(Configuration.GetConnectionString("AppUsersConnection")));

      // ===== Add Jwt Authentication ========
      var jwtAppSettingOptions = Configuration.GetSection(nameof(JwtIssuerOptions));

      services.Configure<JwtIssuerOptions>(options =>
      {
        options.Issuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
        options.Audience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)];
        options.SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
      });

      var tokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)],

        ValidateAudience = true,
        ValidAudience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)],

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _signingKey,

        RequireExpirationTime = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
      };

      services.AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
      }).AddJwtBearer(configureOptions =>
      {
        configureOptions.ClaimsIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
        configureOptions.TokenValidationParameters = tokenValidationParameters;
        configureOptions.SaveToken = true;
        configureOptions.RequireHttpsMetadata = false;
        configureOptions.IncludeErrorDetails = true;
      });

      // api user claim policy
      services.AddAuthorization(options =>
      {
        options.AddPolicy("Admin",
          policy => policy.RequireClaim("Admin", "Administrator"));
        options.AddPolicy("User", policy => policy.RequireClaim("User", "User"));
      });
      // JWT End
      /*
      services.Configure<MvcOptions>(options =>
      {
          options.Filters.Add(new RequireHttpsAttribute());
      });
      */
      services.AddIdentity<AppUser, IdentityRole>
        (o =>
        {
          // configure identity options
          o.Password.RequireDigit = false;
          o.Password.RequireLowercase = false;
          o.Password.RequireUppercase = false;
          o.Password.RequireNonAlphanumeric = false;
          o.Password.RequiredLength = 6;
          //o.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationUserDbContext>()
        .AddDefaultTokenProviders();

      // Add azure blob storage services

      services.AddScoped<IAzureBlobStorage>(factory =>
      {
        return new AzureBlobStorage(new AzureBlobSetings(
          storageAccount: Configuration["Blob_StorageAccount"],
          storageKey: Configuration["Blob_StorageKey"],
          containerName: Configuration["Blob_ContainerName"]));
      });

      services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
      {
        builder.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
      }));
      // Add application services.

      services.AddMvc().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>());

      services.AddAutoMapper();

      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new Info {Title = "FIIAdmission API", Version = "v1"});
      });

      services.AddSignalR();

      services.AddMvc();


    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseSignalR(routes =>
      {
        routes.MapHub<Signal>("signal");
      });

      app.UseCors("MyPolicy");
      // Enable middleware to serve generated Swagger as a JSON endpoint.
      app.UseSwagger();
      /*
      var options = new RewriteOptions()
          .AddRedirectToHttps();
     
      app.UseRewriter(options);
      */
      // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
      app.UseSwaggerUI(c =>
      {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FIIAdmission API");
      });
      app.UseDefaultFiles();
      app.UseStaticFiles();

      app.UseMvc();
    }
  }
}