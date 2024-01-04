﻿using Finbuckle.MultiTenant;
using FSH.Framework.Abstractions.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Infrastructure.Persistence;
public class FshDbContext : MultiTenantDbContext
{
    private readonly IPublisher _publisher;
    private readonly DbConfig _settings;
    public FshDbContext(ITenantInfo currentTenant, DbContextOptions options, IPublisher publisher, IOptions<DbConfig> settings)
        : base(currentTenant, options)
    {
        _publisher = publisher;
        _settings = settings.Value;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();

        if (!string.IsNullOrWhiteSpace(TenantInfo?.ConnectionString))
        {
            optionsBuilder.ConfigureDatabase(_settings.Provider, TenantInfo.ConnectionString);
        }
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PublishDomainEventsAsync().ConfigureAwait(false);
        return result;
    }
    private async Task PublishDomainEventsAsync()
    {
        var domainEvents = ChangeTracker.Entries<IEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .SelectMany(e =>
            {
                var domainEvents = e.DomainEvents.ToList();
                e.DomainEvents.Clear();
                return domainEvents;
            })
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent).ConfigureAwait(false);
        }
    }
}
