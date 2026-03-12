using Godot;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// 数据表格组件，负责将 DataSource 渲染为二维表格。
/// </summary>
[GlobalClass]
public partial class DataTableView : VBoxContainer
{
    // 表格标题标签。
    private Label titleLabel = null!;

    // 数据网格容器。
    private GridContainer dataGrid = null!;

    // 当前数据源。
    private DataSource currentDataSource = DataTableDataSourceFactory.Create(string.Empty, [], []);

    // 当前表格配置。
    private DataTable currentDataTable = DataTable.Create(string.Empty);

    // 当前排序列索引。
    private int? sortedColumnIndex;

    // 当前排序方向。
    private TableSortDirection.Enums sortDirection = TableSortDirection.Enums.DEFAULT;

    /// <summary>
    /// 初始化节点引用。
    /// </summary>
    public override void _Ready()
    {
        titleLabel = GetNode<Label>("%TitleLabel");
        dataGrid = GetNode<GridContainer>("%DataGrid");
    }

    /// <summary>
    /// 使用数据源渲染表格。
    /// </summary>
    /// <param name="dataSource">表格数据源。</param>
    /// <param name="dataTable">表格可视化配置。</param>
    public void Render(DataSource dataSource, DataTable? dataTable = null)
    {
        currentDataSource = dataSource;
        currentDataTable = dataTable ?? createDefaultTableConfig(dataSource);
        sortedColumnIndex = null;
        sortDirection = TableSortDirection.Enums.DEFAULT;
        renderCurrent();
    }

    // 重新渲染当前数据与配置。
    private void renderCurrent()
    {
        string title = string.IsNullOrWhiteSpace(currentDataTable.Title)
            ? currentDataSource.Title
            : currentDataTable.Title;
        titleLabel.Text = title;
        dataGrid.Columns = currentDataSource.Headers.Count;
        ClearGrid();

        Enumerable.Range(0, currentDataSource.Headers.Count)
            .ToList()
            .ForEach(columnIndex =>
            {
                HorizontalAlignment headerAlignment = columnIndex < currentDataTable.HeaderAlignments.Count
                    ? mapAlignment(currentDataTable.HeaderAlignments[columnIndex])
                    : HorizontalAlignment.Left;

                dataGrid.AddChild(createHeaderCell(columnIndex, currentDataSource.Headers[columnIndex], headerAlignment));
            });

        getSortedRows()
            .ToList()
            .ForEach(row =>
            {
                Enumerable.Range(0, currentDataSource.Headers.Count)
                    .ToList()
                    .ForEach(columnIndex =>
                    {
                        string cellValue = columnIndex < row.Count
                            ? row[columnIndex]
                            : string.Empty;

                        HorizontalAlignment cellAlignment = columnIndex < currentDataTable.CellAlignments.Count
                            ? mapAlignment(currentDataTable.CellAlignments[columnIndex])
                            : HorizontalAlignment.Right;

                        dataGrid.AddChild(CreateCell(cellValue, cellAlignment));
                    });
            });
    }

    // 清空当前网格中的所有单元格。
    private void ClearGrid()
    {
        List<Node> children = dataGrid.GetChildren().Cast<Node>().ToList();
        children.ForEach(child => child.QueueFree());
    }

    // 创建单元格标签。
    private static Label CreateCell(string text, HorizontalAlignment alignment)
    {
        return new Label
        {
            Text = text,
            HorizontalAlignment = alignment
        };
    }

