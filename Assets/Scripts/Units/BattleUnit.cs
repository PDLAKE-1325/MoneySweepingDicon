using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class BattleUnit : Unit
{
    [Header("식별")]
    [SerializeField] UnitTeam _team;
    [SerializeField] int _id;
    [SerializeField] bool _isDied;
    public UnitTeam Team => _team;
    public int Id => _id;
    public bool IsDied => _isDied;
    
    
    [Header("상태")]
    // UnitData, _unitdata
    public int Status_Hp { get; private set; }
    public int Status_MaxHp => _unitData.Status.MaxHp + _unitData.StatusModifier.MaxHp;
    public int Status_AttackDamage => _unitData.Status.AttackDamage + _unitData.StatusModifier.AttackDamage;
    public int Status_MagicDamage => _unitData.Status.MagicDamage + _unitData.StatusModifier.MagicDamage;
    public int Status_AttackDefence => _unitData.Status.AttackDefence + _unitData.StatusModifier.AttackDefence;
    public int Status_MagicDefence => _unitData.Status.MagicDefence + _unitData.StatusModifier.MagicDefence;
    public int Status_Penetration => _unitData.Status.Penetration + _unitData.StatusModifier.Penetration;
    public int Status_Speed => _unitData.Status.Speed + _unitData.StatusModifier.Speed;
    public int Status_Critial => _unitData.Status.Critial + _unitData.StatusModifier.Critial;
    public int Status_CritialDamage => _unitData.Status.CritialDamage + _unitData.StatusModifier.CritialDamage;

    [Header("효과들")]
    private List<BattleUnitEffect> _effects;
    public IReadOnlyList<BattleUnitEffect> Effects => _effects;

    private Dictionary<MarkType, int> _marks;
    public IReadOnlyDictionary<MarkType, int> Marks => _marks;


    #region Unity Methods

    protected virtual void Update()
    {
        RotateToCamera();
    }

    #endregion

    #region Utilities

    public void SetUnit(int id, UnitTeam team)
    {
        _id = id;
        _team = team;
    }

    protected override void InitializeUnit()
    {
        base.InitializeUnit();

        _isDied = false;
        Status_Hp = _unitData.Status.MaxHp;

        _effects = new();
        _marks = new();
    }

    protected virtual void RotateToCamera()
    {
        transform.rotation = Cam.Instance.MainCamera.transform.rotation;
    }
    #endregion

    #region HP

    protected virtual void TakeDamage(int damage)
    {
        if(_isDied) return;
        if(damage < 0)
        {
            Heal(-damage);
            return;
        }

        Status_Hp = Mathf.Clamp(Status_Hp - damage, 0, Status_MaxHp);

        if(Status_Hp == 0) Die();
    }

    protected virtual void Heal(int amount)
    {
        if(_isDied) return;
        if(amount < 0)
        {
            TakeDamage(-amount);
            return;
        }

        Status_Hp = Mathf.Clamp(Status_Hp + amount, 0, Status_MaxHp);
    }

    protected virtual void Die()
    {
        _isDied = true;
    }

    #endregion

    #region Turn

    public virtual async UniTask OnMyTurn(CancellationToken token)
    {
        print($"[턴 시작 > {Info_Name} - {TurnManager.Instance.GetBattleTime()}]");
        await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.K), cancellationToken: token);
        // print($"[턴 종료 > {_unitData.Name}]");
    }

    #endregion

    #region Action Execute

    public virtual bool CanExecute()
    {
        bool result = true;

        if(_isDied) result = false;

        return result;
    }

    public virtual void ApplyEffects(int index)
    {
        if(!CanExecute()) return;

        BattleUnitEffect effect = _effects[index];
        if(effect.AffectTurn == 0) _effects.RemoveAt(index);

    }

    public virtual async UniTask ExecuteTurnActions(TurnActionType action, CancellationToken token)
    {
        if(!CanExecute()) return;

        switch (action)
        {
            case TurnActionType.NormalAttack:
                await NormalAttack(token);
                break;
            case TurnActionType.Skill_1:
                await Skill_1(token);
                break;
        }
    }

    #endregion

    #region ActionEvents

    protected virtual async UniTask NormalAttack(CancellationToken token)
    {
        // ICommand command = new NormalAttackCommand(SO 넣고)
        // CommandInvoker.ExecuteCommand()
        await UniTask.Yield(token);
    }
    protected virtual async UniTask Skill_1(CancellationToken token)
    {
        // ICommand command = new NormalAttackCommand(SO 넣고)
        // CommandInvoker.ExecuteCommand()
        await UniTask.Yield(token);
    }

    public virtual void OnTurnStart()
    {
        for (int i = _effects.Count-1; i >= 0; i--)
        {
            if(_effects[i].ActionType == ActionType.OnTurnStart) 
                ApplyEffects(i);
        }
    }

    public virtual void OnTurnEnd()
    {
        for (int i = _effects.Count-1; i >= 0; i--)
        {
            if(_effects[i].ActionType == ActionType.OnTurnStart) 
                ApplyEffects(i);
        }
    }
//     public virtual void OnBattleStart()
// {
// }

// public virtual void OnBattleEnd()
// {
// }

// public virtual void OnAttack(BattleUnit target)
// {
// }

// public virtual void OnTakeDamage(int damage)
// {
// }

// public virtual void OnKill(BattleUnit target)
// {
// }
    #endregion

    #region Effect / Mark

    public virtual void AddEffect(params BattleUnitEffect[] effect)
    {
        if(!CanExecute()) return;

        for (int i = 0; i < effect.Length; i++)
            _effects.Add(effect[i]);
    }

    public virtual void ClearEffect(EffectType effectType, bool clearAll = false)
    {
        if(!CanExecute()) return;

        if (clearAll)
        {
            _effects.Clear();
            return;
        }

        for (int i = _effects.Count-1; i >=0 ; i--)
            if(_effects[i].EffectType == effectType)
                _effects.RemoveAt(i);
    }


    // 표식은 effect 액션으로 알아서 추가해야할듯
    protected virtual void SetMark(MarkType type, int amount) => _marks[type] = Mathf.Max(amount, 0);
    protected virtual void AddMark(MarkType type, int amount) => _marks[type] = Mathf.Max(_marks[type] + amount, 0);
    #endregion
}

public enum TurnActionType{
    NormalAttack,
    Skill_1,
    Skill_2,
    Ultimate,
    HaveRest,
}
//플레이어(0)플레이어2(1)나는적(2)플레이어(0)플레이어2(1)나는적(2)플레이어(0)플레이어2(1)나는적(2)플레이어(0)
//플레이어(0)플레이어2(1)나는적(2)플레이어(0)플레이어2(1)플레이어(0)플레이어2(1)나는적(2)플레이어(0)플레이어2(1)나는적(2)