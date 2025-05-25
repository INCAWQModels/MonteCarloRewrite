using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SQLite;
using System.Collections;
using System.IO;

namespace MC
{
    public class resultsDatabase
    {
        protected SQLiteConnection localConnection;

        public resultsDatabase()
        {
            localConnection = new SQLiteConnection();
        }

        public void cleanUp()
        {
            string path = Directory.GetCurrentDirectory();
            MCParameters.databaseFileName = path + "\\mc.db";

            string localConnectionString = $"Data Source={MCParameters.databaseFileName};Version=3;";
            localConnection.ConnectionString = localConnectionString;
            
            try { localConnection.Open(); }
            catch (SQLiteException ex) { Console.WriteLine(ex.Message); }
            
            if (localConnection.State == ConnectionState.Open)
            {
                // Clean up data from tables
                executeSQLCommand("DELETE FROM ParNames");
                executeSQLCommand("DELETE FROM ParList");
                executeSQLCommand("DELETE FROM SortedParameters");
                executeSQLCommand("DELETE FROM CoefficientWeights");
                executeSQLCommand("DELETE FROM DValues");
                executeSQLCommand("DELETE FROM ParameterSensitivitySummary");
                
                // Drop tables if they exist
                executeSQLCommand("DROP TABLE IF EXISTS Coefficients");
                executeSQLCommand("DROP TABLE IF EXISTS Results");
                executeSQLCommand("DROP TABLE IF EXISTS INCAInputs");
                executeSQLCommand("DROP TABLE IF EXISTS Observations");
                executeSQLCommand("DROP TABLE IF EXISTS ReachID");
                
                localConnection.Close();
            }
            else 
            { 
                Console.WriteLine("Could not clean up database"); 
            }
        }

