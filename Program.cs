    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.IdentityModel.Tokens;
    using System.Text;

    using pq_chat_httpserver.Services;
    using pq_chat_httpserver.Database;

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllers();
    // Services
    builder.Services.AddScoped<UserService>();
    builder.Services.AddScoped<FriendshipService>();
    builder.Services.AddScoped<ConversationService>();
    builder.Services.AddScoped<JwtService>();

    // Databases
    builder.Services.AddScoped<UserDatabase>();
    builder.Services.AddScoped<FriendshipDatabase>();
    builder.Services.AddScoped<ConversationDatabase>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });

    // Auth
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new Exception("Jwt:Key is missing in configuration.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Middleware order 
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    // Map HTTP controllers 
    app.MapControllers();

    app.Run();
