using Amazon.S3;
using DogPlatform.Pets.Application;
using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.Pets.Infrastructure.Persistence.Context;
using DogPlatform.Pets.Infrastructure.Persistence.Repositories;
using DogPlatform.Pets.Infrastructure.Security;
using DogPlatform.Pets.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DogPlatform.Pets.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICurrentUser, CurrentUserService>();

        services.AddDbContext<PetsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("PetsDb")));

        services.AddScoped<IPetsUnitOfWork>(sp => sp.GetRequiredService<PetsDbContext>());
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IPetPhotoRepository, PetPhotoRepository>();
        services.AddScoped<IBreedRepository, BreedRepository>();
        services.AddScoped<ISpeciesRepository, SpeciesRepository>();

        // ?? S3 Storage ??????????????????????????????????????????????????????????
        var s3Section = configuration.GetSection(S3StorageOptions.SectionName);
        services.Configure<S3StorageOptions>(s3Section);

        var s3Enabled = s3Section.GetValue<bool>("Enabled");
        if (s3Enabled)
        {
            // IAmazonS3 resolves credentials from the standard AWS chain:
            // local profile ? environment variables ? IAM role (EC2/ECS).
            // Never store credentials in appsettings.
            services.AddAWSService<IAmazonS3>();
        }
        else
        {
            // Register a stub so DI doesn't fail when Enabled=false
            services.AddSingleton<IAmazonS3>(_ =>
                new AmazonS3Client(new Amazon.Runtime.AnonymousAWSCredentials(),
                    Amazon.RegionEndpoint.USEast1));
        }

        services.AddScoped<IPhotoStorageService, S3PhotoStorageService>();
        // ????????????????????????????????????????????????????????????????????????

        return services;
    }
}