        public void processParameterData()
        {
            string path = Directory.GetCurrentDirectory();
            MCParameters.databaseFileName = path + "\\mc.db";

            string localConnectionString = $"Data Source={MCParameters.databaseFileName};Version=3;";
            localConnection.ConnectionString = localConnectionString;
            
            try { localConnection.Open(); }
            catch (SQLiteException ex) { Console.WriteLine(ex.Message); }
            
            if (localConnection.State == ConnectionState.Open)
            {
                executeSQLCommand(@"
                    INSERT INTO SortedParameters (ParID, ParameterValue, RunID)
                    SELECT pl.ParID, pl.NumericValue, pl.RunID 
                    FROM ParList pl 
                    INNER JOIN SampledPars sp ON pl.ParID = sp.ParID 
                    ORDER BY pl.ParID, pl.NumericValue
                ");
                localConnection.Close();
            }
            else 
            { 
                Console.WriteLine("Could not process parameters");
                Console.ReadLine();
            }
        }

        public void createParameterSensitivitySummaryTable()
        {
            string localConnectionString = $"Data Source={MCParameters.databaseFileName};Version=3;";
            localConnection.ConnectionString = localConnectionString;
            
            try { localConnection.Open(); }
            catch (SQLiteException ex) { Console.WriteLine(ex.Message); }

            // Create the summary table and populate it from statistics views
            executeSQLCommand(@"
                INSERT OR REPLACE INTO ParameterSensitivitySummary 
                (ParName, ParID, D, MinOfNumericValue, MaxOfNumericValue, xRange, z, p)
                SELECT ParName, ParID, D, MinOfNumericValue, MaxOfNumericValue, xRange, z, p 
                FROM StatisticsSummary
            ");
            
            localConnection.Close();
        }

        private void notYetImplemented()
        {
            Console.WriteLine("This feature is not yet implemented for this version of INCA");
            Console.WriteLine("Text files are generated which can be used for subsequent analysis");
        }

        public void makeResultsTable()
        {
            string localConnectionString = $"Data Source={MCParameters.databaseFileName};Version=3;";
            localConnection.ConnectionString = localConnectionString;
            
            try { localConnection.Open(); }
            catch (SQLiteException ex) { Console.WriteLine(ex.Message); }
            
            switch (MCParameters.model)
            {
                case 1: //PERSiST 1.4
                case 8: // PERSiST 1.6
                case 10: //PERSiST v2
                    makePERSiSTResultsTable();
                    makeINCAInputsTable();
                    break;
                case 2: //INCA-C 1.7
                    makeINCA_CResultsTable();
                    break;
                case 3: //INCA-PEco
                case 4: //INCA-P
                case 5: //INCA-Contaminants
                case 6: //INCA-Path
                case 11: //INCA-C v2.x
                case 12: //INCA-N Classic
                case 13: //INCA-C 1.8
                    notYetImplemented();
                    break;
                case 7:
                    makeINCA_HgResultsTable();
                    break;
                case 9:
                    makeINCA_ONTHEResultsTable();
                    break;
                default:
                    Console.WriteLine("Something has gone wrong when making the RESULTS table");
                    break;
            }
            localConnection.Close();
        }

        private void makePERSiSTResultsTable()
        {
            string SQLString = @"
                CREATE TABLE IF NOT EXISTS Results (
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    Reach TEXT,
                    TerrestrialInput REAL,
                    Flow REAL,
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (RUN, RowNumber)
                )";
            executeSQLCommand(SQLString);
        }

        private void makeINCA_CResultsTable()
        {
            string SQLString = @"
                CREATE TABLE IF NOT EXISTS Results (
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    Reach TEXT,
                    Flow REAL,
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (RUN, RowNumber)
                )";
            executeSQLCommand(SQLString);
        }

        private void makeINCA_HgResultsTable()
        {
            string SQLString = @"
                CREATE TABLE IF NOT EXISTS Results (
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    Reach TEXT,
                    Flow REAL,
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (RUN, RowNumber)
                )";
            executeSQLCommand(SQLString);
        }

        private void makeINCAInputsTable()
        {
            string SQLString = @"
                CREATE TABLE IF NOT EXISTS INCAInputs (
                    FileName TEXT NOT NULL,
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    SMD REAL,
                    HER REAL,
                    T REAL,
                    P REAL,
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (FileName, RUN, RowNumber)
                )";
            executeSQLCommand(SQLString);
        }

        private void makeINCA_ONTHEResultsTable()
        {
            string SQLString = @"
                CREATE TABLE IF NOT EXISTS Results (
                    FileName TEXT,
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    FLOW REAL,
                    NITRATE REAL,
                    AMMONIUM REAL,
                    VOLUME REAL,
                    DON REAL,
                    VELOCITY REAL,
                    WIDTH REAL,
                    DEPTH REAL,
                    AREA REAL,
                    PERIMETER REAL,
                    RADIUS REAL,
                    RESIDENCETIME REAL,
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (RUN, RowNumber)
                )";
            executeSQLCommand(SQLString);
        }

        private void makeINCA_PResultsTable()
        {
            string SQLString = @"
                CREATE TABLE IF NOT EXISTS Results (
                    FileName TEXT,
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    Discharge REAL,
                    Volume REAL,
                    Velocity REAL,
                    WaterDepth REAL,
                    StreamPower REAL,
                    ShearVelocity REAL,
                    MaxEntGrainSize REAL,
                    MoveableBedMass REAL,
                    EntrainmentRate REAL,
                    DepositionRate REAL,
                    BedSediment REAL,
                    SuspendedSediment REAL,
                    DiffuseSediment REAL,
                    WaterColumnTDP REAL,
                    WaterColumnPP REAL,
                    WCSorptionRelease REAL,
                    StreamBedTDP REAL,
                    StreamBedPP REAL,
                    BedSorptionRelease REAL,
                    MacrophyteMass REAL,
                    EpiphyteMass REAL,
                    WaterColumnTP REAL,
                    WaterColumnSRP REAL,
                    WaterTemperature REAL,
                    TDPInput REAL,
                    PPInput REAL,
                    WaterColumnEPC0 REAL,
                    StreamBedEPC0 REAL,
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (RUN, RowNumber)
                )";
            executeSQLCommand(SQLString);
        }

        public void makeObservationsTable()
        {
            string localConnectionString = $"Data Source={MCParameters.databaseFileName};Version=3;";
            localConnection.ConnectionString = localConnectionString;
            
            try { localConnection.Open(); }
            catch (SQLiteException ex) { Console.WriteLine(ex.Message); }
            
            string SQLString = @"
                CREATE TABLE IF NOT EXISTS Observations (
                    Reach TEXT NOT NULL,
                    Parameter TEXT NOT NULL,
                    Value REAL,
                    QC TEXT,
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (Reach, Parameter, DateStamp)
                )";
            executeSQLCommand(SQLString);
            localConnection.Close();
        }

        public void makeCoefficientsTable()
        {
            string localConnectionString = $"Data Source={MCParameters.databaseFileName};Version=3;";
            localConnection.ConnectionString = localConnectionString;
            
            try { localConnection.Open(); }
            catch (SQLiteException ex) { Console.WriteLine(ex.Message); }
            
            switch (MCParameters.model)
            {
                case 1: //PERSiST 1.4
                case 8: // PERSiST 1.6
                    makePERSiSTCoefficientsTable();
                    break;
                case 2: //INCA-C
                case 11: //INCA-C 2.x
                    makeINCA_CCoefficientsTable();
                    break;
                case 3: //INCA-PEco
                    makeINCA_PEcoCoefficientsTable();
                    break;
                case 4: //INCA-P
                    makeINCA_PCoefficientsTable();
                    break;
                case 5: //INCA-Contaminants
                    notYetImplemented();
                    break;
                case 6: //INCA-Path
                    notYetImplemented();
                    break;
                case 7:
                    makeINCA_HgCoefficientsTable();
                    break;
                case 9:
                    makeINCA_ONTHECoefficientsTable();
                    break;
                case 10: //PERSiST v2
                    makePERSiST_v2CoefficientsTable();
                    break;
                case 12: //INCA-N
                    makeINCA_NCoefficientsTable();
                    break;
                case 13: // INCA-C 1.8
                    makeINCA_C18CoefficientsTable();
                    break;
                default:
                    Console.WriteLine("Something has gone wrong when making the COEFFICIENTS table");
                    Console.ReadLine();
                    break;
            }
            localConnection.Close();
        }

        private void makeDefaultCoefficientsTable()
        {
            string SQLString = @"
                CREATE TABLE IF NOT EXISTS Coefficients (
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    Reach TEXT,
                    Parameter TEXT,
                    R2 REAL,
                    NS REAL,
                    RMSE REAL,
                    RE REAL,
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (RUN, RowNumber, Reach, Parameter)
                )";
            executeSQLCommand(SQLString);
        }

        private void makeINCA_NCoefficientsTable()
        {
            makeDefaultCoefficientsTable();
        }

        private void makeINCA_ONTHECoefficientsTable()
        {
            string SQLString = @"
                CREATE TABLE IF NOT EXISTS Coefficients (
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    Reach TEXT,
                    Parameter TEXT,
                    R2 REAL,
                    NS REAL,
                    logNS REAL,
                    AD REAL,
                    VAR REAL,
                    KGE REAL,
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (RUN, RowNumber, Reach, Parameter)
                )";
            executeSQLCommand(SQLString);
        }

        private void makeINCA_PEcoCoefficientsTable()
        {
            string SQLString = @"
                CREATE TABLE IF NOT EXISTS Coefficients (
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    Reach TEXT,
                    Parameter TEXT,
                    R2 REAL,
                    NS REAL,
                    logNS REAL,
                    RMSE REAL,
                    AD REAL,
                    VR REAL,
                    KGE REAL,
                    CAT_B REAL,
                    CAT_C REAL,
                    CAT_CA REAL,
                    CAT_CB REAL,
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (RUN, RowNumber, Reach, Parameter)
                )";
            executeSQLCommand(SQLString);
        }

        private void makeINCA_PCoefficientsTable()
        {
            string SQLString = @"
                CREATE TABLE IF NOT EXISTS Coefficients (
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    Reach TEXT,
                    Parameter TEXT,
                    R2 REAL,
                    NS REAL,
                    RMSE REAL,
                    RE REAL,
                    VR REAL,
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (RUN, RowNumber, Reach, Parameter)
                )";
            executeSQLCommand(SQLString);
        }

        private void makePERSiSTCoefficientsTable()
        {
            string SQLString = @"
                CREATE TABLE IF NOT EXISTS Coefficients (
                    RUN INTEGER NOT NULL,
                    RowNumber INTEGER NOT NULL,
                    Reach TEXT,
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
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (RUN, RowNumber, Reach)
                )";
            executeSQLCommand(SQLString);
        }

        private void makeINCA_C18CoefficientsTable()
        {
            string SQLString = @"
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
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (RUN, RowNumber, Reach, Parameter)
                )";
            executeSQLCommand(SQLString);
        }

        private void makePERSiST_v2CoefficientsTable()
        {
            string SQLString = @"
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
                    DateStamp DATETIME DEFAULT (datetime('now')),
                    PRIMARY KEY (RUN, RowNumber, Reach, Parameter)
                )";
            executeSQLCommand(SQLString);
        }

