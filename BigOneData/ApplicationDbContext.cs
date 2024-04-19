using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration.Json;
public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }

    public DbSet<Sound> Sounds { get; set; }
}

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())  
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        builder.UseSqlServer(connectionString);

        return new ApplicationDbContext(builder.Options);
    }
}