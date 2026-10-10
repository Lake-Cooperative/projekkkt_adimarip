// Snapshot placeholder. Regenerate with `dotnet ef migrations add` after configuring the project startup.
// EF Core will create the full model snapshot based on AdimaripDbContext.OnModelCreating.
using Adimarip.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adimarip.Data.Migrations;

[DbContext(typeof(AdimaripDbContext))]
partial class AdimaripDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.11");
        // The model configuration of record is maintained in AdimaripDbContext.cs.
        // Run `dotnet ef migrations add InitialDomainSchema` in the host project to generate a canonical snapshot.
    }
}
