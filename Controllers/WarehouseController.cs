using Microsoft.AspNetCore.Mvc;
using Product_Warehouse.Exceptions;
using Product_Warehouse.Models;
using Product_Warehouse.Services;
using System;
using System.Threading.Tasks;

namespace Product_Warehouse.Controllers
{
    [Route("api/warehouses")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IDbService _dbService;
        private readonly IDbProcedureService _dbProcedureService;

        public WarehouseController(IDbService dbService, IDbProcedureService dbProcedureService)
        {
            _dbService = dbService;
            _dbProcedureService = dbProcedureService;
        }

        // Dodaj produkt za pomocą standardowej logiki
        [HttpPost("addProduct")]
        public async Task<IActionResult> AddProductToWarehouse([FromBody] ProductWarehouse productWarehouse)
        {
            try
            {
                int idProductWarehouse = await _dbService.AddProductToWarehouseAsync(productWarehouse);
                return Ok($"Successfully added! ID: {idProductWarehouse}");
            }
            catch (MyException e)
            {
                return NotFound(e.Message);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        
        // Dodaj produkt za pomocą procedury składowanej
        [HttpPost("addProductUsingSP")]
        public async Task<IActionResult> AddProductToWarehouseUsingSP([FromBody] ProductWarehouse productWarehouse)
        {
            try 
            {
                int idProductWarehouse = await _dbProcedureService.AddProductToWarehouseAsync(productWarehouse);
                return Ok($"Successfully added using stored procedure! ID: {idProductWarehouse}");
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }
    }
}

