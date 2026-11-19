namespace AppName.ServiceName.Application.Common.Mapping;

public interface IMapper<in TSource, out TDestination>
{
    TDestination Map(TSource source);
}