    // 创建可排序表头单元格。
    private Control createHeaderCell(int columnIndex, string headerText, HorizontalAlignment alignment)
    {
        if (!isSortableColumn(columnIndex))
        {
            return CreateCell(headerText, alignment);
        }

        string displayedHeader = getDisplayedHeaderText(columnIndex, headerText);
        Button button = new Button
        {
            Text = displayedHeader,
            Flat = true,
            Alignment = alignment,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            FocusMode = FocusModeEnum.None,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        button.Pressed += () => toggleSort(columnIndex);
        return button;
    }

    // 获取排序后的行数据。
    private IReadOnlyList<IReadOnlyList<string>> getSortedRows()
    {
        if (!sortedColumnIndex.HasValue || sortDirection == TableSortDirection.Enums.DEFAULT)
        {
            return currentDataSource.Rows;
        }

        int columnIndex = sortedColumnIndex.Value;
        bool allNumeric = currentDataSource.Rows
            .Select(row => getCellValue(row, columnIndex))
            .All(tryParseNumber);

        return sortDirection == TableSortDirection.Enums.ASC
            ? sortRows(columnIndex, allNumeric, ascending: true)
            : sortRows(columnIndex, allNumeric, ascending: false);
    }

    // 执行排序。
    private IReadOnlyList<IReadOnlyList<string>> sortRows(int columnIndex, bool allNumeric, bool ascending)
    {
        List<(IReadOnlyList<string> Row, int Index)> indexedRows = currentDataSource.Rows
            .Select((row, index) => (Row: row, Index: index))
            .ToList();

        if (allNumeric)
        {
            return ascending
                ? indexedRows
                    .OrderBy(item => float.Parse(getCellValue(item.Row, columnIndex), CultureInfo.InvariantCulture))
                    .ThenBy(item => item.Index)
                    .Select(item => item.Row)
                    .ToList()
                : indexedRows
                    .OrderByDescending(item => float.Parse(getCellValue(item.Row, columnIndex), CultureInfo.InvariantCulture))
                    .ThenBy(item => item.Index)
                    .Select(item => item.Row)
                    .ToList();
        }

        return ascending
            ? indexedRows
                .OrderBy(item => getCellValue(item.Row, columnIndex), System.StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Index)
                .Select(item => item.Row)
                .ToList()
            : indexedRows
                .OrderByDescending(item => getCellValue(item.Row, columnIndex), System.StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Index)
                .Select(item => item.Row)
                .ToList();
    }

    // 切换排序状态（默认 -> 升序 -> 降序 -> 默认）。
    private void toggleSort(int columnIndex)
    {
        if (!sortedColumnIndex.HasValue || sortedColumnIndex.Value != columnIndex)
        {
            sortedColumnIndex = columnIndex;
            sortDirection = TableSortDirection.Enums.ASC;
            renderCurrent();
            return;
        }

        sortDirection = sortDirection switch
        {
            TableSortDirection.Enums.DEFAULT => TableSortDirection.Enums.ASC,
            TableSortDirection.Enums.ASC => TableSortDirection.Enums.DESC,
            TableSortDirection.Enums.DESC => TableSortDirection.Enums.DEFAULT,
            _ => TableSortDirection.Enums.DEFAULT
        };

        if (sortDirection == TableSortDirection.Enums.DEFAULT)
        {
            sortedColumnIndex = null;
        }

        renderCurrent();
    }

    // 是否允许该列排序。
    private bool isSortableColumn(int columnIndex)
    {
        if (currentDataTable.SortableColumns.Count == 0)
        {
            return true;
        }

        return currentDataTable.SortableColumns.Contains(columnIndex);
    }

    // 获取带排序标记的表头文本。
    private string getDisplayedHeaderText(int columnIndex, string headerText)
    {
        if (!sortedColumnIndex.HasValue || sortedColumnIndex.Value != columnIndex)
        {
            return headerText;
        }

        return sortDirection switch
        {
            TableSortDirection.Enums.ASC => $"{headerText} (ASC)",
            TableSortDirection.Enums.DESC => $"{headerText} (DESC)",
            _ => headerText
        };
    }

    // 获取单元格值。
    private static string getCellValue(IReadOnlyList<string> row, int columnIndex)
    {
        return columnIndex < row.Count ? row[columnIndex] : string.Empty;
    }

    // 判断文本是否可解析为数值。
    private static bool tryParseNumber(string text)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    // 生成默认表格配置。
    private static DataTable createDefaultTableConfig(DataSource dataSource)
    {
        return DataTable.Create(
            dataSource.Title,
            sortableColumns: Enumerable.Range(0, dataSource.Headers.Count));
    }

    // 将通用对齐值映射为 Godot 对齐枚举。
    private static HorizontalAlignment mapAlignment(DataTextAlignment alignment)
    {
        return alignment switch
        {
            DataTextAlignment.Left => HorizontalAlignment.Left,
            DataTextAlignment.Center => HorizontalAlignment.Center,
            DataTextAlignment.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };
    }
}