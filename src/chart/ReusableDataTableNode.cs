using Godot;

/// <summary>
/// 可复用数据表格组件，负责将 DataSource 渲染为二维表格。
/// </summary>
[GlobalClass]
public partial class ReusableDataTableNode : VBoxContainer
{
    private Label _titleLabel = null!;
    private GridContainer _dataGrid = null!;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("%TitleLabel");
        _dataGrid = GetNode<GridContainer>("%DataGrid");
    }

    public void Render(DataSource dataSource)
    {
        _titleLabel.Text = dataSource.Title;
        _dataGrid.Columns = dataSource.Headers.Count;
        ClearGrid();

        for (var columnIndex = 0; columnIndex < dataSource.Headers.Count; columnIndex++)
        {
            var headerAlignment = HorizontalAlignment.Left;
            if (columnIndex < dataSource.HeaderAlignments.Count)
            {
                headerAlignment = (HorizontalAlignment)dataSource.HeaderAlignments[columnIndex];
            }

            _dataGrid.AddChild(CreateCell(dataSource.Headers[columnIndex], headerAlignment));
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

                _dataGrid.AddChild(CreateCell(cellValue, cellAlignment));
            }
        }
    }

    private void ClearGrid()
    {
        foreach (var child in _dataGrid.GetChildren())
        {
            child.QueueFree();
        }
    }

    private static Label CreateCell(string text, HorizontalAlignment alignment)
    {
        return new Label
        {
            Text = text,
            HorizontalAlignment = alignment
        };
    }
}