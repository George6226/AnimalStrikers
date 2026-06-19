using System;
using UnityEngine;

/// <summary>
/// 各アニマルに付与する操作ロール。SquadControlController が割当する。
/// </summary>
public class AnimalControlAssignment : MonoBehaviour
{
    [SerializeField] private AnimalControlRole _role = AnimalControlRole.Unassigned;

    public AnimalControlRole Role => _role;
    public bool IsHumanControlled => _role == AnimalControlRole.Human;
    public bool IsTeammateNpc => _role == AnimalControlRole.TeammateNpc;
    public bool IsGoalkeeperNpc => _role == AnimalControlRole.GoalkeeperNpc;

    public event Action<AnimalControlRole> RoleChanged;

    public void SetRole(AnimalControlRole role)
    {
        if (_role == role)
        {
            return;
        }

        _role = role;
        RoleChanged?.Invoke(_role);
    }
}
