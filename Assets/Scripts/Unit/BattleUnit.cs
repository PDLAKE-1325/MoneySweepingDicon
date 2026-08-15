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

    // [Header("스킬")]


    #region Unity Methods

    protected virtual void Update()
    {
        RotateToCamera();
    }

    protected virtual void OnEnable()
    {
        BattleManager.Instance.OnSomeoneDied += OnSomeoneDied;
    }

    protected virtual void OnDisable()
    {
        BattleManager.Instance.OnSomeoneDied -= OnSomeoneDied;
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

    #region Turn

    public virtual async UniTask OnPlayerTurn(CancellationToken token)
    {
        print($"[턴 시작 > {Info_Name} - {TurnManager.Instance.GetBattleTime()}]");
        await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.K), cancellationToken: token);
        await UniTask.Yield(token);
    }

    public virtual async UniTask OnEnemyTurn(CancellationToken token)
    {
        print($"[턴 시작 > {Info_Name} - {TurnManager.Instance.GetBattleTime()}]");
        await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.K), cancellationToken: token);
        await UniTask.Yield(token);
    }

    #endregion

    #region HP

    public virtual void GetDamage(int damage)
    {
        if (_isDied) return;
        if (damage < 0)
        {
            GetHeal(-damage);
            return;
        }

        Status_Hp = Mathf.Clamp(Status_Hp - damage, 0, Status_MaxHp);

        print($"{Info_Name} 데미지 {damage} 받음. 남음 체력 {Status_Hp}");

        if (Status_Hp == 0) Die();
    }

    public virtual void GetHeal(int amount)
    {
        if (_isDied) return;
        if (amount < 0)
        {
            GetDamage(-amount);
            return;
        }

        Status_Hp = Mathf.Clamp(Status_Hp + amount, 0, Status_MaxHp);
    }

    protected virtual void Die()
    {
        print($"{Info_Name} 사망");
        BattleManager.Instance.OnSomeoneDied.Invoke();
        _marks.Clear();
        _effects.Clear();
        _isDied = true;
    }

    #endregion

    #region Action Execute

    public virtual bool CanExecute(BattleUnitEffect effect = null)
    {
        bool result = true;

        if (_isDied) result = false;
        if (
            effect != null &&
            effect.DisappearWhenUserDied &&
            BattleManager.Instance.GetUnit(effect.UserId).IsDied
        ) result = false;

        return result;
    }

    public virtual void EffectApplyRoutine(ActionType actionType)
    {

        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            if (_effects[i].ApplyActionType == actionType)
                ApplyEffects(i);
        }
    }

    public virtual void ApplyEffects(int index)
    {
        BattleUnitEffect effect = _effects[index];
        if (!CanExecute(effect)) return;
        if (effect.AffectTurn <= 0)
        {
            effect.RemoveEffectFunc(effect, this);
            _effects.RemoveAt(index);
            return;
        }
        effect.AffectTurn -= 1;

        if (!effect.Affectable) return;
        effect.ApplyEffectFunc(effect, this);
    }

    public virtual void OnUnitDeadEffectDisappear()
    {
        if (!CanExecute()) return;

        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            BattleUnitEffect effect = _effects[i];
            if (BattleManager.Instance.GetUnit(effect.UserId).IsDied && effect.DisappearWhenUserDied)
            {
                effect.RemoveEffectFunc(effect, this);
                _effects.RemoveAt(i);
            }
        }
    }

    public virtual async UniTask ExecuteTurnAction(TurnActionType action, CancellationToken token)
    {
        if (!CanExecute()) return;

        switch (action)
        {
            case TurnActionType.NormalAttack:
                await NormalAttack(token);
                break;
            case TurnActionType.Skill_1:
                await Skill_1(token);
                break;
            case TurnActionType.Ultimate:
                await Ultimate(token);
                break;
        }
    }

    #endregion

    #region ActionEvents

    protected virtual async UniTask NormalAttack(CancellationToken token)
    {
        // Skill So 만들어야함
        // ICommand command = new NormalAttackCommand(SO 넣고)
        // CommandInvoker.ExecuteCommand()
        await UniTask.Yield(token);
    }
    protected virtual async UniTask Skill_1(CancellationToken token)
    {
        await UniTask.Yield(token);
    }

    protected virtual async UniTask Ultimate(CancellationToken token)
    {
        await UniTask.Yield(token);
    }

    public virtual void OnTurnStart()
    {
        EffectApplyRoutine(ActionType.OnTurnStart);
    }

    public virtual void OnTurnEnd()
    {
        EffectApplyRoutine(ActionType.OnTurnEnd);
    }

    public virtual void OnAffected()
    {
        EffectApplyRoutine(ActionType.OnEffectAdded);
    }

    public virtual void OnSomeoneDied()
    {
        // 각 유닛 Died -> 배틀매니저 액션 호출 -> 해당 액션에 이 함수 구독 OnEn/Disabled에
        OnUnitDeadEffectDisappear();
    }

    //턴 시작 시 공격력 +10% / HP 30% 이하일 때 방어력 증가 / 적 처치 시 추가 행동 <- 이런거
    //     public virtual void OnBattleStart()
    // {
    // 이런건 BattleManager에서 순회 돌면서 실행해주면 됨
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

    public virtual void AddEffect(params BattleUnitEffect[] effects)
    {
        if (!CanExecute()) return;

        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i].OverlapEffect)
            {
                bool overlapped = false;
                for (int j = 0; j < _effects.Count; j++)
                {
                    if (_effects[j].Name == effects[i].Name)
                    {
                        overlapped = true;
                        _effects[j] = effects[i];
                        break;
                    }
                }
                if (!overlapped)
                    _effects.Add(effects[i]);
            }
            else
                _effects.Add(effects[i]);

            if (effects[i].ApplyActionType == ActionType.OnEffectAdded)
                OnAffected();
        }
    }

    public virtual void ClearEffect(EffectType effectType, bool clearAll = false)
    {
        if (!CanExecute()) return;

        if (clearAll)
        {
            _effects.Clear();
            return;
        }

        for (int i = _effects.Count - 1; i >= 0; i--)
            if (_effects[i].EffectType == effectType)
                _effects.RemoveAt(i);
    }


    // 표식은 effect 액션으로 알아서 추가해야할듯
    public virtual void SetMark(MarkType type, int amount) => _marks[type] = Mathf.Max(amount, 0);
    public virtual void AddMark(MarkType type, int amount)
    {
        _marks.TryGetValue(type, out int value);
        _marks[type] = Mathf.Max(value + amount, 0);
    }
    #endregion
}

public enum TurnActionType
{
    NormalAttack,
    Skill_1,
    Skill_2,
    Ultimate,
    HaveRest,
}


// 이제 스킬 SO 만들어서 TurnAction 만들고 메인 OnMyTurn해야함.
// SO에 매커니즘까지 다박자 걍


//플레이어(0)플레이어2(1)나는적(2)플레이어(0)플레이어2(1)나는적(2)플레이어(0)플레이어2(1)나는적(2)플레이어(0)
//플레이어(0)플레이어2(1)나는적(2)플레이어(0)플레이어2(1)플레이어(0)플레이어2(1)나는적(2)플레이어(0)플레이어2(1)나는적(2)