using UnityEngine;

/// <summary>
/// 味方GK用のNPC思考。現状はプレースホルダ（GOAP / 専用ロジックは今後実装）。
/// </summary>
[RequireComponent(typeof(AnimalControlAssignment))]
public class GoalkeeperNpcBrain : MonoBehaviour
{
    [SerializeField] private AnimalControlAssignment _assignment;

    private void Awake()
    {
        if (_assignment == null)
        {
            _assignment = GetComponent<AnimalControlAssignment>();
        }
    }

    private void OnEnable()
    {
        if (_assignment != null)
        {
            _assignment.RoleChanged += OnRoleChanged;
            OnRoleChanged(_assignment.Role);
        }
    }

    private void OnDisable()
    {
        if (_assignment != null)
        {
            _assignment.RoleChanged -= OnRoleChanged;
        }
    }

    private void OnRoleChanged(AnimalControlRole role)
    {
        enabled = role == AnimalControlRole.GoalkeeperNpc;
    }
}
