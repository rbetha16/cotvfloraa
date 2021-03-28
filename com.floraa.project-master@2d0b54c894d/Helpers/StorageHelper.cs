using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Table;


namespace CoreBot.Helpers
{
    public class StorageHelper
    {
        //private static IConfiguration _configuration;
        private static CloudStorageAccount _storageAccount;
        private static CloudTableClient _tableClient;
        private static CloudTable _feedbackTable;

      

        public  async Task StoreFeedback(IConfiguration _configuration, FeedbackEntity feedbackEntity)
        {
            var connectionString = _configuration["storageConnectionString"];
            var tableName = "feedback";            
            TableOperation insertOperation = TableOperation.Insert(feedbackEntity);
            try
            {
                _storageAccount = CloudStorageAccount.Parse(connectionString);

                // Create the table client.
                _tableClient = _storageAccount.CreateCloudTableClient();

                // Get a reference to the table
                _feedbackTable = _tableClient.GetTableReference(tableName);
                await _feedbackTable.ExecuteAsync(insertOperation);               
            }
            catch (Exception e)
            {
                Console.WriteLine($"Insert to table failed with error {e.Message}");
                throw;
            }
        }
    }
}
