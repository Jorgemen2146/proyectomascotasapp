using Amazon.S3;
using DogPlatform.Pets.Application;
using DogPlatform.Pets.Application.Features.Pets.GetVaccinationContexts;
using DogPlatform.Pets.Application.Queries;
using DogPlatform.Pets.Application.Security;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Domain.Repositories;
using DogPlatform.Pets.Infrastructure.Persistence.Context;
using DogPlatform.Pets.Infrastructure.Persistence.Queries;
using DogPlatform.Pets.Infrastructure.Persistence.Repositories;
using DogPlatform.Pets.Infrastructure.Security;
using DogPlatform.Pets.Infrastructure.Storage;
using Microsoft.AspNetCore.DataProtection;
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
        services.AddScoped<IPetQueryService, PetQueryService>();
        services.AddScoped<IPetVaccinationContextQueryService, PetVaccinationContextQueryService>();
        services.AddScoped<IBreedRepository, BreedRepository>();
        services.AddScoped<ISpeciesRepository, SpeciesRepository>();

        services.AddDataProtection();
        var storageSection = configuration.GetSection(StorageOptions.SectionName);
        services.Configure<StorageOptions>(storageSection);

        var s3Section = configuration.GetSection(S3StorageOptions.SectionName);
        services.Configure<S3StorageOptions>(s3Section);

        var provider = storageSection["Provider"] ?? "Local";
        if (string.Equals(provider, "S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddAWSService<IAmazonS3>();
            services.AddScoped<IPhotoStorageService, S3PhotoStorageService>();
        }
        else if (string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IPhotoStorageService, LocalPetPhotoStorage>();
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported photo storage provider '{provider}'. Use Local or S3.");
        }

        return services;
    }
}

