-- Access Database Queries Converted to SQLite
-- Original queries from mc - Copy.accdb converted to SQLite-compatible SQL

-- 101 Par Stats - Parameter statistics summary
CREATE VIEW IF NOT EXISTS ParStats AS
SELECT 
    ParID,
    MIN(NumericValue) AS MinOfNumericValue,
    AVG(NumericValue) AS AvgOfNumericValue,
    MAX(NumericValue) AS MaxOfNumericValue
FROM ParList
WHERE NumericValue IS NOT NULL
GROUP BY ParID;

-- 102 Sampled Pars - Parameters selected for sampling/analysis
CREATE VIEW IF NOT EXISTS SampledPars AS
SELECT DISTINCT ParID
FROM ParList
WHERE NumericValue IS NOT NULL
ORDER BY ParID;

-- 104 Parameter Ranges - Calculate parameter ranges for analysis
CREATE VIEW IF NOT EXISTS ParameterRanges AS
SELECT 
    sp.ParID,
    MIN(sp.ID) AS MinOfID,
    MAX(sp.ID) AS MaxOfID,
    MIN(sp.ParameterValue) AS MinOfParameterValue,
    MAX(sp.ParameterValue) AS MaxOfParameterValue,
    (MAX(sp.ID) - MIN(sp.ID)) AS IDRange
FROM SortedParameters sp
INNER JOIN SampledPars ON sp.ParID = SampledPars.ParID
GROUP BY sp.ParID;

-- 105 Parameters with Offsets - Calculate parameter offsets for distribution analysis
CREATE VIEW IF NOT EXISTS ParametersWithOffsets AS
SELECT 
    sp.ID,
    sp.RunID,
    sp.ParID,
    sp.ParameterValue,
    pr.MinOfParameterValue,
    pr.MaxOfParameterValue,
    (sp.ParameterValue - pr.MinOfParameterValue) / 
    (pr.MaxOfParameterValue - pr.MinOfParameterValue) AS Offset,
    pr.IDRange AS Runs
FROM SortedParameters sp
INNER JOIN ParameterRanges pr ON sp.ParID = pr.ParID;

-- 106 Observed and Theoretical Offsets - Compare empirical vs theoretical distributions
CREATE VIEW IF NOT EXISTS ObservedAndTheoreticalOffsets AS
SELECT 
    pwo.ParID,
    pwo.Runs,
    pwo.Offset / pwo.Runs AS ObservedCDF,
    pwo.Offset AS TheoreticalCDF
FROM ParametersWithOffsets pwo;

-- 107 Test Statistic - Calculate test statistics for distribution testing
CREATE VIEW IF NOT EXISTS TestStatistic AS
SELECT 
    ParID,
    MAX(ABS(TheoreticalCDF - ObservedCDF)) AS Test,
    Runs,
    TheoreticalCDF
FROM ObservedAndTheoreticalOffsets
GROUP BY ParID;

-- 108 KS D Statistic - Kolmogorov-Smirnov D statistic calculation
CREATE VIEW IF NOT EXISTS KSDStatistic AS
SELECT 
    ts.ParID,
    ts.Test AS D,
    ts.Runs,
    ts.TheoreticalCDF,
    SQRT(ts.Runs * ts.Runs / (2.0 * ts.Runs)) AS RunTerm,
    ts.Test * SQRT(ts.Runs * ts.Runs / (2.0 * ts.Runs)) AS xRange
FROM TestStatistic ts;

-- 109 KS D Statistic Extended - Extended KS statistic with additional calculations
CREATE VIEW IF NOT EXISTS KSDStatisticExtended AS
SELECT 
    ks.ParID,
    ks.D,
    ks.xRange,
    ks.RunTerm
FROM KSDStatistic ks;

-- 110 KS D and z - KS D statistic with z-score calculation
CREATE VIEW IF NOT EXISTS KSDAndZ AS
SELECT 
    kse.ParID,
    kse.D,
    kse.xRange,
    kse.D * SQRT(kse.RunTerm * kse.RunTerm / (2.0 * kse.RunTerm)) + 0.12 + 0.11/SQRT(kse.RunTerm) AS z
FROM KSDStatisticExtended kse;

