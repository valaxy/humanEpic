using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 可复用数据表格组件，负责将 DataSource 渲染为二维表格。
/// </summary>
[GlobalClass]
public partial class ReusableDataTable : VBoxContainer
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

        Enumerable.Range(0, dataSource.Headers.Count)
            .ToList()
            .ForEach(columnIndex =>
            {
                HorizontalAlignment headerAlignment = columnIndex < dataSource.HeaderAlignments.Count
                    ? mapAlignment(dataSource.HeaderAlignments[columnIndex])
                    : HorizontalAlignment.Left;

                dataGrid.AddChild(CreateCell(dataSource.Headers[columnIndex], headerAlignment));
            });

        dataSource.Rows
            .ToList()
            .ForEach(row =>
            {
                Enumerable.Range(0, dataSource.Headers.Count)
                    .ToList()
                    .ForEach(columnIndex =>
                    {
                        string cellValue = columnIndex < row.Count
                            ? row[columnIndex]
                            : string.Empty;

                        HorizontalAlignment cellAlignment = columnIndex < dataSource.CellAlignments.Count
                            ? mapAlignment(dataSource.CellAlignments[columnIndex])
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