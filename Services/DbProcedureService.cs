using System.Data;
using Microsoft.Data.SqlClient;
using Product_Warehouse.Exceptions;
using Product_Warehouse.Models;

namespace Product_Warehouse.Services
{
    public class DbProcedureService : IDbProcedureService
    {
        private readonly string connectionString = @"Data Source=localhost;Initial Catalog=APBD;Integrated Security=True";


        public async Task<int> AddProductToWarehouseAsync(ProductWarehouse productWarehouse)
        {
            int idProductWarehouse = 0;

            using var connection = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("AddProductToWarehouse", connection);

            var transaction = (SqlTransaction) await connection.BeginTransactionAsync();
            cmd.Transaction = transaction;

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdProduct);
                cmd.Parameters.AddWithValue("IdWarehouse", productWarehouse.IdWarehouse);
                cmd.Parameters.AddWithValue("Amount", productWarehouse.Amount);
                cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);

                await connection.OpenAsync();
                int rowsChanged = await cmd.ExecuteNonQueryAsync();

                if (rowsChanged < 1) throw new MyException();

                await transaction.CommitAsync();
            } catch (Exception)
            {
                await transaction.RollbackAsync();
                throw new Exception();
            }

            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT TOP 1 IdProductWarehouse FROM Product_Warehouse ORDER BY IdProductWarehouse DESC";

            using var reader = await cmd.ExecuteReaderAsync();

            await reader.ReadAsync();
            if (await reader.ReadAsync())
                idProductWarehouse = int.Parse(reader["IdProductWarehouse"].ToString());
            await reader.CloseAsync();

            await connection.CloseAsync();

            return idProductWarehouse;
        }
    }
}
