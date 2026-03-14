using BookWise.Domain.Entities;
using BookWise.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookWise.Infrastructure.Data.Seed;

public static class GenreSeeder
{
    public readonly record struct SeedSummary(int Added, int Reactivated);

    public static SeedSummary SeedForUser(BookWiseDbContext db, int userId, ILogger logger)
    {
        var existingGenres = db.Genres
            .IgnoreQueryFilters()
            .Where(g => g.UserAccountId == userId)
            .ToList();

        var existingByName = existingGenres
            .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var reactivated = 0;

        foreach (var seed in GetMainGenres(userId))
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

    private static IEnumerable<Genre> GetMainGenres(int userId)
    {
        return
        [
            new Genre(userId, "Romance", null),
            new Genre(userId, "Fantasia", null),
            new Genre(userId, "Ficção Científica", null),
            new Genre(userId, "Aventura", null),
            new Genre(userId, "Mistério", null),
            new Genre(userId, "Suspense", null),
            new Genre(userId, "Thriller", null),
            new Genre(userId, "Terror", null),
            new Genre(userId, "Drama", null),
            new Genre(userId, "Poesia", null),
            new Genre(userId, "Contos", null),
            new Genre(userId, "Clássicos", null),
            new Genre(userId, "Literatura Brasileira", null),
            new Genre(userId, "Literatura Estrangeira", null),
            new Genre(userId, "Infantil", null),
            new Genre(userId, "Jovem Adulto", null),
            new Genre(userId, "Quadrinhos", null),
            new Genre(userId, "Mangá", null),
            new Genre(userId, "Não-ficção", null),
            new Genre(userId, "Biografia", null),
            new Genre(userId, "História", null),
            new Genre(userId, "Política", null),
            new Genre(userId, "Filosofia", null),
            new Genre(userId, "Psicologia", null),
            new Genre(userId, "Religião e Espiritualidade", null),
            new Genre(userId, "Autoajuda", null),
            new Genre(userId, "Desenvolvimento Pessoal", null),
            new Genre(userId, "Negócios", null),
            new Genre(userId, "Finanças", null),
            new Genre(userId, "Educação", null),
            new Genre(userId, "Ciência", null),
            new Genre(userId, "Saúde", null),
            new Genre(userId, "Tecnologia", null),
            new Genre(userId, "Programação", null),
            new Genre(userId, "Artes", null),
            new Genre(userId, "Gastronomia", null),
            new Genre(userId, "Viagem", null)
        ];
    }
}
