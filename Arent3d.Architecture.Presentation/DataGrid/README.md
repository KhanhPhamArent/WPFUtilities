# DataGridWrapper Frozen Column Support

The `DataGridWrapper` now supports frozen columns functionality, allowing you to freeze specific columns in the header that correspond to the DataGrid's frozen columns.

## Features

- **Dependency Property Support**: `FrozenColumnCount` is now a dependency property that can be set independently
- **Automatic Header Updates**: Changes to `FrozenColumnCount` automatically trigger header updates
- **Separate Header Sections**: Frozen columns have their own header section that doesn't scroll horizontally
- **Synchronized Scrolling**: The scrollable header section remains synchronized with the DataGrid's scrollable columns
- **XAML Support**: Can be set directly in XAML or through data binding

## Usage

### Basic Usage

```csharp
// Create a DataGrid
var dataGrid = new DataGrid();

// Create the wrapper and set the DataGrid
var wrapper = new DataGridWrapper();
wrapper.DataGrid = dataGrid;
wrapper.DataContext = yourDataGridContext;

// Set frozen column count
wrapper.FrozenColumnCount = 2; // Freeze the first 2 columns
```

### XAML Usage

```xml
<local:DataGridWrapper 
    DataGrid="{Binding MyDataGrid}"
    DataContext="{Binding MyDataGridContext}"
    FrozenColumnCount="2" />
```

### Data Binding

```xml
<local:DataGridWrapper 
    DataGrid="{Binding MyDataGrid}"
    DataContext="{Binding MyDataGridContext}"
    FrozenColumnCount="{Binding FrozenColumnCount}" />
```

### Dynamic Updates

```csharp
// Change the frozen column count - header updates automatically
wrapper.FrozenColumnCount = 3;
```

## How It Works

1. **Header Structure**: The wrapper creates two separate header sections:
   - `FrozenHeader`: Contains headers for frozen columns (doesn't scroll)
   - `Header`: Contains headers for scrollable columns (scrolls with the DataGrid)

2. **Column Definitions**: Column definitions are created separately for frozen and scrollable sections, with proper width bindings to the DataGrid columns.

3. **Content Placement**: Header content is placed in the appropriate section based on whether the column is frozen or not.

4. **Width Synchronization**: The frozen column width is automatically calculated and synchronized with the DataGrid's frozen columns.

5. **Automatic Updates**: When `FrozenColumnCount` changes, the header is automatically rebuilt to reflect the new frozen column configuration.

## Implementation Details

- **FrozenColumnCount Dependency Property**: Added as a dependency property for XAML support and automatic updates
- **GroupInfo.IsFrozen**: Added property to track whether a column group is frozen
- **HeaderGridBuilder**: Updated to create separate column definitions for frozen and scrollable sections
- **HeaderContentBuilder**: Updated to place content in the appropriate header section
- **DataGridWrapper**: Added frozen column width calculation and synchronization

## Requirements

- The `DataContext` must implement `IDataGridContext` interface
- Column headers must be properly configured in the `IDataGridContext.ColumnHeaders` property
- `FrozenColumnCount` should be set to the desired number of frozen columns (0 or positive integer)
