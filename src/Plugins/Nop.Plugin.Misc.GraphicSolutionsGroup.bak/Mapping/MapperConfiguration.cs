using AutoMapper;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure.Mapper;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Mapping;

/// <summary>
/// Represents AutoMapper configuration for plugin models
/// </summary>
public class MapperConfiguration : Profile, IOrderedMapperProfile
{
    #region Ctor

    public MapperConfiguration()
    {
        CreateMap<Customer, StoreCustomerModel>()
            .ForMember(model => model.StoreId, options => options.Ignore());
    }

    #endregion

    #region Properties

    /// <summary>
    /// Order of this mapper implementation
    /// </summary>
    public int Order => 1;

    #endregion
}