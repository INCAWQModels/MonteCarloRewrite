using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SQLite;

namespace MC
{
    class resultsDatabase
    {
        protected SQLiteConnection connection;

        protected resultsDatabase()
        {
            connection = new SQLiteConnection();
        }

        protected void connect()
        {
            string localConnectionString = $"Data Source={MCParameters.databaseFileName};Version=3;";
            connection.ConnectionString = localConnectionString;
            connection.Open();
        }

        protected void disconnect()
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                connection.Close();
            }
        }

        // Optional: Method to dispose of the connection properly
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                disconnect();
                connection?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}