using UnityEngine;

/// <summary>
/// 操作ロールに応じて子階層の GOAP（GoapAgent / AIContextSwitcher）の有効/無効を切り替える。
/// </summary>
[RequireComponent(typeof(AnimalControlAssignment))]
public class AnimalControlBrainRouter : MonoBehaviour
{
    [SerializeField] private AnimalControlAssignment _assignment;

    private AnimalFacade _facade;
    private AnimalGoapBrainComponents _goap;
    private bool _goapConfigured;
    private bool _productionGoapActive;

    private void Awake()
    {
        if (_assignment == null)
        {
            _assignment = GetComponent<AnimalControlAssignment>();
        }

        _facade = GetComponent<AnimalFacade>();
        _goap = AnimalGoapBrainComponents.Resolve(gameObject);
        _goap.SetActive(false);
    }

    private void OnEnable()
    {
        if (_assignment != null)
        {
            _assignment.RoleChanged += ApplyRole;
            ApplyRole(_assignment.Role);
        }
    }

    private void OnDisable()
    {
        if (_assignment != null)
        {
            _assignment.RoleChanged -= ApplyRole;
        }

        _goap.SetActive(false);
    }

    public void ApplyRole(AnimalControlRole role)
    {
        bool useGoap = role == AnimalControlRole.TeammateNpc && ShouldUseGoapPilot();
        if ((GoapBatchVerifyEnvironment.IsActive || GoapMainNpcVerifyEnvironment.IsActive) && _facade != null)
        {
            var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
            useGoap = squad != null && squad.ShouldUseGoapFor(_facade);
        }
        else if (GoapMainNpcProductionEnvironment.IsActive
            && role == AnimalControlRole.Human
            && _facade != null
            && GoapMainNpcProductionEnvironment.IsProductionMainPlayer(_facade))
        {
            useGoap = false;
            _productionGoapActive = false;
        }

        if (GoapMainNpcVerifyEnvironment.RequiresBootstrap)
        {
            useGoap = false;
        }

        if (useGoap)
        {
            TryConfigureGoapPilot();
        }

        _goap.SetActive(useGoap);
    }

    private void LateUpdate()
    {
        RefreshProductionMainNpcGoap();
    }

    private void RefreshProductionMainNpcGoap()
    {
        if (!GoapMainNpcProductionEnvironment.IsActive
            || _facade == null
            || _assignment == null
            || !_assignment.IsHumanControlled)
        {
            _productionGoapActive = false;
            return;
        }

        bool wantGoap = GoapMainNpcProductionEnvironment.ShouldEnableGoap(_goap.Blackboard, _facade);
        if (wantGoap == _productionGoapActive)
        {
            return;
        }

        _productionGoapActive = wantGoap;
        if (wantGoap)
        {
            _goapConfigured = false;
            TryConfigureGoapPilot();
            _goap.SetActive(true);
            return;
        }

        _goap.SetActive(false);
    }

    public void ResetGoapConfiguration()
    {
        _goapConfigured = false;
    }

    private bool ShouldUseGoapPilot()
    {
        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        return squad != null && _facade != null && squad.ShouldUseGoapPilotFor(_facade);
    }

    private void TryConfigureGoapPilot()
    {
        if (_goapConfigured || !_goap.HasAgent)
        {
            return;
        }

        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        if (squad == null)
        {
            return;
        }

        squad.ApplyGoapPilotConfiguration(_goap.Agent, _facade);
        _goapConfigured = true;
    }
}
