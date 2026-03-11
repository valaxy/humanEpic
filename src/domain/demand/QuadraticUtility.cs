using System.Diagnostics;

/// <summary>
/// 【提示词】
/// - $$U(x)$$：$$x$$为人均需求满足度，$$U(x)$$表示总效用
/// - $$U(0) = 0$$，$$U(1) = A$$，$$A$$代表最大效用
/// - $$x > 0$$时，$$U(x)$$升高；$$x > 1$$时，$$U(x)$$降低
/// - 一个可能的$$U(x)$$是什么？它的导数是什么？
/// 
/// 【二次模型】
/// - $$U(x) = -Ax^2 + 2Ax$$
/// - $$U'(x) = -2Ax + 2A$$
/// </summary>
public class QuadraticUtility : IDemandUtility
{
    private float maxUtility; // 最大效用

    public QuadraticUtility(float maxUtility)
    {
        this.maxUtility = maxUtility;
    }

    public float GetUtility(float demandDegree)
    {
        Debug.Assert(demandDegree >= 0);
        return -maxUtility * demandDegree * demandDegree + 2 * maxUtility * demandDegree; // 二次函数
    }

    public float GetUtilityDerivative(float demandDegree)
    {
        Debug.Assert(demandDegree >= 0);
        return 2 * maxUtility * (1 - demandDegree); // 导数
    }
}