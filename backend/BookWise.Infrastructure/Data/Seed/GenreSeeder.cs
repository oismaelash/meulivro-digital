using BookWise.Domain.Entities;
using BookWise.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookWise.Infrastructure.Data.Seed;

public static class GenreSeeder
{
    public readonly record struct SeedSummary(int Added, int Reactivated);

    public static SeedSummary Seed(BookWiseDbContext db, ILogger logger)
    {
        var existingGenres = db.Genres
            .IgnoreQueryFilters()
            .ToList();

        var existingByName = existingGenres
            .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var reactivated = 0;

        foreach (var seed in GetMainGenres())
        {
            if (!existingByName.TryGetValue(seed.Name, out var existing))
            {
                db.Genres.Add(seed);
                added++;
                continue;
            }

            if (existing.IsActive)
                continue;

            db.Entry(existing).Property(e => e.IsActive).CurrentValue = true;
            db.Entry(existing).Property(e => e.UpdatedAt).CurrentValue = DateTime.UtcNow;
            reactivated++;
        }

        if (added == 0 && reactivated == 0)
        {
            logger.LogInformation("Genre seed: nothing to change");
            return new SeedSummary(0, 0);
        }

        db.SaveChanges();
        logger.LogInformation("Genre seed applied: added {Added}, reactivated {Reactivated}", added, reactivated);
        return new SeedSummary(added, reactivated);
    }

    private static IEnumerable<Genre> GetMainGenres()
    {
        return
        [
            new Genre("Romance", null),
            new Genre("Fantasia", null),
            new Genre("Ficção Científica", null),
            new Genre("Aventura", null),
            new Genre("Mistério", null),
            new Genre("Suspense", null),
            new Genre("Thriller", null),
            new Genre("Terror", null),
            new Genre("Drama", null),
            new Genre("Poesia", null),
            new Genre("Contos", null),
            new Genre("Clássicos", null),
            new Genre("Literatura Brasileira", null),
            new Genre("Literatura Estrangeira", null),
            new Genre("Infantil", null),
            new Genre("Jovem Adulto", null),
            new Genre("Quadrinhos", null),
            new Genre("Mangá", null),
            new Genre("Não-ficção", null),
            new Genre("Biografia", null),
            new Genre("História", null),
            new Genre("Política", null),
            new Genre("Filosofia", null),
            new Genre("Psicologia", null),
            new Genre("Religião e Espiritualidade", null),
            new Genre("Autoajuda", null),
            new Genre("Desenvolvimento Pessoal", null),
            new Genre("Negócios", null),
            new Genre("Finanças", null),
            new Genre("Educação", null),
            new Genre("Ciência", null),
            new Genre("Saúde", null),
            new Genre("Tecnologia", null),
            new Genre("Programação", null),
            new Genre("Artes", null),
            new Genre("Gastronomia", null),
            new Genre("Viagem", null)
        ];
    }
}
