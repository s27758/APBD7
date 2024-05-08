using Microsoft.Data.SqlClient;
using Product_Warehouse.Exceptions;
using Product_Warehouse.Models;

namespace Product_Warehouse.Services
{
    public class DbService : IDbService
    {
        private readonly string connectionString = @"Data Source=localhost;Initial Catalog=APBD;Integrated Security=True";

        

        public async Task<int> AddProductToWarehouseAsync(ProductWarehouse productWarehouse)
        {
            using var connection = new SqlConnection(connectionString);
            using var cmd = new SqlCommand();

            cmd.Connection = connection;
            await connection.OpenAsync();
            
            // Sprawdzamy, czy produkt o podanym identyfikatorze istnieje. Następnie sprawdzamy, czy magazyn o podanym identyfikatorze istnieje.
            // Wartość ilości przekazana w żądaniu powinna być większa niż 0.
            cmd.CommandText = "SELECT TOP 1 [Order].IdOrder FROM [Order] " +
                "LEFT JOIN Product_Warehouse ON [Order].IdOrder = Product_Warehouse.IdOrder " +
                "WHERE [Order].IdProduct = @IdProduct " +
                "AND [Order].Amount = @Amount " +
                "AND Product_Warehouse.IdProductWarehouse IS NULL " +
                "AND [Order].CreatedAt < @CreatedAt";

            cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdProduct);
            cmd.Parameters.AddWithValue("Amount", productWarehouse.Amount);
            cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);
            
            var reader = await cmd.ExecuteReaderAsync();
            
            //Możemy dodać produkt do magazynu tylko wtedy, gdy istnieje zamówienie zakupu produktu w tabeli Order.
            //Dlatego sprawdzamy, czy w tabeli Order istnieje rekord z IdProduktu i Ilością (Amount),
            //które odpowiadają naszemu żądaniu. Data utworzenia zamówienia powinna być wcześniejsza niż data utworzenia w żądaniu.
            if (!reader.HasRows) throw new MyException("Invalid parameter: there is no order to fullfill!");
            
            await reader.ReadAsync();
            int idOrder = int.Parse(reader["IdOrder"].ToString());
            await reader.CloseAsync();
            
            //Sprawdzamy, czy to zamówienie zostało przypadkiem zrealizowane.
            //Sprawdzamy, czy nie ma wiersza z danym IdOrder w tabeli Product_Warehouse.
            cmd.Parameters.Clear();

            cmd.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdProduct);

            reader = await cmd.ExecuteReaderAsync();
            
            if (!reader.HasRows) throw new MyException("Invalid parameter: provided IdProduct does not exist!");
            
            await reader.ReadAsync();
            double price = double.Parse(reader["Price"].ToString());
            await reader.CloseAsync();

            cmd.Parameters.Clear();
            //
            
            //
            cmd.CommandText = "SELECT IdWarehouse FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            cmd.Parameters.AddWithValue("IdWarehouse", productWarehouse.IdWarehouse);

            reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows) throw new MyException("Invalid parameter: provided IdWarehouse does not exist!");

            await reader.CloseAsync();
            cmd.Parameters.Clear();
            

            var transaction = (SqlTransaction) await connection.BeginTransactionAsync();
            cmd.Transaction = transaction;

            try
            {   
                //Aktualizujemy kolumnę FullfilledAt zamówienia na aktualną datę i godzinę. (UPDATE)
                cmd.CommandText = "UPDATE [Order] SET FulfilledAt = @CreatedAt WHERE IdOrder = @IdOrder";
                cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);
                cmd.Parameters.AddWithValue("IdOrder", idOrder);

                int rowsUpdated = await cmd.ExecuteNonQueryAsync();

                if (rowsUpdated < 1) throw new MyException();
                //
                cmd.Parameters.Clear();
                
                //Wstawiamy rekord do tabeli Product_Warehouse. Kolumna Price powinna odpowiadać cenie produktu pomnożonej
                //przez kolumnę Amount z naszego zamówienia. Ponadto wstawiamy wartość CreatedAt zgodnie z aktualnym czasem.
                //(INSERT)
                cmd.CommandText = "INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) " +
                    $"VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Amount*{price}, @CreatedAt)";

                cmd.Parameters.AddWithValue("IdWarehouse", productWarehouse.IdWarehouse);
                cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdProduct);
                cmd.Parameters.AddWithValue("IdOrder", idOrder);
                cmd.Parameters.AddWithValue("Amount", productWarehouse.Amount);
                cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);

                int rowsInserted = await cmd.ExecuteNonQueryAsync();

                if (rowsInserted < 1) throw new MyException();
                // 
                
                await transaction.CommitAsync();
            } catch (Exception) { 
                await transaction.RollbackAsync();
                throw new MyException("Something went wrong! Database internal server problem!");
            }

            cmd.Parameters.Clear();

            cmd.CommandText = "SELECT TOP 1 IdProductWarehouse FROM Product_Warehouse ORDER BY IdProductWarehouse DESC";

            reader = await cmd.ExecuteReaderAsync();

            await reader.ReadAsync();
            int idProductWarehouse = int.Parse(reader["IdProductWarehouse"].ToString());
            await reader.CloseAsync();

            await connection.CloseAsync();
           
            return idProductWarehouse;
        }
    }
}