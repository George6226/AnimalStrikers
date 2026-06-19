using UnityEngine;

public class AnimalInputHandler_NPC : AnimalInputHandler
{
    // スケール
    private float _slideScale = 0.0f;
    public float SlideScale
    {
        set { _slideScale = value; }
    }
    // 角度
    private float _radian = 0.0f;
    public float Radian
    {
        set { _radian = value; }
    }

    // タグを取得
    protected override string getTag()
    {
        return ConstData.NPC_TAG;
    }

    protected override (float, float) getMoveActionValues()
    {
        return (_slideScale, _radian);
    }
}
