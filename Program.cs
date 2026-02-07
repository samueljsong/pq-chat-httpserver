using pq_chat_httpserver.Services;
using pq_chat_httpserver.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<UserService>(); // Concrete example of DI --> if someone wants UserService create it like this.
builder.Services.AddScoped<UserDatabase>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

// This is the middleware that states lets use the specific policy.
// It should be ran before you map the controllers.
app.UseCors("AllowAll"); 
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();