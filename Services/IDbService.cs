using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Product_Warehouse.Models;

namespace Product_Warehouse.Services
{
    public interface IDbService
    {
        Task<int> AddProductToWarehouseAsync(ProductWarehouse productWarehouse);
    }
}