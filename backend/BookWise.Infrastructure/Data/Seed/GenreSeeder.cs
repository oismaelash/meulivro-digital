using BookWise.Domain.Entities;
using BookWise.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace BookWise.Infrastructure.Data.Seed;

public static class GenreSeeder
{
    public static void Seed(BookWiseDbContext db)
    {
        var existing = db.Genres
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(g => g.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = GetMainGenres()
            .Where(g => !existing.Contains(g.Name))
            .ToList();

        if (toAdd.Count == 0) return;

        db.Genres.AddRange(toAdd);
        db.SaveChanges();
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
