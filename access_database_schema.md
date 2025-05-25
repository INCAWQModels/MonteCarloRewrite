# Access Database Schema Documentation

## Database Information
- **File**: mc - Copy.accdb
- **Type**: Microsoft Access Database (.accdb)
- **Size**: 778,445 bytes
- **Purpose**: Monte Carlo simulation results storage for environmental modeling (INCA/PERSiST models)

## Main Tables

### 1. ParNames
**Purpose**: Parameter name lookup table
- `ParID` (INTEGER) - Primary Key, Parameter identifier
- `ParName` (TEXT) - Parameter name/description

### 2. ParList  
**Purpose**: Parameter values for each model run
- `RunID` (INTEGER) - Model run identifier
- `ParID` (INTEGER) - Foreign Key to ParNames.ParID
- `TextValue` (TEXT) - Parameter value as text
- `NumericValue` (REAL) - Parameter value as number

### 3. SortedParameters
**Purpose**: Sorted parameter values for analysis
- `ID` (INTEGER) - Primary Key
- `RunID` (INTEGER) - Model run identifier  
- `ParID` (INTEGER) - Foreign Key to ParNames.ParID
- `ParameterValue` (REAL) - Numeric parameter value

### 4. Results
**Purpose**: Model simulation results
- `RUN` (INTEGER) - Run identifier
- `RowNumber` (INTEGER) - Row sequence number
- `Reach` (TEXT) - Reach/location identifier
- `TerrestrialInput` (REAL) - Terrestrial input values
- `Flow` (REAL) - Flow values
- `DateStamp` (DATETIME) - Timestamp

### 5. Coefficients
**Purpose**: Statistical coefficients and performance metrics
- `RUN` (INTEGER) - Run identifier
- `RowNumber` (INTEGER) - Row sequence number
- `Reach` (TEXT) - Reach/location identifier
- `Parameter` (TEXT) - Parameter name
- `R2` (REAL) - R-squared coefficient
- `NS` (REAL) - Nash-Sutcliffe coefficient
- `LOG_NS` (REAL) - Log Nash-Sutcliffe coefficient
- `RMSE` (REAL) - Root Mean Square Error
- `RE` (REAL) - Relative Error
- `AD` (REAL) - Anderson-Darling statistic
- `VAR` (REAL) - Variance
- `N` (REAL) - Sample size
- `N_RE` (REAL) - N Relative Error
- `SS` (REAL) - Sum of Squares
- `LOG_SS` (REAL) - Log Sum of Squares
- `DateStamp` (DATETIME) - Timestamp

### 6. CoefficientWeights
**Purpose**: Weights for different statistical coefficients
- `CoefficientName` (TEXT) - Name of the coefficient
- `CoefficientWeight` (REAL) - Weight value for the coefficient

### 7. INCAInputs
**Purpose**: INCA model input data
- `FileName` (TEXT) - Input file name
- `RUN` (INTEGER) - Run identifier
- `RowNumber` (INTEGER) - Row sequence number
- `SMD` (REAL) - Soil Moisture Deficit
- `HER` (REAL) - Hydrologically Effective Rainfall
- `T` (REAL) - Temperature
- `P` (REAL) - Precipitation
- `DateStamp` (DATETIME) - Timestamp

### 8. Observations
**Purpose**: Observed data for model comparison
- `Reach` (TEXT) - Reach/location identifier
- `Parameter` (TEXT) - Parameter name
- `Value` (REAL) - Observed value
- `QC` (TEXT) - Quality control flag
- `DateStamp` (DATETIME) - Timestamp

### 9. DValues
**Purpose**: Statistical analysis values
- `ID` (INTEGER) - Primary Key
- `Rank` (INTEGER) - Ranking value
- `ParName` (TEXT) - Parameter name
- `ParID` (INTEGER) - Parameter identifier
- `xRange` (REAL) - Parameter range
- `D` (REAL) - D statistic value
- `z` (REAL) - Z statistic value
- `p` (REAL) - P-value
- `AdjustedP` (REAL) - Adjusted P-value

### 10. ParameterSensitivitySummary
**Purpose**: Summary of parameter sensitivity analysis
- `ParName` (TEXT) - Parameter name
- `ParID` (INTEGER) - Parameter identifier
- `D` (REAL) - D statistic
- `MinOfNumericValue` (REAL) - Minimum parameter value
- `MaxOfNumericValue` (REAL) - Maximum parameter value
- `xRange` (REAL) - Parameter range
- `z` (REAL) - Z statistic
- `p` (REAL) - P-value

### 11. ReachID
**Purpose**: Reach/location lookup table
- `IDCode` (INTEGER) - Primary Key
- `Reach` (TEXT) - Reach name/identifier

## Query Objects

The database contains numerous query objects for statistical analysis:

### Statistical Analysis Queries
- `101 Par Stats` - Parameter statistics summary
- `102 Sampled Pars` - Sampled parameters for analysis
- `104 Parameter Ranges` - Parameter range calculations
- `105 Parameters with Offsets` - Parameters with offset calculations
- `106 Observed And Theoretical Offsets` - Offset comparisons
- `107 Test Statistic` - Test statistic calculations
- `108 KS D Statistic` - Kolmogorov-Smirnov D statistic
- `109 KS D Statistic` - Extended KS D statistic
- `110 KS D and z` - KS D statistic with z-values
- `111 pTerm1` - P-value calculation term 1
- `112 pTerm2` - P-value calculation term 2  
- `113 pTerm3` - P-value calculation term 3
- `114 KS D z and p` - Complete KS test results
- `115 KS D z and P with Names` - KS test results with parameter names
- `116 Statistics Summary` - Overall statistics summary
- `117 Parameter Summary to Plot` - Data formatted for plotting

## Relationships

### Primary Relationships
- `ParNames.ParID` ↔ `ParList.ParID`
- `ParNames.ParID` ↔ `SortedParameters.ParID`
- `ParNames.ParID` ↔ `DValues.ParID`
- `ParNames.ParID` ↔ `ParameterSensitivitySummary.ParID`
- `ReachID.Reach` ↔ `Results.Reach`
- `ReachID.Reach` ↔ `Coefficients.Reach`
- `ReachID.Reach` ↔ `Observations.Reach`

### Data Flow
1. **Parameters**: `ParNames` → `ParList` → `SortedParameters`
2. **Analysis**: `SortedParameters` → Statistical Queries → `DValues`/`ParameterSensitivitySummary`
3. **Results**: Model runs → `Results` → `Coefficients`
4. **Validation**: `Observations` → Statistical comparisons

## Data Types Used

- **INTEGER**: Identifiers, counts, row numbers
- **REAL**: Numeric values, statistics, measurements
- **TEXT**: Names, descriptions, categorical data
- **DATETIME**: Timestamps and dates

## Purpose and Usage

This database supports Monte Carlo sensitivity analysis for environmental models (INCA/PERSiST), storing:

1. **Parameter definitions and values** across multiple model runs
2. **Model simulation results** (flow, inputs, outputs)
3. **Statistical analysis results** (sensitivity, performance metrics)
4. **Observed data** for model validation
5. **Calculated statistics** (KS tests, p-values, coefficients)

The structure enables comprehensive uncertainty and sensitivity analysis of environmental modeling results.