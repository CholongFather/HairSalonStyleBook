using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Services;

public interface IShopConfigService
{
    Task<ShopConfig> GetAsync();
    Task SaveAsync(ShopConfig config);
}