        private void makeINCA_CCoefficientsTable()
        {
            makeDefaultCoefficientsTable();
        }

        private void makeINCA_HgCoefficientsTable()
        {
            makeDefaultCoefficientsTable();
        }

        // Write results methods with updated parameterized queries
        public void writeResults()
        {
            string localConnectionString = $"Data Source={MCParameters.databaseFileName};Version=3;";
            localConnection.ConnectionString = localConnectionString;
            
            try { localConnection.Open(); }
            catch (SQLiteException ex) { Console.WriteLine(ex.Message); }
            
            switch (MCParameters.model)
            {
                case 1: //PERSiST 1.4
                case 8: //PERSiST 1.6
                    //writeINCAResultsFromPERSiST();
                    //writePERSiSTResults();
                    break;
                case 2: //INCA-C
                    notYetImplemented();
                    break;
                case 3: //INCA-PEco
                    notYetImplemented();
                    break;
                case 4: //INCA-P
                    notYetImplemented();
                    break;
                case 5: //INCA-Contaminants
                    notYetImplemented();
                    break;
                case 6: //INCA-Path
                    notYetImplemented();
                    break;
                case 7: //INCA-Hg
                    notYetImplemented();
                    break;
                case 9: //INCA_ONTHE
                    notYetImplemented();
                    break;
                default:
                    Console.WriteLine("Something has gone wrong when populating the RESULTS table");
                    break;
            }
            localConnection.Close();
        }

        // Updated coefficient writing methods with parameterized queries
        public void writeCoefficients()
        {
            string localConnectionString = $"Data Source={MCParameters.databaseFileName};Version=3;";
            localConnection.ConnectionString = localConnectionString;
            
            try { localConnection.Open(); }
            catch (SQLiteException ex) { Console.WriteLine(ex.Message); }
            
            switch (MCParameters.model)
            {
                case 1: //PERSiST 1.4
                case 8: // PERSiST 1.6
                    writePERSiSTCoefficients();
                    break;
                case 2: //INCA-C v.1.7
                case 7: // INCA-Hg
                    writeGenericINCA_Coefficients();
                    break;
                case 3: //INCA-PEco
                    writeINCA_PEcoCoefficients();
                    break;
                case 4: //INCA-P
                    notYetImplemented();
                    break;
                case 5: //INCA-Contaminants
                    notYetImplemented();
                    break;
                case 6: //INCA-Path
                    notYetImplemented();
                    break;
                case 9: //INCA_ONTHE
                    writeINCA_ONTHECoefficients();
                    break;
                case 10: //PERSIST v2
                    writePERSiST_v2Coefficients();
                    break;
                case 11: //INCA_C v.2
                    writeGenericINCA_Coefficients();
                    break;
                case 12: // INCA-N v.1.x
                    writeINCA_NCoefficients();
                    break;
                case 13:
                    writeINCA_C18Coefficients();
                    break;
                default:
                    Console.WriteLine("Something has gone wrong when populating the COEFFICIENTS table");
                    break;
            }
            localConnection.Close();
        }

