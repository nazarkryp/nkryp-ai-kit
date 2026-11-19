namespace AppName.Api.Infrastructure.Common.Mapping;

public interface IMapper<in TSource, out TDestination>
{
    TDestination Map(TSource source);
}