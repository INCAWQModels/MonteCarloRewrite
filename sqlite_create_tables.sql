-- SQLite CREATE TABLE statements for Access database conversion
-- Generated from mc - Copy.accdb schema analysis

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
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
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
    DateStamp DATETIME DEFAULT (datetime('now')),
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
    DateStamp DATETIME DEFAULT (datetime('now')),
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
    DateStamp DATETIME DEFAULT (datetime('now')),
    PRIMARY KEY (FileName, RUN, RowNumber)
);

-- 8. Observed data for model validation
CREATE TABLE IF NOT EXISTS Observations (
    Reach TEXT NOT NULL,
    Parameter TEXT NOT NULL,
    Value REAL,
    QC TEXT,
    DateStamp DATETIME DEFAULT (datetime('now')),
    PRIMARY KEY (Reach, Parameter, DateStamp)
);

-- 9. Statistical analysis D-values
CREATE TABLE IF NOT EXISTS DValues (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
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

-- Enable foreign key constraints
PRAGMA foreign_keys = ON;