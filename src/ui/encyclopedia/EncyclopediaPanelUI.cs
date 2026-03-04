using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 百科全书面板 UI。
/// </summary>
[GlobalClass]
public partial class EncyclopediaPanelUI : PanelContainer
{
	// 词条元信息。
	private sealed class EncyclopediaEntry
	{
		// 词条标题。
		public string Title { get; }

		// 词条文件路径。
		public string Path { get; }

		public EncyclopediaEntry(string title, string path)
		{
			Title = title;
			Path = path;
		}
	}

	// 词条列表。
	private readonly List<EncyclopediaEntry> entries = new List<EncyclopediaEntry>
	{
		new EncyclopediaEntry("世界逻辑系统", "res://src/data/encyclopedia/世界逻辑系统.txt"),
		new EncyclopediaEntry("覆盖物与地表编辑", "res://src/data/encyclopedia/覆盖物与地表编辑.txt"),
		new EncyclopediaEntry("建筑生产与劳动力", "res://src/data/encyclopedia/建筑生产与劳动力.txt")
	};

	// 左侧词条列表控件。
	private ItemList entryList = null!;

	// 右侧词条标题控件。
	private Label entryTitle = null!;

	// 右侧词条正文控件。
	private RichTextLabel entryContent = null!;

	public override void _Ready()
	{
		entryList = GetNode<ItemList>("Margin/RootSplit/LeftContainer/EntryList");
		entryTitle = GetNode<Label>("Margin/RootSplit/RightContainer/EntryTitle");
		entryContent = GetNode<RichTextLabel>("Margin/RootSplit/RightContainer/EntryContent");
		entryList.ItemSelected += onEntrySelected;
	}

	/// <summary>
	/// 初始化词条列表，仅加载元信息。
	/// </summary>
	public void Setup()
	{
		entryList.Clear();
		entries.Select(entry => entry.Title).ToList().ForEach(title => entryList.AddItem(title));
		entryTitle.Text = "词条内容";
		entryContent.Text = "请选择左侧词条进行查看。";
	}

	// 处理词条选中事件。
	private void onEntrySelected(long index)
	{
		if (index < 0 || index >= entries.Count)
		{
			return;
		}

		loadEntryContent((int)index);
	}

	// 动态加载指定词条内容。
	private void loadEntryContent(int index)
	{
		EncyclopediaEntry entry = entries[index];
		entryTitle.Text = entry.Title;

		string globalPath = ProjectSettings.GlobalizePath(entry.Path);
		if (!File.Exists(globalPath))
		{
			entryContent.Text = $"词条文件不存在：{entry.Path}";
			return;
		}

		entryContent.Text = File.ReadAllText(globalPath);
	}
}