-- 111 pTerm1 - First term for p-value calculation
CREATE VIEW IF NOT EXISTS pTerm1 AS
SELECT 
    kz.ParID,
    kz.D,
    kz.xRange,
    kz.z,
    EXP(-2.0 * kz.z * kz.z) AS pTerm1
FROM KSDAndZ kz;

-- 112 pTerm2 - Second term for p-value calculation  
CREATE VIEW IF NOT EXISTS pTerm2 AS
SELECT 
    p1.ParID,
    p1.D,
    p1.xRange,
    p1.z,
    p1.pTerm1,
    -1.0 * EXP(-2.0 * 4.0 * p1.z * p1.z) + p1.pTerm1 AS pTerm2
FROM pTerm1 p1;

-- 113 pTerm3 - Third term for p-value calculation
CREATE VIEW IF NOT EXISTS pTerm3 AS
SELECT 
    p2.ParID,
    p2.D,
    p2.xRange,
    p2.z,
    EXP(-18.0 * p2.z * p2.z) + p2.pTerm2 AS pTerm3
FROM pTerm2 p2;

-- 114 KS D z and p - Complete KS test results with p-values
CREATE VIEW IF NOT EXISTS KSDZAndP AS
SELECT 
    p3.ParID,
    p3.D,
    p3.xRange,
    p3.z,
    -1.0 * EXP(-32.0 * p3.z * p3.z) + p3.pTerm3 AS p
FROM pTerm3 p3;

-- 115 KS D z and P with Names - KS test results with parameter names
CREATE VIEW IF NOT EXISTS KSDZAndPWithNames AS
SELECT 
    pn.ParName,
    kzp.ParID,
    kzp.D,
    sp.MinOfNumericValue,
    sp.MaxOfNumericValue,
    kzp.xRange,
    kzp.z,
    kzp.p
FROM KSDZAndP kzp
INNER JOIN ParNames pn ON kzp.ParID = pn.ParID
INNER JOIN SampledPars sp_ref ON kzp.ParID = sp_ref.ParID
INNER JOIN ParStats sp ON kzp.ParID = sp.ParID;

-- 116 Statistics Summary - Overall statistics summary for parameters
CREATE VIEW IF NOT EXISTS StatisticsSummary AS
SELECT 
    kzpn.ParName,
    kzpn.ParID,
    kzpn.p
FROM KSDZAndPWithNames kzpn
ORDER BY kzpn.p DESC, kzpn.ParID;

-- 117 Parameter Summary to Plot - Data formatted for plotting/visualization
CREATE VIEW IF NOT EXISTS ParameterSummaryToPlot AS
SELECT 
    ss.ParName,
    ss.ParID,
    ss.p,
    kzpn.D,
    kzpn.xRange,
    kzpn.z,
    kzpn.MinOfNumericValue,
    kzpn.MaxOfNumericValue
FROM StatisticsSummary ss
INNER JOIN KSDZAndPWithNames kzpn ON ss.ParID = kzpn.ParID
ORDER BY ss.p DESC;

-- Additional helper views for data analysis

-- Parameter List with Names - Join parameter values with names
CREATE VIEW IF NOT EXISTS ParameterListWithNames AS
SELECT 
    pl.RunID,
    pl.ParID,
    pn.ParName,
    pl.TextValue,
    pl.NumericValue
FROM ParList pl
INNER JOIN ParNames pn ON pl.ParID = pn.ParID;

-- Results Summary - Basic results summary by reach
CREATE VIEW IF NOT EXISTS ResultsSummary AS
SELECT 
    Reach,
    COUNT(*) AS RecordCount,
    AVG(Flow) AS AvgFlow,
    MIN(Flow) AS MinFlow,
    MAX(Flow) AS MaxFlow,
    AVG(TerrestrialInput) AS AvgTerrestrialInput
FROM Results
WHERE Flow IS NOT NULL
GROUP BY Reach;

-- Coefficient Summary - Performance metrics summary
CREATE VIEW IF NOT EXISTS CoefficientSummary AS
SELECT 
    Reach,
    Parameter,
    AVG(R2) AS AvgR2,
    AVG(NS) AS AvgNS,
    AVG(RMSE) AS AvgRMSE,
    COUNT(*) AS RunCount
FROM Coefficients
WHERE R2 IS NOT NULL
GROUP BY Reach, Parameter;