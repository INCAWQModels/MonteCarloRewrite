        private void CreateSQLiteDatabase()
        {
            Console.WriteLine("Creating SQLite database and tables...");
            
            sqliteConnection.Open();
            
            string createTablesScript = @"
                -- ENABLE FOREIGN KEY CONSTRAINTS
                PRAGMA FOREIGN_KEYS = ON;

                -- 1. PARAMETER NAMES LOOKUP TABLE
                CREATE TABLE IF NOT EXISTS PAR_NAMES (
                    PAR_ID INTEGER PRIMARY KEY,
                    PAR_NAME TEXT NOT NULL
                );

                -- 2. PARAMETER LIST - VALUES FOR EACH MODEL RUN
                CREATE TABLE IF NOT EXISTS PAR_LIST (
                    RUN_ID INTEGER NOT NULL,
                    PAR_ID INTEGER NOT NULL,
                    TEXT_VALUE TEXT,
                    NUMERIC_VALUE REAL,
                    PRIMARY KEY (RUN_ID, PAR_ID),
                    FOREIGN KEY (PAR_ID) REFERENCES PAR_NAMES(PAR_ID)
                );

                -- 3. SORTED PARAMETERS FOR ANALYSIS
                CREATE TABLE IF NOT EXISTS SORTED_PARAMETERS (
                    ID INTEGER PRIMARY KEY,
                    RUN_ID INTEGER NOT NULL,
                    PAR_ID INTEGER NOT NULL,
                    PARAMETER_VALUE REAL,
                    FOREIGN KEY (PAR_ID) REFERENCES PAR_NAMES(PAR_ID)
                );

                -- 4. MODEL SIMULATION RESULTS
                CREATE TABLE IF NOT EXISTS RESULTS (
                    RUN INTEGER NOT NULL,
                    ROW_NUMBER INTEGER NOT NULL,
                    REACH TEXT,
                    TERRESTRIAL_INPUT REAL,
                    FLOW REAL,
                    DATE_STAMP DATETIME,
                    PRIMARY KEY (RUN, ROW_NUMBER)
                );

                -- 5. STATISTICAL COEFFICIENTS AND PERFORMANCE METRICS
                CREATE TABLE IF NOT EXISTS COEFFICIENTS (
                    RUN INTEGER NOT NULL,
                    ROW_NUMBER INTEGER NOT NULL,
                    REACH TEXT,
                    PARAMETER TEXT,
                    R2 REAL,
                    NS REAL,
                    LOG_NS REAL,
                    RMSE REAL,
                    RE REAL,
                    AD REAL,
                    VAR REAL,
                    N REAL,
                    N_RE REAL,
                    SS REAL,
                    LOG_SS REAL,
                    DATE_STAMP DATETIME,
                    PRIMARY KEY (RUN, ROW_NUMBER, REACH, PARAMETER)
                );

                -- 6. COEFFICIENT WEIGHTS FOR ANALYSIS
                CREATE TABLE IF NOT EXISTS COEFFICIENT_WEIGHTS (
                    COEFFICIENT_NAME TEXT PRIMARY KEY,
                    COEFFICIENT_WEIGHT REAL NOT NULL
                );

                -- 7. INCA MODEL INPUT DATA
                CREATE TABLE IF NOT EXISTS INCA_INPUTS (
                    FILE_NAME TEXT NOT NULL,
                    RUN INTEGER NOT NULL,
                    ROW_NUMBER INTEGER NOT NULL,
                    SMD REAL,
                    HER REAL,
                    T REAL,
                    P REAL,
                    DATE_STAMP DATETIME,
                    PRIMARY KEY (FILE_NAME, RUN, ROW_NUMBER)
                );

                -- 8. OBSERVED DATA FOR MODEL VALIDATION
                CREATE TABLE IF NOT EXISTS OBSERVATIONS (
                    REACH TEXT NOT NULL,
                    PARAMETER TEXT NOT NULL,
                    VALUE REAL,
                    QC TEXT,
                    DATE_STAMP DATETIME,
                    PRIMARY KEY (REACH, PARAMETER, DATE_STAMP)
                );

                -- 9. STATISTICAL ANALYSIS D-VALUES
                CREATE TABLE IF NOT EXISTS D_VALUES (
                    ID INTEGER PRIMARY KEY,
                    RANK INTEGER,
                    PAR_NAME TEXT,
                    PAR_ID INTEGER,
                    X_RANGE REAL,
                    D REAL,
                    Z REAL,
                    P REAL,
                    ADJUSTED_P REAL,
                    FOREIGN KEY (PAR_ID) REFERENCES PAR_NAMES(PAR_ID)
                );

                -- 10. PARAMETER SENSITIVITY SUMMARY
                CREATE TABLE IF NOT EXISTS PARAMETER_SENSITIVITY_SUMMARY (
                    PAR_NAME TEXT NOT NULL,
                    PAR_ID INTEGER PRIMARY KEY,
                    D REAL,
                    MIN_OF_NUMERIC_VALUE REAL,
                    MAX_OF_NUMERIC_VALUE REAL,
                    X_RANGE REAL,
                    Z REAL,
                    P REAL,
                    FOREIGN KEY (PAR_ID) REFERENCES PAR_NAMES(PAR_ID)
                );

                -- 11. REACH/LOCATION LOOKUP TABLE
                CREATE TABLE IF NOT EXISTS REACH_ID (
                    ID_CODE INTEGER PRIMARY KEY,
                    REACH TEXT UNIQUE NOT NULL
                );

                -- CREATE INDEXES FOR BETTER PERFORMANCE
                CREATE INDEX IF NOT EXISTS IDX_PAR_LIST_RUN_ID ON PAR_LIST(RUN_ID);
                CREATE INDEX IF NOT EXISTS IDX_PAR_LIST_PAR_ID ON PAR_LIST(PAR_ID);
                CREATE INDEX IF NOT EXISTS IDX_SORTED_PARAMS_PAR_ID ON SORTED_PARAMETERS(PAR_ID);
                CREATE INDEX IF NOT EXISTS IDX_SORTED_PARAMS_RUN_ID ON SORTED_PARAMETERS(RUN_ID);
                CREATE INDEX IF NOT EXISTS IDX_RESULTS_RUN ON RESULTS(RUN);
                CREATE INDEX IF NOT EXISTS IDX_RESULTS_REACH ON RESULTS(REACH);
                CREATE INDEX IF NOT EXISTS IDX_COEFFICIENTS_RUN ON COEFFICIENTS(RUN);
                CREATE INDEX IF NOT EXISTS IDX_COEFFICIENTS_REACH ON COEFFICIENTS(REACH);
                CREATE INDEX IF NOT EXISTS IDX_INCA_INPUTS_RUN ON INCA_INPUTS(RUN);
                CREATE INDEX IF NOT EXISTS IDX_OBSERVATIONS_REACH ON OBSERVATIONS(REACH);
                CREATE INDEX IF NOT EXISTS IDX_D_VALUES_PAR_ID ON D_VALUES(PAR_ID);
            ";

            var command = new SQLiteCommand(createTablesScript, sqliteConnection);
            command.ExecuteNonQuery();
            
            Console.WriteLine("SQLite tables created successfully.");
        }using System;
