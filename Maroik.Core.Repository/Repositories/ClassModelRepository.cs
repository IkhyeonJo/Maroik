using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Finances;
using Maroik.Core.PostgreSQL.Contexts;
using Microsoft.EntityFrameworkCore;
using OrmClassModel = Maroik.Core.PostgreSQL.Models.ClassModel;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core repository for <see cref="ClassModel"/> records.</summary>
public class ClassModelRepository(ApplicationDbContext context)
    : GenericRepository<ClassModel, OrmClassModel>(context), IClassModelRepository
{
    /// <summary>The underlying <see cref="DbSet{TEntity}"/> for <see cref="OrmClassModel"/> rows.</summary>
    protected override DbSet<OrmClassModel> Set => Context.ClassModels;

    /// <summary>Maps a persisted <see cref="OrmClassModel"/> row to the <see cref="ClassModel"/> domain object.</summary>
    protected override ClassModel ToDomain(OrmClassModel e)
        => ClassModel.Reconstitute(e.Id, e.AccountEmail, e.Book, e.FilePath, e.Fingerprint, e.ModelVersion, e.Trained);

    /// <summary>Maps a <see cref="ClassModel"/> domain object to its <see cref="OrmClassModel"/> persistence representation.</summary>
    protected override OrmClassModel ToEntity(ClassModel m) => new()
    {
        Id = m.Id,
        AccountEmail = m.AccountEmail,
        Book = m.Book,
        FilePath = m.FilePath,
        Fingerprint = m.Fingerprint,
        ModelVersion = m.ModelVersion,
        Trained = m.Trained
    };

    /// <inheritdoc />
    public Task<ClassModel?> FindAsync(string accountEmail, string book, CancellationToken ct = default)
        => QueryFirstAsync(e => e.AccountEmail == accountEmail && e.Book == book, ct: ct);
}
