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
    public int AllocateId(int? id)
    {
        if (id.HasValue)
        {
            int retId = id.Value;
            nextId = Math.Max(nextId, retId + 1);
            return retId;
        }
        else
        {
            return nextId++;
        }
    }
}