        private void writeINCA_C18Coefficients()
        {
            using (StreamReader sr = new StreamReader(MCParameters.coefficientsSummaryFile))
            {
                string reachName = "undefined";
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    try
                    {
                        string[] fields = line.Split(MCParameters.separatorChar);
                        int rownum = int.Parse(fields[1]);
                        
                        if ((rownum % 7) == 0)
                        {
                            reachName = fields[2];
                        }
                        else
                        {
                            if ((rownum % 7) > 1)
                            {
                                string SQLString = @"
                                    INSERT INTO Coefficients 
                                    (RUN, RowNumber, Reach, Parameter, R2, NS, LOG_NS, RMSE, RE, AD, VAR, N, N_RE, DateStamp) 
                                    VALUES (@run, @rowNumber, @reach, @parameter, @r2, @ns, @logNs, @rmse, @re, @ad, @var, @n, @nRe, datetime('now'))";

                                using (var cmd = new SQLiteCommand(SQLString, localConnection))
                                {
                                    cmd.Parameters.AddWithValue("@run", fields[0]);
                                    cmd.Parameters.AddWithValue("@rowNumber", fields[1]);
                                    cmd.Parameters.AddWithValue("@reach", reachName);
                                    cmd.Parameters.AddWithValue("@parameter", fields[2]);
                                    cmd.Parameters.AddWithValue("@r2", fields[3]);
                                    cmd.Parameters.AddWithValue("@ns", fields[4]);
                                    cmd.Parameters.AddWithValue("@logNs", fields[5]);
                                    cmd.Parameters.AddWithValue("@rmse", fields[6]);
                                    cmd.Parameters.AddWithValue("@re", fields[7]);
                                    cmd.Parameters.AddWithValue("@ad", fields[8]);
                                    cmd.Parameters.AddWithValue("@var", fields[9]);
                                    cmd.Parameters.AddWithValue("@n", fields[10]);
                                    cmd.Parameters.AddWithValue("@nRe", fields[11]);
                                    
                                    try { cmd.ExecuteNonQuery(); }
                                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                                }
                            }
                        }
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
            }
        }

        private void writePERSiSTCoefficients()
        {
            using (StreamReader sr = new StreamReader(MCParameters.coefficientsSummaryFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    try
                    {
                        string[] fields = line.Split(MCParameters.separatorChar);
                        if (!fields[1].Equals("0"))
                        {
                            string SQLString = @"
                                INSERT INTO Coefficients 
                                (RUN, RowNumber, Reach, R2, NS, LOG_NS, RMSE, RE, AD, VAR, N, N_RE, SS, LOG_SS, DateStamp) 
                                VALUES (@run, @rowNumber, @reach, @r2, @ns, @logNs, @rmse, @re, @ad, @var, @n, @nRe, @ss, @logSs, datetime('now'))";

                            using (var cmd = new SQLiteCommand(SQLString, localConnection))
                            {
                                cmd.Parameters.AddWithValue("@run", fields[0]);
                                cmd.Parameters.AddWithValue("@rowNumber", fields[1]);
                                cmd.Parameters.AddWithValue("@reach", fields[2]);
                                cmd.Parameters.AddWithValue("@r2", fields[3]);
                                cmd.Parameters.AddWithValue("@ns", fields[4]);
                                cmd.Parameters.AddWithValue("@logNs", fields[5]);
                                cmd.Parameters.AddWithValue("@rmse", fields[6]);
                                cmd.Parameters.AddWithValue("@re", fields[7]);
                                cmd.Parameters.AddWithValue("@ad", fields[8]);
                                cmd.Parameters.AddWithValue("@var", fields[9]);
                                cmd.Parameters.AddWithValue("@n", fields[10]);
                                cmd.Parameters.AddWithValue("@nRe", fields[11]);
                                cmd.Parameters.AddWithValue("@ss", fields[12]);
                                cmd.Parameters.AddWithValue("@logSs", fields[13]);
                                
                                try { cmd.ExecuteNonQuery(); }
                                catch (Exception ex) { Console.WriteLine(ex.Message); }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void writeINCA_PEcoCoefficients()
        {
            try
            {
                using (StreamReader sr = new StreamReader(MCParameters.coefficientsSummaryFile))
                {
                    string line;
                    string reachName = "";
                    
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] fields = line.Split(MCParameters.separatorChar);
                        int fieldNumber = Int32.Parse(fields[1]);
                        if ((fieldNumber % 16) == 0)
                        {
                            reachName = fields[2];
                        }
                        if (fields.Length > 10)
                        {
                            string SQLString = @"
                                INSERT INTO Coefficients 
                                (RUN, RowNumber, Reach, Parameter, R2, NS, logNS, RMSE, AD, VR, KGE, CAT_B, CAT_C, CAT_CA, CAT_CB, DateStamp) 
                                VALUES (@run, @rowNumber, @reach, @parameter, @r2, @ns, @logNs, @rmse, @ad, @vr, @kge, @catB, @catC, @catCa, @catCb, datetime('now'))";

                            using (var cmd = new SQLiteCommand(SQLString, localConnection))
                            {
                                cmd.Parameters.AddWithValue("@run", fields[0]);
                                cmd.Parameters.AddWithValue("@rowNumber", fields[1]);
                                cmd.Parameters.AddWithValue("@reach", reachName);
                                cmd.Parameters.AddWithValue("@parameter", fields[2]);
                                cmd.Parameters.AddWithValue("@r2", fields[3]);
                                cmd.Parameters.AddWithValue("@ns", fields[4]);
                                cmd.Parameters.AddWithValue("@logNs", fields[5]);
                                cmd.Parameters.AddWithValue("@rmse", fields[6]);
                                cmd.Parameters.AddWithValue("@ad", fields[8]);
                                cmd.Parameters.AddWithValue("@vr", fields[9]);
                                cmd.Parameters.AddWithValue("@kge", fields[13]);
                                cmd.Parameters.AddWithValue("@catB", fields[14]);
                                cmd.Parameters.AddWithValue("@catC", fields[15]);
                                cmd.Parameters.AddWithValue("@catCa", fields[16]);
                                cmd.Parameters.AddWithValue("@catCb", fields[17]);
                                
                                try { cmd.ExecuteNonQuery(); }
                                catch (Exception ex) { Console.WriteLine(ex.Message); }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void executeSQLCommand(string commandString)
        {
            using (var cmd = new SQLiteCommand(commandString, localConnection))
            {
                try { cmd.ExecuteNonQuery(); }
                catch (SQLiteException ex) { Console.WriteLine(ex.Message); }
            }
        }

        private void writeParameter(int runID, int ParID, string textValue)
        {
            string numericValueString;

            try
            {
                double test = Convert.ToDouble(textValue);
                numericValueString = textValue;
            }
            catch { numericValueString = null; }

            string insertCommandString = @"
                INSERT OR REPLACE INTO ParList (RunID, ParID, TextValue, NumericValue) 
                VALUES (@runId, @parId, @textValue, @numericValue)";
            
            using (var cmd = new SQLiteCommand(insertCommandString, localConnection))
            {
                cmd.Parameters.AddWithValue("@runId", runID);
                cmd.Parameters.AddWithValue("@parId", ParID);
                cmd.Parameters.AddWithValue("@textValue", textValue);
                cmd.Parameters.AddWithValue("@numericValue", numericValueString == null ? (object)DBNull.Value : Convert.ToDouble(numericValueString));
                
                try { cmd.ExecuteNonQuery(); }
                catch (Exception ex) { Console.WriteLine(ex.Message); }
            }
        }

        private void writeParameterName(int ParID, string parName)
        {
            string insertCommandString = @"
                INSERT OR REPLACE INTO ParNames (ParID, ParName) 
                VALUES (@parId, @parName)";
            
            using (var cmd = new SQLiteCommand(insertCommandString, localConnection))
            {
                cmd.Parameters.AddWithValue("@parId", ParID);
                cmd.Parameters.AddWithValue("@parName", parName);
                
                try { cmd.ExecuteNonQuery(); }
                catch (SQLiteException ex) { Console.WriteLine(ex.Message); }
            }
        }

        public void writeParameterSet(int runID, parameterSet pSet)
        {
            Console.WriteLine("Writing parameter set {0}", runID);
            string localConnectionString = $"Data Source={MCParameters.databaseFileName};Version=3;";
            localConnection.ConnectionString = localConnectionString;
            
            try { localConnection.Open(); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            using (var transaction = localConnection.BeginTransaction())
            {
                try
                {
                    int m = 0;
                    foreach (ArrayList l in pSet)
                    {
                        foreach (parameter p in l)
                        {
                            string[] s = (p.stringValue()).Split(MCParameters.separatorChar);
                            foreach (string par in s)
                            {
                                writeParameter(runID, m++, par);
                            }
                        }
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Error writing parameter set: {ex.Message}");
                }
            }
            
            localConnection.Close();
        }

        public void writeParameterNames(ParameterArrayList pal)
        {
            string localConnectionString = $"Data Source={MCParameters.databaseFileName};Version=3;";
            localConnection.ConnectionString = localConnectionString;
            
            try { localConnection.Open(); }
            catch (SQLiteException ex) { Console.WriteLine(ex.Message); }

            using (var transaction = localConnection.BeginTransaction())
            {
                try
                {
                    string[] s = (pal.header.ToString()).Split('\n');
                    int m = 0;
                    foreach (string par in s)
                    {
                        int splitPos = par.IndexOf(MCParameters.separatorChar);
                        if (splitPos > 0)
                        {
                            string parName = par.Substring(splitPos + 1);
                            writeParameterName(m++, parName);
                        }
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Error writing parameter names: {ex.Message}");
                }
            }
            
            localConnection.Close();
        }

        public void writeCoefficientWeights()
        {
            string localConnectionString = $"Data Source={MCParameters.databaseFileName};Version=3;";
            localConnection.ConnectionString = localConnectionString;
            
            try { localConnection.Open(); }
            catch (SQLiteException ex) { Console.WriteLine(ex.Message); }

            using (var transaction = localConnection.BeginTransaction())
            {
                try
                {
                    using (StreamReader coefficientWeights = new StreamReader(MCParameters.coefficientsWeightFile))
                    {
                        string line;
                        while ((line = coefficientWeights.ReadLine()) != null)
                        {
                            string[] fields = line.Split(MCParameters.separatorChar);
                            if (fields.Length >= 2)
                            {
                                string SQLString = @"
                                    INSERT OR REPLACE INTO CoefficientWeights (CoefficientName, CoefficientWeight) 
                                    VALUES (@coefficientName, @coefficientWeight)";

                                using (var cmd = new SQLiteCommand(SQLString, localConnection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@coefficientName", fields[0]);
                                    cmd.Parameters.AddWithValue("@coefficientWeight", fields[1]);
                                    
                                    try { cmd.ExecuteNonQuery(); }
                                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                                }
                            }
                        }
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Error writing coefficient weights: {ex.Message}");
                }
            }
            
            localConnection.Close();
        }

        // Helper methods for data retrieval
        public DataTable GetParameterStatistics()
        {
            var dataTable = new DataTable();
            string query = @"
                SELECT 
                    pn.ParName,
                    ps.ParID,
                    ps.MinOfNumericValue,
                    ps.AvgOfNumericValue,
                    ps.MaxOfNumericValue
                FROM ParStats ps
                INNER JOIN ParNames pn ON ps.ParID = pn.ParID
                ORDER BY pn.ParName";

            using (var cmd = new SQLiteCommand(query, localConnection))
            {
                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    adapter.Fill(dataTable);
                }
            }
            return dataTable;
        }

        public DataTable GetSensitivitySummary()
        {
            var dataTable = new DataTable();
            string query = @"
                SELECT ParName, ParID, D, xRange, z, p
                FROM ParameterSensitivitySummary
                ORDER BY p DESC";

            using (var cmd = new SQLiteCommand(query, localConnection))
            {
                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    adapter.Fill(dataTable);
                }
            }
            return dataTable;
        }

        public DataTable GetModelPerformance(string reach = null)
        {
            var dataTable = new DataTable();
            string query = @"
                SELECT Reach, Parameter, AVG(R2) as AvgR2, AVG(NS) as AvgNS, AVG(RMSE) as AvgRMSE, COUNT(*) as RunCount
                FROM Coefficients
                WHERE R2 IS NOT NULL";
            
            if (!string.IsNullOrEmpty(reach))
            {
                query += " AND Reach = @reach";
            }
            
            query += " GROUP BY Reach, Parameter ORDER BY Reach, Parameter";

            using (var cmd = new SQLiteCommand(query, localConnection))
            {
                if (!string.IsNullOrEmpty(reach))
                {
                    cmd.Parameters.AddWithValue("@reach", reach);
                }
                
                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    adapter.Fill(dataTable);
                }
            }
            return dataTable;
        }

        // Cleanup and disposal methods
        public void disconnect()
        {
            if (localConnection != null && localConnection.State == ConnectionState.Open)
            {
                localConnection.Close();
            }
        }

        public void Dispose()
        {
            disconnect();
            localConnection?.Dispose();
        }

        ~resultsDatabase()
        {
            Dispose();
        }
    }

    // Extension class for database initialization
    public static class DatabaseInitializer
    {
        public static void InitializeDatabase(string databasePath)
        {
            string connectionString = $"Data Source={databasePath};Version=3;";
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                // Read and execute the complete schema creation script
                string schemaScript = GetSchemaScript();
                
                using (var command = new SQLiteCommand(schemaScript, connection))
                {
                    command.ExecuteNonQuery();
                }
                
                Console.WriteLine($"Database initialized successfully at: {databasePath}");
            }
        }

        private static string GetSchemaScript()
        {
            return @"
                PRAGMA FOREIGN_KEYS = ON;

                CREATE TABLE IF NOT EXISTS PAR_NAMES (
                    PAR_ID INTEGER PRIMARY KEY,
                    PAR_NAME TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS PAR_LIST (
                    RUN_ID INTEGER NOT NULL,
                    PAR_ID INTEGER NOT NULL,
                    TEXT_VALUE TEXT,
                    NUMERIC_VALUE REAL,
                    PRIMARY KEY (RUN_ID, PAR_ID),
                    FOREIGN KEY (PAR_ID) REFERENCES PAR_NAMES(PAR_ID)
                );

                CREATE TABLE IF NOT EXISTS SORTED_PARAMETERS (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    RUN_ID INTEGER NOT NULL,
                    PAR_ID INTEGER NOT NULL,
                    PARAMETER_VALUE REAL,
                    FOREIGN KEY (PAR_ID) REFERENCES PAR_NAMES(PAR_ID)
                );

                CREATE TABLE IF NOT EXISTS RESULTS (
                    RUN INTEGER NOT NULL,
                    ROW_NUMBER INTEGER NOT NULL,
                    REACH TEXT,
                    TERRESTRIAL_INPUT REAL,
                    FLOW REAL,
                    DATE_STAMP DATETIME DEFAULT (DATETIME('NOW')),
                    PRIMARY KEY (RUN, ROW_NUMBER)
                );

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
                    DATE_STAMP DATETIME DEFAULT (DATETIME('NOW')),
                    PRIMARY KEY (RUN, ROW_NUMBER, REACH, PARAMETER)
                );

                CREATE TABLE IF NOT EXISTS COEFFICIENT_WEIGHTS (
                    COEFFICIENT_NAME TEXT PRIMARY KEY,
                    COEFFICIENT_WEIGHT REAL NOT NULL
                );

                CREATE TABLE IF NOT EXISTS INCA_INPUTS (
                    FILE_NAME TEXT NOT NULL,
                    RUN INTEGER NOT NULL,
                    ROW_NUMBER INTEGER NOT NULL,
                    SMD REAL,
                    HER REAL,
                    T REAL,
                    P REAL,
                    DATE_STAMP DATETIME DEFAULT (DATETIME('NOW')),
                    PRIMARY KEY (FILE_NAME, RUN, ROW_NUMBER)
                );

                CREATE TABLE IF NOT EXISTS OBSERVATIONS (
                    REACH TEXT NOT NULL,
                    PARAMETER TEXT NOT NULL,
                    VALUE REAL,
                    QC TEXT,
                    DATE_STAMP DATETIME DEFAULT (DATETIME('NOW')),
                    PRIMARY KEY (REACH, PARAMETER, DATE_STAMP)
                );

                CREATE TABLE IF NOT EXISTS D_VALUES (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
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

                CREATE TABLE IF NOT EXISTS REACH_ID (
                    ID_CODE INTEGER PRIMARY KEY,
                    REACH TEXT UNIQUE NOT NULL
                );

                -- CREATE ALL THE STATISTICAL ANALYSIS VIEWS
                CREATE VIEW IF NOT EXISTS PAR_STATS AS
                SELECT 
                    PAR_ID,
                    MIN(NUMERIC_VALUE) AS MIN_OF_NUMERIC_VALUE,
                    AVG(NUMERIC_VALUE) AS AVG_OF_NUMERIC_VALUE,
                    MAX(NUMERIC_VALUE) AS MAX_OF_NUMERIC_VALUE
                FROM PAR_LIST
                WHERE NUMERIC_VALUE IS NOT NULL
                GROUP BY PAR_ID;

                CREATE VIEW IF NOT EXISTS SAMPLED_PARS AS
                SELECT DISTINCT PAR_ID
                FROM PAR_LIST
                WHERE NUMERIC_VALUE IS NOT NULL
                ORDER BY PAR_ID;

                -- CREATE INDEXES
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
        }
    }
}
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
            }
        }

        private void writePERSiST_v2Coefficients()
        {
            using (StreamReader sr = new StreamReader(MCParameters.coefficientsSummaryFile))
            {
                string line;
                string reachName = "undefined";
                while ((line = sr.ReadLine()) != null)
                {
                    try
                    {
                        string[] fields = line.Split(MCParameters.separatorChar);
                        if (fields.Length > 3)
                        {
                            int rownum = int.Parse(fields[1]);
                            if ((rownum % 7) == 0)
                            {
                                reachName = fields[2];
                            }
                            else
                            {
                                string SQLString = @"
                                    INSERT INTO Coefficients 
                                    (RUN, RowNumber, Reach, Parameter, R2, NS, LOG_NS, RMSE, RE, AD, VAR, N, N_RE, SS, LOG_SS, DateStamp) 
                                    VALUES (@run, @rowNumber, @reach, @parameter, @r2, @ns, @logNs, @rmse, @re, @ad, @var, @n, @nRe, @ss, @logSs, datetime('now'))";

                                using (var cmd = new SQLiteCommand(SQLString, localConnection))
                                {
                                    cmd.Parameters.AddWithValue("@run", fields[0]);
                                    cmd.Parameters.AddWithValue("@rowNumber", fields[1]);
                                    cmd.Parameters.AddWithValue("@reach", reachName);
                                    cmd.Parameters.AddWithValue("@parameter", fields[2]);
                                    cmd.Parameters.AddWithValue("@r2", fields[3]);
                                    cmd.Parameters.AddWithValue("@ns", fields[4]);
                                    cmd.Parameters.AddWithValue("@logNs", fields[5]);
                                    cmd.Parameters.AddWithValue("@rmse", fields[6]);
                                    cmd.Parameters.AddWithValue("@re", fields[7]);
                                    cmd.Parameters.AddWithValue("@ad", fields[8]);
                                    cmd.Parameters.AddWithValue("@var", fields[9]);
                                    cmd.Parameters.AddWithValue("@n", fields[10]);
                                    cmd.Parameters.AddWithValue("@nRe", fields[11]);
                                    cmd.Parameters.AddWithValue("@ss", fields[12]);
                                    cmd.Parameters.AddWithValue("@logSs", fields[13]);
                                    
                                    try { cmd.ExecuteNonQuery(); }
                                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                                }
                            }
                        }
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
            }
        }

        private void writeGenericINCA_Coefficients()
        {
            using (StreamReader sr = new StreamReader(MCParameters.coefficientsSummaryFile))
            {
                string line;
                string reachName = "undefined";
                while ((line = sr.ReadLine()) != null)
                {
                    try
                    {
                        string[] fields = line.Split(MCParameters.separatorChar);
                        if (fields.Length > 2)
                        {
                            int rownum = int.Parse(fields[1]);
                            if ((rownum % 8) == 0)
                            {
                                reachName = fields[2];
                            }
                            else
                            {
                                string SQLString = @"
                                    INSERT INTO Coefficients 
                                    (RUN, RowNumber, Reach, Parameter, R2, NS, RMSE, RE, DateStamp) 
                                    VALUES (@run, @rowNumber, @reach, @parameter, @r2, @ns, @rmse, @re, datetime('now'))";

                                using (var cmd = new SQLiteCommand(SQLString, localConnection))
                                {
                                    cmd.Parameters.AddWithValue("@run", fields[0]);
                                    cmd.Parameters.AddWithValue("@rowNumber", fields[1]);
                                    cmd.Parameters.AddWithValue("@reach", reachName);
                                    cmd.Parameters.AddWithValue("@parameter", fields[2]);
                                    cmd.Parameters.AddWithValue("@r2", fields[3]);
                                    cmd.Parameters.AddWithValue("@ns", fields[4]);
                                    cmd.Parameters.AddWithValue("@rmse", fields[5]);
                                    cmd.Parameters.AddWithValue("@re", fields[6]);
                                    
                                    try { cmd.ExecuteNonQuery(); }
                                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                                }
                            }
                        }
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
            }
        }

        private void writeINCA_NCoefficients()
        {
            using (StreamReader sr = new StreamReader(MCParameters.coefficientsSummaryFile))
            {
                string line;
                string reachName = "undefined";
                while ((line = sr.ReadLine()) != null)
                {
                    try
                    {
                        string[] fields = line.Split(MCParameters.separatorChar);
                        if (fields.Length > 2)
                        {
                            int rownum = int.Parse(fields[1]);
                            if ((rownum % 6) == 0)
                            {
                                reachName = fields[2];
                            }
                            else
                            {
                                string SQLString = @"
                                    INSERT INTO Coefficients 
                                    (RUN, RowNumber, Reach, Parameter, R2, NS, RMSE, RE, DateStamp) 
                                    VALUES (@run, @rowNumber, @reach, @parameter, @r2, @ns, @rmse, @re, datetime('now'))";

                                using (var cmd = new SQLiteCommand(SQLString, localConnection))
                                {
                                    cmd.Parameters.AddWithValue("@run", fields[0]);
                                    cmd.Parameters.AddWithValue("@rowNumber", fields[1]);
                                    cmd.Parameters.AddWithValue("@reach", reachName + "_Reach");
                                    cmd.Parameters.AddWithValue("@parameter", fields[2]);
                                    cmd.Parameters.AddWithValue("@r2", fields[3]);
                                    cmd.Parameters.AddWithValue("@ns", fields[4]);
                                    cmd.Parameters.AddWithValue("@rmse", fields[5]);
                                    cmd.Parameters.AddWithValue("@re", fields[6]);
                                    
                                    try { cmd.ExecuteNonQuery(); }
                                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                                }
                            }
                        }
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
            }
        }

        private void writeINCA_ONTHECoefficients()
        {
            try
            {
                using (StreamReader sr = new StreamReader(MCParameters.coefficientsSummaryFile))
                {
                    string line;
                    string reachName = "";

                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] fields = line.Split(MCParameters.separatorChar);
                        if (fields.Length == 3)
                        {
                            reachName = fields[2];
                        }
                        else if (fields.Length > 4)
                        {
                            string SQLString = @"
                                INSERT INTO Coefficients 
                                (RUN, RowNumber, Reach, Parameter, R2, NS, logNS, AD, VAR, KGE, DateStamp) 
                                VALUES (@run, @rowNumber, @reach, @parameter, @r2, @ns, @logNs, @ad, @var, @kge, datetime('now'))";

                            using (var cmd = new SQLiteCommand(SQLString, localConnection))
                            {
                                cmd.Parameters.AddWithValue("@run", fields[0]);
                                cmd.Parameters.AddWithValue("@rowNumber", fields[1]);
                                cmd.Parameters.AddWithValue("@reach", reachName);
                                cmd.Parameters.AddWithValue("@parameter", fields[2]);
                                cmd.Parameters.AddWithValue("@r2", fields[3]);
                                cmd.Parameters.AddWithValue("@ns", fields[4]);
                                cmd.Parameters.AddWithValue("@logNs", fields[5]);
                                cmd.Parameters.AddWithValue("@ad", fields[7]);
                                cmd.Parameters.AddWithValue("@var", fields[8]);
                                cmd.Parameters.AddWithValue("@kge", fields[13]);
                                
                                try { cmd.ExecuteNonQuery(); }
                                catch (Exception ex) { Console.WriteLine(ex.Message); }
                            }
                        }
                    }