using System;

    /// <summary>
    /// 宣言番号を共有
    /// </summary>
public sealed class SysDoubtCurrentNumberNet
{
    public static SysDoubtCurrentNumberNet Instance { get; private set; }

    public Rank Current { get; private set; }
    public event Action<Rank> OnChanged;

    public SysDoubtCurrentNumberNet()
    {
        Instance = this;
        Current = Rank.Ace;
        OnChanged?.Invoke(Current);
    }

    /// <summary>
    /// 次の番号
    /// </summary>
    public Rank Next()
    {
        int next = (int)Current + 1;
        if (next > (int)Rank.King) next = (int)Rank.Ace;
        return (Rank)next;
    }
    
    /// <summary>
    /// 次に進む
    /// </summary>
    public void SetCurrentFromNet(Rank r)
    {
        Current = r;
        OnChanged?.Invoke(Current);
    }
}
