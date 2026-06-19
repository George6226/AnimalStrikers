using UnityEngine;

public class Param_AnimalInfo : ScriptableObject
{
    [SerializeField] private AnimalInfoParam _animalInfoParam;
    public AnimalInfoParam InfoParam => _animalInfoParam;

    // 動物の種類
    public enum AnimalType
    {
        None,
        Lion,
        Gorilla,
        Boar,
        Tiger,
        Rhinoceros,
        Elephant,
        Crocodile,
        Shark,
        Bear,
        Ball
    }

    public enum SpecialAbility
    {
        None,
        Shoot,
        Attack,
        Buff,
        Debuff,
    }

    public enum SpecialTiming
    {
        None,
        HasBall,         // ボールを所持している
        NoBall,          // ボールを未所持
        AnyTime          // いつでも
    }

    // 戦闘パラメータ（一部のAnimalタイプのみが持つ）
    [System.Serializable]
    public struct AnimalCombatParam
    {
        // スタミナ
        [SerializeField] private int stamina;
        // 速度
        [SerializeField] private int speed;
        // シュート
        [SerializeField] private int shoot;
        // パス
        [SerializeField] private int pass;
        // 攻撃
        [SerializeField] private int attack;
        // 防御
        [SerializeField] private int defense;
        // スペシャル能力
        [SerializeField] private SpecialAbility specialAbility;
        // スペシャルのタイミング
        [SerializeField] private SpecialTiming specialTiming;

        public int Stamina => stamina;
        public int Speed => speed;
        public int Shoot => shoot;
        public int Pass => pass;
        public int Attack => attack;
        public int Defense => defense;
        public SpecialAbility SpecialAbility => specialAbility;
        public SpecialTiming SpecialTiming => specialTiming;
    }

    [System.Serializable]
    public struct AnimalInfoParam
    {
        // 動物名
        [SerializeField] private string _animalName;
        // 動物の種類
        [SerializeField] private AnimalType animalType;
        // 戦闘パラメータを持つかどうか
        [SerializeField] private bool _hasCombatParam;
        // 戦闘パラメータ（一部のAnimalタイプのみが持つ）
        [SerializeField] private AnimalCombatParam _combatParam;
        // アイコン
        [SerializeField] private Sprite icon;
        // GKかどうか
        [SerializeField] private bool _isGK;

        public string AnimalName => _animalName;
        public AnimalType AnimalType => animalType;
        public bool HasCombatParam => _hasCombatParam;
        public AnimalCombatParam CombatParam => _combatParam;
        public Sprite Icon => icon;
        public bool IsGK => _isGK;
        // 戦闘パラメータのプロパティ（HasCombatParamがtrueの場合のみ有効）
        public int Stamina => _hasCombatParam ? _combatParam.Stamina : 0;
        public int Speed => _hasCombatParam ? _combatParam.Speed : 0;
        public int Shoot => _hasCombatParam ? _combatParam.Shoot : 0;
        public int Pass => _hasCombatParam ? _combatParam.Pass : 0;
        public int Attack => _hasCombatParam ? _combatParam.Attack : 0;
        public int Defense => _hasCombatParam ? _combatParam.Defense : 0;
        public SpecialAbility SpecialAbility => _hasCombatParam ? _combatParam.SpecialAbility : SpecialAbility.None;
        public SpecialTiming SpecialTiming => _hasCombatParam ? _combatParam.SpecialTiming : SpecialTiming.None;
    }
}
