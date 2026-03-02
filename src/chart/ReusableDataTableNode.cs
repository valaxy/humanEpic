using Godot;

/// <summary>
/// 可复用数据表格组件，负责将 DataSource 渲染为二维表格。
/// </summary>
[GlobalClass]
public partial class ReusableDataTableNode : VBoxContainer
{
    // 表格标题标签。
    private Label titleLabel = null!;

    // 数据网格容器。
    private GridContainer dataGrid = null!;

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
    public void Render(DataSource dataSource)
    {
        titleLabel.Text = dataSource.Title;
        dataGrid.Columns = dataSource.Headers.Count;
        ClearGrid();

        for (var columnIndex = 0; columnIndex < dataSource.Headers.Count; columnIndex++)
        {
            var headerAlignment = HorizontalAlignment.Left;
            if (columnIndex < dataSource.HeaderAlignments.Count)
            {
                headerAlignment = (HorizontalAlignment)dataSource.HeaderAlignments[columnIndex];
            }

            dataGrid.AddChild(CreateCell(dataSource.Headers[columnIndex], headerAlignment));
        }

        foreach (var row in dataSource.Rows)
        {
            for (var columnIndex = 0; columnIndex < dataSource.Headers.Count; columnIndex++)
            {
                var cellValue = string.Empty;
                if (columnIndex < row.Count)
                {
                    cellValue = row[columnIndex].ToString();
                }

                var cellAlignment = HorizontalAlignment.Right;
                if (columnIndex < dataSource.CellAlignments.Count)
                {
                    cellAlignment = (HorizontalAlignment)dataSource.CellAlignments[columnIndex];
                }

                dataGrid.AddChild(CreateCell(cellValue, cellAlignment));
            }
        }
    }

    // 清空当前网格中的所有单元格。
    private void ClearGrid()
    {
        foreach (var child in dataGrid.GetChildren())
        {
            child.QueueFree();
        }
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
}