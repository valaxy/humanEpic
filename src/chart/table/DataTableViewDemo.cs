using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 表格独立 Demo 入口。
/// </summary>
[GlobalClass]
public partial class DataTableViewDemo : Control
{
    // 表格组件。
    private DataTableView dataTable = null!;

    /// <summary>
    /// 初始化并渲染表格 Demo。
    /// </summary>
    public override void _Ready()
    {
        dataTable = GetNode<DataTableView>("%DataTable");
        dataTable.Render(createTableData(), createTableConfig());
    }

    // 构建表格演示数据。
    private static DataSource createTableData()
    {
        List<string> headers = ["地块", "日产量", "库存", "效率"]; 

        List<List<string>> rows = Enumerable.Range(1, 10)
            .Select(index =>
            {
                int production = 920 + index * 64;
                int storage = 1800 + index * 95;
                string efficiency = $"{0.62f + index * 0.025f:P1}";
                return new List<string>
                {
                    $"农田-{index:00}",
                    production.ToString(),
                    storage.ToString(),
                    efficiency
                };
            })
            .ToList();

        return DataSource.CreateTable("资源统计表", headers, rows);
    }

    // 构建表格演示配置。
    private static DataTable createTableConfig()
    {
        List<DataTextAlignment> alignments =
        [
            DataTextAlignment.Left,
            DataTextAlignment.Right,
            DataTextAlignment.Right,
            DataTextAlignment.Right
        ];

        return DataTable.Create(
            "资源统计表",
            alignments,
            alignments,
            sortableColumns: Enumerable.Range(0, 4));
    }
}
