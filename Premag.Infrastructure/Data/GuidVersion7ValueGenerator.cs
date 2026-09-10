using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Premag.Core;

namespace Premag.Infrastructure.Data;

internal sealed class GuidVersion7ValueGenerator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;
    public override Guid Next(EntityEntry entry) => GeradorId.Novo();
}