using System.Data;
using System.Data.OleDb;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace MC.DataMigration
{
    public class AccessToSQLiteMigrator
    {
        private string accessConnectionString;
        private string sqliteConnectionString;
        private SQLiteConnection sqliteConnection;
        private OleDbConnection accessConnection;

        public AccessToSQLiteMigrator(string accessDbPath, string sqliteDbPath)
        {
            // Access connection string
            accessConnectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={accessDbPath};";
            
            // SQLite connection string
            sqliteConnectionString = $"Data Source={sqliteDbPath};Version=3;";
            
            // Initialize connections
            accessConnection = new OleDbConnection(accessConnectionString);
            sqliteConnection = new SQLiteConnection(sqliteConnectionString);
        }

        public void MigrateDatabase()
        {
            try
            {
                Console.WriteLine("Starting database migration from Access to SQLite...");
                
                // Create SQLite database and tables
                CreateSQLiteDatabase();
                
                // Migrate each table
                MigrateTable("PAR_NAMES", "SELECT ParID, ParName FROM ParNames");
                MigrateTable("PAR_LIST", "SELECT RunID, ParID, TextValue, NumericValue FROM ParList");
                MigrateTable("SORTED_PARAMETERS", "SELECT ID, RunID, ParID, ParameterValue FROM SortedParameters");
                MigrateTable("RESULTS", "SELECT RUN, RowNumber, Reach, TerrestrialInput, Flow, DateStamp FROM Results");
                MigrateTable("COEFFICIENTS", "SELECT RUN, RowNumber, Reach, Parameter, R2, NS, LOG_NS, RMSE, RE, AD, VAR, N, N_RE, SS, LOG_SS, DateStamp FROM Coefficients");
                MigrateTable("COEFFICIENT_WEIGHTS", "SELECT CoefficientName, CoefficientWeight FROM CoefficientWeights");
                MigrateTable("INCA_INPUTS", "SELECT FileName, RUN, RowNumber, SMD, HER, T, P, DateStamp FROM INCAInputs");
                MigrateTable("OBSERVATIONS", "SELECT Reach, Parameter, Value, QC, DateStamp FROM Observations");
                MigrateTable("D_VALUES", "SELECT ID, Rank, ParName, ParID, xRange, D, z, p, AdjustedP FROM DValues");
                MigrateTable("PARAMETER_SENSITIVITY_SUMMARY", "SELECT ParName, ParID, D, MinOfNumericValue, MaxOfNumericValue, xRange, z, p FROM ParameterSensitivitySummary");
                MigrateTable("REACH_ID", "SELECT IDCode, Reach FROM ReachID");
                
                Console.WriteLine("Database migration completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration failed: {ex.Message}");
                throw;
            }
            finally
            {
                accessConnection?.Close();
                sqliteConnection?.Close();
            }
        }

        private void CreateSQLiteDatabase()
        {
            Console.WriteLine("Creating SQLite database and tables...");
            
            sqliteConnection.Open();
            
            string createTablesScript = @"
                -- Enable foreign key constraints
                PRAGMA foreign_keys = ON;

                -- 1. Parameter Names lookup table
                CREATE TABLE IF NOT EXISTS ParNames (
                    ParID INTEGER PRIMARY KEY,
                    ParName TEXT NOT NULL
                );

                -- 2. Parameter List - values for each model run
                CREATE TABLE IF NOT EXISTS ParList (
                    RunID INTEGER NOT NULL,
                    ParID INTEGER NOT NULL,
                    TextValue TEXT,
                    NumericValue REAL,
                    PRIMARY KEY (RunID, ParID),
                    FOREIGN KEY (ParID) REFERENCES ParNames(ParID)
                );

                -- 3. Sorted Parameters for analysis
                CREATE TABLE IF NOT EXISTS SortedParameters (
                    ID INTEGER PRIMARY KEY,
                    RunID INTEGER NOT NULL,
                    ParID INTEGER NOT NULL,
                    ParameterValue REAL,
                    FOREIGN KEY (ParID) REFERENCES ParNames(ParID)
                );

                -- 4. Model simulation results
                CREATE TABLE IF NOT EXISTS Results (
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    Reach TEXT,
                    TerrestrialInput REAL,
                    Flow REAL,
                    DateStamp DATETIME,
                    PRIMARY KEY (RUN, RowNumber)
                );

                -- 5. Statistical coefficients and performance metrics
                CREATE TABLE IF NOT EXISTS Coefficients (
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    Reach TEXT,
                    Parameter TEXT,
                    R2 REAL,
                    NS REAL,
                    LOG_NS REAL,
                    RMSE REAL,
                    RE REAL,
                    AD REAL,
                    VAR REAL,
                    N REAL,
                    N_RE REAL,
                    SS REAL,
                    LOG_SS REAL,
                    DateStamp DATETIME,
                    PRIMARY KEY (RUN, RowNumber, Reach, Parameter)
                );

                -- 6. Coefficient weights for analysis
                CREATE TABLE IF NOT EXISTS CoefficientWeights (
                    CoefficientName TEXT PRIMARY KEY,
                    CoefficientWeight REAL NOT NULL
                );

                -- 7. INCA model input data
                CREATE TABLE IF NOT EXISTS INCAInputs (
                    FileName TEXT NOT NULL,
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    SMD REAL,
                    HER REAL,
                    T REAL,
                    P REAL,
                    DateStamp DATETIME,
                    PRIMARY KEY (FileName, RUN, RowNumber)
                );

                -- 8. Observed data for model validation
                CREATE TABLE IF NOT EXISTS Observations (
                    Reach TEXT NOT NULL,
                    Parameter TEXT NOT NULL,
                    Value REAL,
                    QC TEXT,
                    DateStamp DATETIME,
                    PRIMARY KEY (Reach, Parameter, DateStamp)
                );

                -- 9. Statistical analysis D-values
                CREATE TABLE IF NOT EXISTS DValues (
                    ID INTEGER PRIMARY KEY,
                    Rank INTEGER,
                    ParName TEXT,
                    ParID INTEGER,
                    xRange REAL,
                    D REAL,
                    z REAL,
                    p REAL,
                    AdjustedP REAL,
                    FOREIGN KEY (ParID) REFERENCES ParNames(ParID)
                );

                -- 10. Parameter sensitivity summary
                CREATE TABLE IF NOT EXISTS ParameterSensitivitySummary (
                    ParName TEXT NOT NULL,
                    ParID INTEGER PRIMARY KEY,
                    D REAL,
                    MinOfNumericValue REAL,
                    MaxOfNumericValue REAL,
                    xRange REAL,
                    z REAL,
                    p REAL,
                    FOREIGN KEY (ParID) REFERENCES ParNames(ParID)
                );

                -- 11. Reach/Location lookup table
                CREATE TABLE IF NOT EXISTS ReachID (
                    IDCode INTEGER PRIMARY KEY,
                    Reach TEXT UNIQUE NOT NULL
                );

                -- Create indexes for better performance
                CREATE INDEX IF NOT EXISTS idx_parlist_runid ON ParList(RunID);
                CREATE INDEX IF NOT EXISTS idx_parlist_parid ON ParList(ParID);
                CREATE INDEX IF NOT EXISTS idx_sortedparams_parid ON SortedParameters(ParID);
                CREATE INDEX IF NOT EXISTS idx_sortedparams_runid ON SortedParameters(RunID);
                CREATE INDEX IF NOT EXISTS idx_results_run ON Results(RUN);
                CREATE INDEX IF NOT EXISTS idx_results_reach ON Results(Reach);
                CREATE INDEX IF NOT EXISTS idx_coefficients_run ON Coefficients(RUN);
                CREATE INDEX IF NOT EXISTS idx_coefficients_reach ON Coefficients(Reach);
                CREATE INDEX IF NOT EXISTS idx_incainputs_run ON INCAInputs(RUN);
                CREATE INDEX IF NOT EXISTS idx_observations_reach ON Observations(Reach);
                CREATE INDEX IF NOT EXISTS idx_dvalues_parid ON DValues(ParID);
            ";

            var command = new SQLiteCommand(createTablesScript, sqliteConnection);
            command.ExecuteNonQuery();
            
            Console.WriteLine("SQLite tables created successfully.");
        }

        private void MigrateTable(string tableName, string selectQuery)
        {
            try
            {
                Console.WriteLine($"Migrating table: {tableName}");
                
                accessConnection.Open();
                
                using (var accessCommand = new OleDbCommand(selectQuery, accessConnection))
                using (var reader = accessCommand.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        Console.WriteLine($"  No data found in {tableName}");
                        accessConnection.Close();
                        return;
                    }

                    // Get column information
                    var schemaTable = reader.GetSchemaTable();
                    var columnCount = reader.FieldCount;
                    
                    // Build parameterized insert statement
                    var insertQuery = BuildInsertQuery(tableName, reader);
                    
                    using (var transaction = sqliteConnection.BeginTransaction())
                    {
                        var insertCommand = new SQLiteCommand(insertQuery, sqliteConnection, transaction);
                        
                        int recordCount = 0;
                        while (reader.Read())
                        {
                            insertCommand.Parameters.Clear();
                            
                            for (int i = 0; i < columnCount; i++)
                            {
                                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                
                                // Handle date conversion
                                if (value is DateTime dateTime)
                                {
                                    value = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                                }
                                
                                insertCommand.Parameters.AddWithValue($"@param{i}", value ?? DBNull.Value);
                            }
                            
                            insertCommand.ExecuteNonQuery();
                            recordCount++;
                            
                            if (recordCount % 1000 == 0)
                            {
                                Console.WriteLine($"  Migrated {recordCount} records...");
                            }
                        }
                        
                        transaction.Commit();
                        Console.WriteLine($"  Successfully migrated {recordCount} records from {tableName}");
                    }
                }
                
                accessConnection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error migrating table {tableName}: {ex.Message}");
                if (accessConnection.State == ConnectionState.Open)
                    accessConnection.Close();
                throw;
            }
        }

        private string BuildInsertQuery(string tableName, IDataReader reader)
        {
            var columns = new StringBuilder();
            var parameters = new StringBuilder();
            
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (i > 0)
                {
                    columns.Append(", ");
                    parameters.Append(", ");
                }
                
                columns.Append(reader.GetName(i));
                parameters.Append($"@param{i}");
            }
            
            return $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";
        }

        public void Dispose()
        {
            accessConnection?.Dispose();
            sqliteConnection?.Dispose();
        }
    }

    // Usage example
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                string accessDbPath = @"C:\path\to\mc.accdb";
                string sqliteDbPath = @"C:\path\to\mc.db";
                
                // Ensure the SQLite file doesn't exist or remove it
                if (File.Exists(sqliteDbPath))
                {
                    File.Delete(sqliteDbPath);
                }
                
                var migrator = new AccessToSQLiteMigrator(accessDbPath, sqliteDbPath);
                migrator.MigrateDatabase();
                
                Console.WriteLine("Migration completed successfully!");
                Console.WriteLine($"SQLite database created at: {sqliteDbPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}