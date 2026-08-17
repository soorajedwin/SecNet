using Microsoft.EntityFrameworkCore;
using SecNetData.Schema;

namespace SecNetData.Context;

public class SecDbContext : DbContext
{
    private readonly SecModelConfigurer? _modelConfigurer;

    public SecDbContext(
        DbContextOptions<SecDbContext> options)
        : base(options)
    {
    }

    public SecDbContext(
        DbContextOptions<SecDbContext> options,
        SecModelConfigurer modelConfigurer)
        : base(options)
    {
        _modelConfigurer = modelConfigurer;
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all discovered SecEntity types dynamically
        if (_modelConfigurer != null)
        {
            _modelConfigurer.ConfigureModel(modelBuilder);
        }
    }
}
