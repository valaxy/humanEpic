using System;


/// <summary>
/// ID分配器，负责为实体分配唯一标识符
/// </summary>
public class IdAllocator
{
    // ID从1开始
    private int nextId = 1;

    /// <summary>
    /// 分配一个新的唯一ID
    /// </summary>
    public int AllocateId()
    {
        return nextId++;
    }

    /// <summary>
    /// 避免分配已经存在的ID，确保下一个分配的ID不小于指定ID
    /// </summary>
    public void AvoidShow(int id)
    {
        nextId = Math.Max(nextId, id + 1);
    }
}