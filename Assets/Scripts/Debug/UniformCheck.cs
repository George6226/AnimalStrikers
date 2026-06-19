using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniformCheck : MonoBehaviour
{

    [SerializeField] UniformList _target;

    public void SetTeamColor(bool isBlue)
    {
        _target.SetTeamColor(isBlue);
    }

    public void ChangeUniform(int texNum)
    {
        _target.ChangeUniform(texNum);
    }

}
