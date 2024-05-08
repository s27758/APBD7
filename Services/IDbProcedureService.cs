using Product_Warehouse.Models;

namespace Product_Warehouse.Services
{
    public interface IDbProcedureService
    {
        Task<int> AddProductToWarehouseAsync(ProductWarehouse productWarehouse);
    }
}