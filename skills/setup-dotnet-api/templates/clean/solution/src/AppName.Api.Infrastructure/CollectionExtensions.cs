using AppName.Common;

namespace AppName.Api.Infrastructure;

public static class CollectionExtensions
{
    extension(CollectionSettings settings)
    {
        /// <summary>Limits the requested page size to <see cref="CollectionSettings.MaxLimit"/>.</summary>
        public PaginationParameters CreatePaginationParameters(string? cursor, int? requestedLimit)
        {
            var limit = requestedLimit is > 0 ? requestedLimit.Value : settings.DefaultLimit;

            return new PaginationParameters(cursor, Math.Min(limit, settings.MaxLimit));
        }
    }
}
