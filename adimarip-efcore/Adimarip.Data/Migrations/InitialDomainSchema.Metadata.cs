using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Adimarip.Data.Migrations;

// The original migration lacked its discoverability metadata. Keep the migration
// itself unchanged; EF Core can now find and apply it to a clean PostgreSQL volume.
[DbContext(typeof(AdimaripDbContext))]
[Migration("202610100001_InitialDomainSchema")]
public partial class InitialDomainSchema;
